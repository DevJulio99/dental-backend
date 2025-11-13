namespace SistemaDental.Application.DTOs.Paciente;

public class PacienteDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    // Propiedad calculada para compatibilidad
    public string NombreCompleto => $"{FirstName} {LastName}".Trim();
    public string DniPasaporte { get; set; } = string.Empty;
    public DateTime FechaNacimiento { get; set; }
    public string Telefono { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public string? Alergias { get; set; }
    public string? Observaciones { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaUltimaCita { get; set; }
}

