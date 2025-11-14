using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;

namespace SistemaDental.Infrastructure.Data;

/// <summary>
/// Interceptor para manejar conversiones de enums de PostgreSQL en comandos INSERT y UPDATE.
/// Convierte automáticamente valores de texto a enums de PostgreSQL para columnas que usan enums.
/// </summary>
public class PostgresEnumInterceptor : DbCommandInterceptor
{
    private readonly ILogger<PostgresEnumInterceptor>? _logger;

    public PostgresEnumInterceptor(ILogger<PostgresEnumInterceptor>? logger = null)
    {
        _logger = logger;
    }

    // Mapeo de tablas y columnas a sus enums correspondientes
    private static readonly Dictionary<string, Dictionary<string, string>> TableColumnEnumMap = new()
    {
        {
            "tenants",
            new Dictionary<string, string> { { "status", "tenant_status" } }
        },
        {
            "users",
            new Dictionary<string, string>
            {
                { "status", "user_status" },
                { "role", "user_role" }
            }
        },
        {
            "appointments",
            new Dictionary<string, string> { { "status", "appointment_status" } }
        },
        {
            "odontogram_records",
            new Dictionary<string, string> { { "status", "tooth_status" } }
        }
    };

    private void ModifyCommand(DbCommand command)
    {
        if (command.CommandText == null)
        {
            return;
        }

        var cmdText = command.CommandText;
        _logger?.LogDebug("ModifyCommand: Interceptando comando. Tipo: {CommandType}, SQL: {Sql}", command.CommandType, cmdText);

        // Manejar comandos INSERT primero (más específico)
        if (cmdText.Contains("INSERT INTO", StringComparison.OrdinalIgnoreCase))
        {
            _logger?.LogDebug("ModifyCommand: Detectado comando INSERT");
            ModifyInsertCommand(command);
            return;
        }

        // Manejar comandos UPDATE
        if (cmdText.Contains("UPDATE", StringComparison.OrdinalIgnoreCase) && 
            cmdText.Contains("SET", StringComparison.OrdinalIgnoreCase))
        {
            _logger?.LogDebug("ModifyCommand: Detectado comando UPDATE");
            ModifyUpdateCommand(command);
            return;
        }
    }

