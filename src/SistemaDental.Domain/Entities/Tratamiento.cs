namespace SistemaDental.Domain.Entities;

public class Tratamiento
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PacienteId { get; set; }
    public Guid? CitaId { get; set; }
    public Guid UsuarioId { get; set; } // Odontólogo que realizó el tratamiento (dentist_id)
    public Guid? TreatmentId { get; set; } // Referencia al catálogo de tratamientos
    public DateOnly TreatmentDate { get; set; }
    // Propiedad calculada para compatibilidad
    public DateTime FechaRealizacion
    {
        get => TreatmentDate.ToDateTime(TimeOnly.MinValue);
        set => TreatmentDate = DateOnly.FromDateTime(value);
    }
    public string? Diagnosis { get; set; }
    public string TreatmentPerformed { get; set; } = string.Empty; // Nombre del tratamiento realizado
    public string? Observaciones { get; set; }
    public decimal? Costo { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Relaciones
    public Tenant Tenant { get; set; } = null!;
    public Paciente Paciente { get; set; } = null!;
    public Cita? Cita { get; set; }
    public Usuario Usuario { get; set; } = null!;
}

