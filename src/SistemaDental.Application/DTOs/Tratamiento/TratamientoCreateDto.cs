namespace SistemaDental.Application.DTOs.Tratamiento;

public class TratamientoCreateDto
{
    public Guid PacienteId { get; set; }
    public Guid? CitaId { get; set; }
    public Guid? TreatmentId { get; set; } // Referencia al catálogo de tratamientos
    public string TreatmentPerformed { get; set; } = string.Empty; // Nombre del tratamiento realizado
    public string? Diagnosis { get; set; }
    public decimal? Costo { get; set; }
    public string? Observaciones { get; set; }
    public DateOnly? TreatmentDate { get; set; } // Opcional, por defecto hoy
    // Propiedades de compatibilidad
    public string Nombre
    {
        get => TreatmentPerformed;
        set => TreatmentPerformed = value;
    }
    public string? Descripcion { get; set; } // No se usa en clinical_records
    public DateTime? FechaRealizacion
    {
        get => TreatmentDate?.ToDateTime(TimeOnly.MinValue);
        set => TreatmentDate = value.HasValue ? DateOnly.FromDateTime(value.Value) : null;
    }
}

