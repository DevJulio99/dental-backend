namespace SistemaDental.Application.DTOs.Odontograma;

public class OdontogramaCreateDto
{
    public Guid PacienteId { get; set; }
    public int NumeroDiente { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public DateOnly? FechaRegistro { get; set; } // Opcional, por defecto hoy
    // Propiedad de compatibilidad
    public DateTime? FechaRegistroDateTime
    {
        get => FechaRegistro?.ToDateTime(TimeOnly.MinValue);
        set => FechaRegistro = value.HasValue ? DateOnly.FromDateTime(value.Value) : null;
    }
}

