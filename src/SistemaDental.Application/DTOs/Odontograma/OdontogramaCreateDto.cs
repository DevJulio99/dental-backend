using SistemaDental.Domain.Enums;

namespace SistemaDental.Application.DTOs.Odontograma;

public class OdontogramaCreateDto
{
    public Guid PacienteId { get; set; }
    public int NumeroDiente { get; set; }
    // Acepta tanto enum como string (en español o inglés) para compatibilidad con frontend
    public string Estado { get; set; } = string.Empty;
    // Propiedad calculada que convierte el string a enum
    public ToothStatus EstadoEnum => ToothStatusExtensions.FromSpanish(Estado);
    public string? Observaciones { get; set; }
    public DateOnly? FechaRegistro { get; set; } // Opcional, por defecto hoy
    // Propiedad de compatibilidad
    public DateTime? FechaRegistroDateTime
    {
        get => FechaRegistro?.ToDateTime(TimeOnly.MinValue);
        set => FechaRegistro = value.HasValue ? DateOnly.FromDateTime(value.Value) : null;
    }
}