    private void ModifyInsertCommand(DbCommand command)
    {
        var originalSql = command.CommandText;
        _logger?.LogDebug("ModifyInsertCommand: Procesando SQL: {Sql}", originalSql);

        // Buscar qué tabla se está insertando
        var tableMatch = Regex.Match(originalSql, @"INSERT\s+INTO\s+(\w+)\s*\(", RegexOptions.IgnoreCase);
        if (!tableMatch.Success)
        {
            _logger?.LogDebug("ModifyInsertCommand: No se encontró patrón INSERT INTO table");
            return;
        }

        var tableName = tableMatch.Groups[1].Value.ToLower();
        _logger?.LogDebug("ModifyInsertCommand: Tabla encontrada: {TableName}", tableName);

        if (!TableColumnEnumMap.TryGetValue(tableName, out var columnEnumMap))
        {
            _logger?.LogDebug("ModifyInsertCommand: Tabla {TableName} no tiene enums configurados", tableName);
            return;
        }

        _logger?.LogDebug("ModifyInsertCommand: Tabla {TableName} tiene {Count} columnas con enums", tableName, columnEnumMap.Count);

        // Buscar las columnas en el INSERT
        var columnMatch = Regex.Match(originalSql, $@"INSERT\s+INTO\s+{Regex.Escape(tableName)}\s*\(([^)]+)\)", RegexOptions.IgnoreCase);
        if (!columnMatch.Success)
        {
            _logger?.LogDebug("ModifyInsertCommand: No se encontraron columnas en INSERT");
            return;
        }

        var columns = columnMatch.Groups[1].Value.Split(',').Select(c => c.Trim()).ToArray();
        _logger?.LogDebug("ModifyInsertCommand: {Count} columnas encontradas: {Columns}", columns.Length, string.Join(", ", columns));
        
        // Encontrar la sección VALUES (puede tener RETURNING después)
        var valuesMatch = Regex.Match(originalSql, @"VALUES\s*\(([^)]+)\)", RegexOptions.IgnoreCase);
        if (!valuesMatch.Success)
        {
            _logger?.LogDebug("ModifyInsertCommand: No se encontró sección VALUES");
            return;
        }

        // Extraer los valores, pero considerar que pueden tener paréntesis anidados
        var valuesSection = valuesMatch.Groups[1].Value;
        var values = new List<string>();
        var currentValue = new System.Text.StringBuilder();
        var depth = 0;
        
        foreach (var c in valuesSection)
        {
            if (c == '(') depth++;
            else if (c == ')') depth--;
            else if (c == ',' && depth == 0)
            {
                values.Add(currentValue.ToString().Trim());
                currentValue.Clear();
                continue;
            }
            currentValue.Append(c);
        }
        if (currentValue.Length > 0)
        {
            values.Add(currentValue.ToString().Trim());
        }
        
        var valuesArray = values.ToArray();
        _logger?.LogDebug("ModifyInsertCommand: {Count} valores encontrados", valuesArray.Length);
        
        // Log de todos los parámetros disponibles
        _logger?.LogDebug("ModifyInsertCommand: Parámetros disponibles ({Count}):", command.Parameters.Count);
        for (int i = 0; i < command.Parameters.Count; i++)
        {
            var p = command.Parameters[i];
            _logger?.LogDebug("  [{Index}] Name={Name}, Value={Value}, Type={Type}", i, p.ParameterName, p.Value, p.DbType);
        }
        
        // Procesar cada columna que necesita conversión a enum
        var modifiedValues = valuesArray.ToArray();
        var parametersToRemove = new List<int>();
        
        for (int colIdx = 0; colIdx < columns.Length && colIdx < modifiedValues.Length; colIdx++)
        {
            var columnName = columns[colIdx].ToLower();
            
            if (!columnEnumMap.TryGetValue(columnName, out var enumType))
            {
                continue;
            }

            _logger?.LogDebug("ModifyInsertCommand: Columna {ColumnName} necesita conversión a {EnumType}", columnName, enumType);

            // Obtener el placeholder del parámetro en el SQL
            var paramPlaceholder = modifiedValues[colIdx].Trim();
            
            // Npgsql usa nombres sin @ en la colección (p0, p1) pero con @ en el SQL (@p0, @p1)
            var paramNameWithoutAt = paramPlaceholder.StartsWith("@") 
                ? paramPlaceholder.Substring(1) 
                : paramPlaceholder;
            
            _logger?.LogDebug("ModifyInsertCommand: Buscando parámetro {ParamName}", paramNameWithoutAt);

            // Buscar el parámetro correspondiente
            int paramIndex = -1;
            DbParameter? foundParam = null;
            
            // Extraer el número del parámetro (p0, p1, @p0, @p1, etc.)
            var paramNumberMatch = Regex.Match(paramNameWithoutAt, @"p(\d+)", RegexOptions.IgnoreCase);
            int? paramNumber = null;
            if (paramNumberMatch.Success)
            {
                paramNumber = int.Parse(paramNumberMatch.Groups[1].Value);
                _logger?.LogDebug("ModifyInsertCommand: Número de parámetro extraído: {ParamNumber}", paramNumber);
            }
            
            // Primero intentar con el nombre exacto
            for (int i = 0; i < command.Parameters.Count; i++)
            {
                var param = command.Parameters[i];
                var paramNameNormalized = param.ParameterName.TrimStart('@');
                if (paramNameWithoutAt.Equals(paramNameNormalized, StringComparison.OrdinalIgnoreCase))
                {
                    paramIndex = i;
                    foundParam = param;
                    _logger?.LogDebug("ModifyInsertCommand: Parámetro encontrado en índice {Index} por nombre", i);
                    break;
                }
            }
            
            // Si no se encontró por nombre y tenemos el número, buscar por número
            if (foundParam == null && paramNumber.HasValue)
            {
                for (int i = 0; i < command.Parameters.Count; i++)
                {
                    var param = command.Parameters[i];
                    var paramNumMatch = Regex.Match(param.ParameterName, @"(\d+)");
                    if (paramNumMatch.Success && int.Parse(paramNumMatch.Groups[1].Value) == paramNumber.Value)
                    {
                        paramIndex = i;
                        foundParam = param;
                        _logger?.LogDebug("ModifyInsertCommand: Parámetro encontrado en índice {Index} por número {Number}", i, paramNumber.Value);
                        break;
                    }
                }
            }
            
            if (foundParam != null)
            {
                if (foundParam.Value != null && foundParam.Value != DBNull.Value)
                {
                    var paramValue = foundParam.Value.ToString();
                    if (!string.IsNullOrEmpty(paramValue))
                    {
                        _logger?.LogInformation("ModifyInsertCommand: Valor del parámetro {ParamName}: {Value}, convirtiendo a enum {EnumType}", 
                            paramNameWithoutAt, paramValue, enumType);
                        // Escapar el valor para evitar SQL injection
                        var escapedValue = paramValue.Replace("'", "''");
                        // Usar la sintaxis correcta de PostgreSQL: 'valor'::enum_type
                        modifiedValues[colIdx] = $"'{escapedValue}'::{enumType}";
                        parametersToRemove.Add(paramIndex);
                    }
                }
                else
                {
                    _logger?.LogDebug("ModifyInsertCommand: Parámetro {ParamName} es null o DBNull", paramNameWithoutAt);
                }
            }
            else
            {
                _logger?.LogWarning("ModifyInsertCommand: No se encontró el parámetro {ParamName}", paramNameWithoutAt);
            }
        }
        
        // Si se modificó algo, actualizar el comando SQL
        if (parametersToRemove.Count > 0)
        {
            _logger?.LogInformation("ModifyInsertCommand: Modificando {Count} parámetros de enum", parametersToRemove.Count);
            var newValues = string.Join(", ", modifiedValues);
            var newSql = Regex.Replace(
                originalSql,
                @"VALUES\s*\([^)]+\)",
                $"VALUES ({newValues})",
                RegexOptions.IgnoreCase);
            
            _logger?.LogInformation("ModifyInsertCommand: SQL original: {OriginalSql}", originalSql);
            _logger?.LogInformation("ModifyInsertCommand: SQL modificado: {NewSql}", newSql);
            command.CommandText = newSql;
            
            // Remover los parámetros por índice, en orden descendente para evitar problemas de índices
            foreach (var index in parametersToRemove.OrderByDescending(i => i))
            {
                if (index >= 0 && index < command.Parameters.Count)
                {
                    _logger?.LogDebug("ModifyInsertCommand: Removiendo parámetro en índice {Index}", index);
                    command.Parameters.RemoveAt(index);
                }
            }
        }
        else
        {
            _logger?.LogDebug("ModifyInsertCommand: No se modificó ningún parámetro");
        }
    }

