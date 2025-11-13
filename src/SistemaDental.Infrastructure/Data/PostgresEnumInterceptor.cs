using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;

namespace SistemaDental.Infrastructure.Data;

/// <summary>
/// Interceptor para manejar conversiones de enums de PostgreSQL en comandos INSERT.
/// Convierte automáticamente valores de texto a enums de PostgreSQL para columnas que usan enums.
/// </summary>
public class PostgresEnumInterceptor : DbCommandInterceptor
{
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
        if (!command.CommandText.Contains("INSERT INTO", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Buscar qué tabla se está insertando
        var tableMatch = Regex.Match(command.CommandText, @"INSERT INTO\s+(\w+)\s*\(", RegexOptions.IgnoreCase);
        if (!tableMatch.Success)
        {
            return;
        }

        var tableName = tableMatch.Groups[1].Value.ToLower();
        if (!TableColumnEnumMap.TryGetValue(tableName, out var columnEnumMap))
        {
            return;
        }

        // Buscar las columnas en el INSERT
        var columnMatch = Regex.Match(command.CommandText, $@"INSERT INTO\s+{Regex.Escape(tableName)}\s*\(([^)]+)\)", RegexOptions.IgnoreCase);
        if (!columnMatch.Success)
        {
            return;
        }

        var columns = columnMatch.Groups[1].Value.Split(',').Select(c => c.Trim()).ToArray();
        
        // Encontrar la sección VALUES
        var valuesMatch = Regex.Match(command.CommandText, @"VALUES\s*\(([^)]+)\)", RegexOptions.IgnoreCase);
        if (!valuesMatch.Success)
        {
            return;
        }

        var values = valuesMatch.Groups[1].Value.Split(',').Select(v => v.Trim()).ToArray();
        
        // Procesar cada columna que necesita conversión a enum
        var modifiedValues = values.ToArray();
        var parametersToRemove = new List<int>(); // Índices de parámetros a remover
        
        foreach (var columnEnum in columnEnumMap)
        {
            var columnName = columnEnum.Key;
            var enumType = columnEnum.Value;
            var columnIndex = Array.IndexOf(columns, columnName);
            
            if (columnIndex < 0 || columnIndex >= values.Length)
            {
                continue;
            }

            // Obtener el placeholder del parámetro en el SQL
            var paramPlaceholder = values[columnIndex].Trim();
            
            // Buscar el parámetro correspondiente por nombre exacto y obtener su índice
            int paramIndex = -1;
            for (int i = 0; i < command.Parameters.Count; i++)
            {
                var param = command.Parameters[i];
                if (paramPlaceholder.Equals(param.ParameterName, StringComparison.OrdinalIgnoreCase))
                {
                    paramIndex = i;
                    break;
                }
            }
            
            if (paramIndex >= 0)
            {
                var param = command.Parameters[paramIndex];
                if (param.Value != null)
                {
                    var paramValue = param.Value.ToString();
                    if (!string.IsNullOrEmpty(paramValue))
                    {
                        // Escapar el valor para evitar SQL injection
                        var escapedValue = paramValue.Replace("'", "''");
                        // Reemplazar el placeholder del parámetro con el valor directo con cast
                        modifiedValues[columnIndex] = $"CAST('{escapedValue}'::text AS {enumType})";
                        parametersToRemove.Add(paramIndex);
                    }
                }
            }
        }
        
        // Si se modificó algo, actualizar el comando SQL
        if (parametersToRemove.Count > 0)
        {
            var newValues = string.Join(", ", modifiedValues);
            command.CommandText = Regex.Replace(
                command.CommandText,
                @"VALUES\s*\([^)]+\)",
                $"VALUES ({newValues})",
                RegexOptions.IgnoreCase);
            
            // Remover los parámetros por índice, en orden descendente para evitar problemas de índices
            foreach (var index in parametersToRemove.OrderByDescending(i => i))
            {
                if (index >= 0 && index < command.Parameters.Count)
                {
                    command.Parameters.RemoveAt(index);
                }
            }
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
}

