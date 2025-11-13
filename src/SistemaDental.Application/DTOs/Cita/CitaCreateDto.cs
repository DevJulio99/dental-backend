namespace SistemaDental.Application.DTOs.Cita;

public class CitaCreateDto
{
    public Guid PacienteId { get; set; }
    public Guid UsuarioId { get; set; } // Requerido en BD
    public DateOnly AppointmentDate { get; set; }
    public TimeOnly StartTime { get; set; }
    // Propiedad de compatibilidad - se convierte a AppointmentDate y StartTime
    public DateTime FechaHora
    {
        get => AppointmentDate.ToDateTime(StartTime);
        set
        {
            AppointmentDate = DateOnly.FromDateTime(value);
            StartTime = TimeOnly.FromDateTime(value);
        }
    }
    public int DuracionMinutos { get; set; } = 30;
    public string Motivo { get; set; } = string.Empty; // Requerido en BD
    public string? Observaciones { get; set; }
}