    private void ModifyUpdateCommand(DbCommand command)
    {
        var originalSql = command.CommandText;
        _logger?.LogDebug("ModifyUpdateCommand: Procesando SQL: {Sql}", originalSql);

        // Buscar qué tabla se está actualizando (más flexible con espacios)
        var tableMatch = Regex.Match(originalSql, @"UPDATE\s+(\w+)\s+SET", RegexOptions.IgnoreCase);
        if (!tableMatch.Success)
        {
            _logger?.LogWarning("ModifyUpdateCommand: No se encontró patrón UPDATE table SET. SQL: {Sql}", originalSql);
            return;
        }

        var tableName = tableMatch.Groups[1].Value.ToLower();
        _logger?.LogDebug("ModifyUpdateCommand: Tabla encontrada: {TableName}", tableName);
        
        if (!TableColumnEnumMap.TryGetValue(tableName, out var columnEnumMap))
        {
            _logger?.LogDebug("ModifyUpdateCommand: Tabla {TableName} no tiene enums configurados", tableName);
            return;
        }
        
        _logger?.LogDebug("ModifyUpdateCommand: Tabla {TableName} tiene {Count} columnas con enums", tableName, columnEnumMap.Count);

        // Encontrar la posición de SET y WHERE
        var setIndex = originalSql.IndexOf("SET", StringComparison.OrdinalIgnoreCase);
        var whereIndex = originalSql.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase);
        
