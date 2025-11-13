using System.Text.Json.Serialization;

namespace SistemaDental.Application.DTOs.Paciente;

public class PacienteCreateDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    // Propiedad de compatibilidad - se divide en FirstName y LastName
    // Oculto de la serialización JSON para evitar confusión en Swagger
    // Solo necesitas enviar firstName y lastName
    [JsonIgnore]
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
    public string TipoDocumento { get; set; } = "DNI";
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
}

