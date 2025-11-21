using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaDental.Application.DTOs.ScheduleConfig;
using SistemaDental.Application.Services;
using SistemaDental.Infrastructure.Services;

namespace SistemaDental.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class ScheduleConfigController : ControllerBase
    {
        private readonly IScheduleConfigService _scheduleConfigService;
        private readonly ITenantService _tenantService;
        private readonly ILogger<ScheduleConfigController> _logger;

        public ScheduleConfigController(
            IScheduleConfigService scheduleConfigService,
            ITenantService tenantService,
            ILogger<ScheduleConfigController> logger)
        {
            _scheduleConfigService = scheduleConfigService;
            _tenantService = tenantService;
            _logger = logger;
        }

        [HttpPost("upsert")]
        public async Task<IActionResult> UpsertSchedule([FromBody] UpsertScheduleConfigRequestDto request)
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return Unauthorized(new { message = "Tenant no identificado." });
            }

            await _scheduleConfigService.UpsertScheduleAsync(tenantId.Value, request.UsuarioId, request.Configurations);

            return Ok(new { message = "Configuración de horario guardada exitosamente." });
        }
    }
}