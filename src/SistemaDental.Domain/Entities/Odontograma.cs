using SistemaDental.Domain.Enums;

namespace SistemaDental.Domain.Entities;

public class Odontograma
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PacienteId { get; set; }
    public int NumeroDiente { get; set; } // Número del diente según numeración dental (11-18, 21-28, 31-38, 41-48)
    public ToothStatus Estado { get; set; } = ToothStatus.Healthy; // Estado del diente usando enum
    public string? Observaciones { get; set; }
    public DateOnly FechaRegistro { get; set; } // record_date en BD es date
    // Propiedad calculada para compatibilidad
    public DateTime FechaRegistroDateTime
    {
        get => FechaRegistro.ToDateTime(TimeOnly.MinValue);
        set => FechaRegistro = DateOnly.FromDateTime(value);
    }
    public Guid UsuarioId { get; set; } // Usuario que registró el estado (recorded_by, requerido)
    public Guid? ClinicalRecordId { get; set; } // Relación con registro clínico
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // created_at en BD
    
    // Relaciones
    public Tenant Tenant { get; set; } = null!;
    public Paciente Paciente { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
}

