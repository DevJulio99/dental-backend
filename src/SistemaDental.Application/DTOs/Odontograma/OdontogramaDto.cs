using SistemaDental.Domain.Enums;
using System.Text.Json.Serialization;

namespace SistemaDental.Application.DTOs.Odontograma;

public class OdontogramaDto
{
    public Guid Id { get; set; }
    public Guid PacienteId { get; set; }
    public int NumeroDiente { get; set; }
    
    // Estado como enum (para uso interno)
    [JsonIgnore]
    public ToothStatus Estado { get; set; }
    
    // Estado serializado como string en español para el frontend
    [JsonPropertyName("estado")]
    public string EstadoEspañol => Estado.ToSpanish();
    
    // Nombre legible del estado
    [JsonPropertyName("estadoNombre")]
    public string EstadoNombre => Estado.GetDisplayName();
    
    public string? Observaciones { get; set; }
    public DateOnly FechaRegistro { get; set; }
    // Propiedad calculada para compatibilidad
    public DateTime FechaRegistroDateTime => FechaRegistro.ToDateTime(TimeOnly.MinValue);
    public Guid UsuarioId { get; set; } // Requerido
    public string UsuarioNombre { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
