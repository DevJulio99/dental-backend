namespace SistemaDental.Application.DTOs.Paciente;

public class PacienteDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    // Propiedad calculada para compatibilidad
    public string NombreCompleto => $"{FirstName} {LastName}".Trim();
    public string TipoDocumento { get; set; } = string.Empty;
    public string DniPasaporte { get; set; } = string.Empty;
    public DateTime FechaNacimiento { get; set; }
    public string? Genero { get; set; }
    public string Telefono { get; set; } = string.Empty;
    public string? TelefonoAlternativo { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public string? Ciudad { get; set; }
    public string? TipoSangre { get; set; }
    public string? Alergias { get; set; }
    public string? CondicionesMedicas { get; set; }
    public string? MedicamentosActuales { get; set; }
    public string? ContactoEmergenciaNombre { get; set; }
    public string? ContactoEmergenciaTelefono { get; set; }
    public string? SeguroDental { get; set; }
    public string? NumeroSeguro { get; set; }
    public string? FotoUrl { get; set; }
    public string? Observaciones { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaUltimaCita { get; set; }
}

