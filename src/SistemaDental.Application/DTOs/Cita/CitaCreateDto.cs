namespace SistemaDental.Application.DTOs.Cita;

public class CitaCreateDto
{
    public Guid PacienteId { get; set; }
    public Guid UsuarioId { get; set; } // Requerido en BD
    public DateOnly AppointmentDate { get; set; }
    public TimeOnly StartTime { get; set; }
    // Propiedad de compatibilidad - se convierte a AppointmentDate y StartTime
    // Si se proporciona FechaHora, tiene prioridad sobre AppointmentDate y StartTime
    public DateTime? FechaHora
    {
        get => AppointmentDate != default && StartTime != default 
            ? AppointmentDate.ToDateTime(StartTime) 
            : null;
        set
        {
            if (value.HasValue)
            {
                AppointmentDate = DateOnly.FromDateTime(value.Value);
                StartTime = TimeOnly.FromDateTime(value.Value);
            }
        }
    }
    public int DuracionMinutos { get; set; } = 30;
    public string Motivo { get; set; } = string.Empty; // Requerido en BD
    public string? Observaciones { get; set; }
}

