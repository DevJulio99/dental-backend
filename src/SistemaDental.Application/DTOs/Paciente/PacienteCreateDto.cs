namespace SistemaDental.Application.DTOs.Paciente;

public class PacienteCreateDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    // Propiedad de compatibilidad - se divide en FirstName y LastName
    public string NombreCompleto
    {
        get => $"{FirstName} {LastName}".Trim();
        set
        {
            var parts = value.Split(' ', 2);
            FirstName = parts.Length > 0 ? parts[0] : string.Empty;
            LastName = parts.Length > 1 ? parts[1] : string.Empty;
        }
    }
    public string DniPasaporte { get; set; } = string.Empty;
    public DateTime FechaNacimiento { get; set; }
    public string Telefono { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public string? Alergias { get; set; }
    public string? Observaciones { get; set; }
}

