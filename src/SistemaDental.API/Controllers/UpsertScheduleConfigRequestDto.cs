namespace SistemaDental.Application.DTOs.ScheduleConfig
{
    public class UpsertScheduleConfigRequestDto
    {
        public Guid? UsuarioId { get; set; } // Nulo para la configuración general del tenant
        public List<ScheduleConfigUpsertDto> Configurations { get; set; } = new List<ScheduleConfigUpsertDto>();
    }
}