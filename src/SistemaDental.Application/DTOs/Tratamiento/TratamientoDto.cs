namespace SistemaDental.Application.DTOs.Tratamiento;

public class TratamientoDto
{
    public Guid Id { get; set; }
    public Guid PacienteId { get; set; }
    public string PacienteNombre { get; set; } = string.Empty;
    public Guid? CitaId { get; set; }
    public Guid UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public Guid? TreatmentId { get; set; }
    public string TreatmentPerformed { get; set; } = string.Empty;
    public string? Diagnosis { get; set; }
    public decimal? Costo { get; set; }
    public DateOnly TreatmentDate { get; set; }
    // Propiedad calculada para compatibilidad
    public DateTime FechaRealizacion => TreatmentDate.ToDateTime(TimeOnly.MinValue);
    public string? Observaciones { get; set; }
    // Propiedades de compatibilidad
    public string Nombre => TreatmentPerformed;
    public string? Descripcion { get; set; } // No existe en clinical_records
    public string Estado { get; set; } = string.Empty; // No existe en clinical_records
}

