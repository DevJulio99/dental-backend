namespace SistemaDental.Application.DTOs.Cita;

public class CitaDto
{
    public Guid Id { get; set; }
    public Guid PacienteId { get; set; }
    public string PacienteNombre { get; set; } = string.Empty;
    public Guid UsuarioId { get; set; } // Requerido
    public string UsuarioNombre { get; set; } = string.Empty;
    public DateOnly AppointmentDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    // Propiedad calculada para compatibilidad
    public DateTime FechaHora => AppointmentDate.ToDateTime(StartTime);
    public int DuracionMinutos { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public bool NotificationSent { get; set; }
    public bool ReminderSent { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime FechaCreacion { get; set; }
}

