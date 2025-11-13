namespace SistemaDental.Application.DTOs.Odontograma;

public class OdontogramaDto
{
    public Guid Id { get; set; }
    public Guid PacienteId { get; set; }
    public int NumeroDiente { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public DateOnly FechaRegistro { get; set; }
    // Propiedad calculada para compatibilidad
    public DateTime FechaRegistroDateTime => FechaRegistro.ToDateTime(TimeOnly.MinValue);
    public Guid UsuarioId { get; set; } // Requerido
    public string UsuarioNombre { get; set; } = string.Empty;
}

