using SistemaDental.Domain.Enums;

namespace SistemaDental.Domain.Entities;

public class Cita
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PacienteId { get; set; }
    public Guid UsuarioId { get; set; } // Odontólogo asignado (requerido en BD)
    public DateOnly AppointmentDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    // Propiedad calculada para compatibilidad
    public DateTime FechaHora
    {
        get => AppointmentDate.ToDateTime(StartTime);
        set
        {
            AppointmentDate = DateOnly.FromDateTime(value);
            StartTime = TimeOnly.FromDateTime(value);
            EndTime = StartTime.AddMinutes(DuracionMinutos);
        }
    }
    public int DuracionMinutos { get; set; } = 30;
    public AppointmentStatus Estado { get; set; } = AppointmentStatus.Scheduled;
    public string Motivo { get; set; } = string.Empty; // Requerido en BD
    public string? Observaciones { get; set; }
    public bool NotificationSent { get; set; } = false;
    public bool ReminderSent { get; set; } = false;
    public string? CancellationReason { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public Guid? CancelledBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    
    // Relaciones
    public Tenant Tenant { get; set; } = null!;
    public Paciente Paciente { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
    public ICollection<Tratamiento> Tratamientos { get; set; } = new List<Tratamiento>();
}

