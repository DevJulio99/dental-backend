using System.ComponentModel.DataAnnotations;

namespace SistemaDental.Application.DTOs.ScheduleConfig
{
    public class ScheduleConfigRequestDto
    {
        [Required]
        public string Subdomain { get; set; }
        public Guid? UsuarioId { get; set; }
    }
}