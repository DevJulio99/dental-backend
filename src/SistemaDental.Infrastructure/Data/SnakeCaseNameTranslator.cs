namespace SistemaDental.Infrastructure.Data;

/// <summary>
/// Traductor de nombres para convertir de PascalCase (C#) a snake_case (PostgreSQL)
/// Implementa la interfaz requerida por Npgsql para traducción de nombres
/// </summary>
public class SnakeCaseNameTranslator : Npgsql.INpgsqlNameTranslator
{
    public string TranslateTypeName(string clrName) => ToSnakeCase(clrName);
    
    public string TranslateMemberName(string clrName) => ToSnakeCase(clrName);

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = new System.Text.StringBuilder();
        result.Append(char.ToLowerInvariant(input[0]));

        for (int i = 1; i < input.Length; i++)
        {
            if (char.IsUpper(input[i]))
            {
                result.Append('_');
                result.Append(char.ToLowerInvariant(input[i]));
            }
            else
            {
                result.Append(input[i]);
            }
        }

        return result.ToString();
    }
}