        if (setIndex < 0 || whereIndex < 0 || whereIndex <= setIndex)
        {
            _logger?.LogDebug("ModifyUpdateCommand: No se encontraron SET o WHERE correctamente. SET={SetIndex}, WHERE={WhereIndex}", setIndex, whereIndex);
            return;
        }

        // Extraer la sección SET
        var setClause = originalSql.Substring(setIndex + 3, whereIndex - setIndex - 3).Trim();
        _logger?.LogDebug("ModifyUpdateCommand: SET clause: {SetClause}", setClause);
        var setParts = setClause.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToArray();
        _logger?.LogDebug("ModifyUpdateCommand: {Count} partes en SET", setParts.Length);
        
        // Log de todos los parámetros disponibles
        _logger?.LogDebug("ModifyUpdateCommand: Parámetros disponibles ({Count}):", command.Parameters.Count);
        for (int i = 0; i < command.Parameters.Count; i++)
        {
            var p = command.Parameters[i];
            _logger?.LogDebug("  [{Index}] Name={Name}, Value={Value}, Type={Type}", i, p.ParameterName, p.Value, p.DbType);
        }
        
        // Procesar cada asignación en el SET
        var modifiedSetParts = new List<string>();
        var parametersToRemove = new List<int>();

        foreach (var setPart in setParts)
        {
            // Formato esperado: "column_name = @paramName" o "column_name = @p0"
            var assignmentMatch = Regex.Match(setPart, @"^(\w+)\s*=\s*(@?\w+)$", RegexOptions.IgnoreCase);
            if (!assignmentMatch.Success)
            {
                modifiedSetParts.Add(setPart);
                continue;
            }

            var columnName = assignmentMatch.Groups[1].Value.ToLower();
            var paramPlaceholder = assignmentMatch.Groups[2].Value;

            // Verificar si esta columna necesita conversión a enum
            if (columnEnumMap.TryGetValue(columnName, out var enumType))
            {
                _logger?.LogDebug("ModifyUpdateCommand: Columna {ColumnName} necesita conversión a {EnumType}", columnName, enumType);
                
                // Extraer el número del parámetro (p0, p1, @p0, @p1, etc.)
                var paramNumberMatch = Regex.Match(paramPlaceholder, @"p(\d+)", RegexOptions.IgnoreCase);
                if (!paramNumberMatch.Success)
                {
                    _logger?.LogWarning("ModifyUpdateCommand: No se pudo extraer el número del parámetro {ParamName}", paramPlaceholder);
                    modifiedSetParts.Add(setPart);
                    continue;
                }

                var paramNumber = int.Parse(paramNumberMatch.Groups[1].Value);
                _logger?.LogDebug("ModifyUpdateCommand: Número de parámetro extraído: {ParamNumber}", paramNumber);

                // Buscar el parámetro por índice o por nombre
                DbParameter? foundParam = null;
                int paramIndex = -1;
                
                // Primero intentar por nombre (p0, p1, etc.)
                var paramNameWithoutAt = paramPlaceholder.StartsWith("@") 
                    ? paramPlaceholder.Substring(1) 
                    : paramPlaceholder;
                
                for (int i = 0; i < command.Parameters.Count; i++)
                {
                    var param = command.Parameters[i];
                    // Buscar por nombre exacto o por número en el nombre
                    if (paramNameWithoutAt.Equals(param.ParameterName, StringComparison.OrdinalIgnoreCase) ||
                        param.ParameterName.Equals($"p{paramNumber}", StringComparison.OrdinalIgnoreCase))
                    {
                        paramIndex = i;
                        foundParam = param;
                        _logger?.LogDebug("ModifyUpdateCommand: Parámetro encontrado en índice {Index} por nombre", i);
                        break;
                    }
                }

                // Si no se encontró por nombre, intentar por índice (asumiendo que el orden coincide)
                if (foundParam == null && paramNumber < command.Parameters.Count)
                {
                    // Buscar parámetros que contengan el número en su nombre
                    for (int i = 0; i < command.Parameters.Count; i++)
                    {
                        var param = command.Parameters[i];
                        var paramNumMatch = Regex.Match(param.ParameterName, @"(\d+)");
                        if (paramNumMatch.Success && int.Parse(paramNumMatch.Groups[1].Value) == paramNumber)
                        {
                            paramIndex = i;
                            foundParam = param;
                            _logger?.LogDebug("ModifyUpdateCommand: Parámetro encontrado en índice {Index} por número", i);
                            break;
                        }
                    }
                }

                if (foundParam != null && foundParam.Value != null && foundParam.Value != DBNull.Value)
                {
                    var paramValue = foundParam.Value.ToString();
                    if (!string.IsNullOrEmpty(paramValue))
                    {
                        _logger?.LogInformation("ModifyUpdateCommand: Valor del parámetro: {Value}, convirtiendo a enum {EnumType}", paramValue, enumType);
                        // Escapar el valor para evitar SQL injection
                        var escapedValue = paramValue.Replace("'", "''");
                        // Usar la sintaxis correcta de PostgreSQL: 'valor'::enum_type
                        modifiedSetParts.Add($"{columnName} = '{escapedValue}'::{enumType}");
                        parametersToRemove.Add(paramIndex);
                        continue;
                    }
                }
                else
                {
                    _logger?.LogWarning("ModifyUpdateCommand: No se encontró el parámetro {ParamName} (número: {ParamNumber}). Total de parámetros: {Count}", 
                        paramNameWithoutAt, paramNumber, command.Parameters.Count);
                }
            }

            // Si no se procesó, mantener la asignación original
            modifiedSetParts.Add(setPart);
        }

        // Si se modificó algo, actualizar el comando SQL
        if (parametersToRemove.Count > 0)
        {
            _logger?.LogInformation("ModifyUpdateCommand: Modificando {Count} parámetros de enum", parametersToRemove.Count);
            var newSetClause = string.Join(", ", modifiedSetParts);
            var beforeSet = originalSql.Substring(0, setIndex + 3);
            var afterWhere = originalSql.Substring(whereIndex);
            var newSql = $"{beforeSet} {newSetClause} {afterWhere}";
            _logger?.LogInformation("ModifyUpdateCommand: SQL original: {OriginalSql}", originalSql);
            _logger?.LogInformation("ModifyUpdateCommand: SQL modificado: {NewSql}", newSql);
            command.CommandText = newSql;

            // Remover los parámetros por índice, en orden descendente
            foreach (var index in parametersToRemove.OrderByDescending(i => i))
            {
                if (index >= 0 && index < command.Parameters.Count)
                {
                    _logger?.LogDebug("ModifyUpdateCommand: Removiendo parámetro en índice {Index}", index);
                    command.Parameters.RemoveAt(index);
                }
            }
        }
        else
        {
            _logger?.LogWarning("ModifyUpdateCommand: No se modificó ningún parámetro. Total de partes procesadas: {Count}", modifiedSetParts.Count);
        }
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        ModifyCommand(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        ModifyCommand(command);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    // Interceptar comandos de escritura (INSERT, UPDATE, DELETE)
    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        ModifyCommand(command);
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ModifyCommand(command);
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }
}
