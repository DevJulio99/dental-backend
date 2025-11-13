using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaDental.Application.Services;

namespace SistemaDental.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportesController : ControllerBase
{
    private readonly IReporteService _reporteService;
    private readonly ILogger<ReportesController> _logger;

    public ReportesController(IReporteService reporteService, ILogger<ReportesController> logger)
    {
        _reporteService = reporteService;
        _logger = logger;
    }

    [HttpGet("citas")]
    public async Task<ActionResult> GetReporteCitas(
        [FromQuery] DateTime? fechaInicio = null,
        [FromQuery] DateTime? fechaFin = null)
    {
        try
        {
            var reporte = await _reporteService.GetReporteCitasAsync(fechaInicio, fechaFin);
            return Ok(reporte);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar reporte de citas");
            return StatusCode(500, new { message = "Error al generar reporte" });
        }
    }

    [HttpGet("tratamientos")]
    public async Task<ActionResult> GetReporteTratamientos(
        [FromQuery] DateTime? fechaInicio = null,
        [FromQuery] DateTime? fechaFin = null)
    {
        try
        {
            var reporte = await _reporteService.GetReporteTratamientosAsync(fechaInicio, fechaFin);
            return Ok(reporte);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar reporte de tratamientos");
            return StatusCode(500, new { message = "Error al generar reporte" });
        }
    }

    [HttpGet("pacientes/total")]
    public async Task<ActionResult> GetTotalPacientes()
    {
        try
        {
            var total = await _reporteService.GetTotalPacientesAsync();
            return Ok(new { total });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener total de pacientes");
            return StatusCode(500, new { message = "Error al obtener total" });
        }
    }

    [HttpGet("pacientes/por-mes")]
    public async Task<ActionResult> GetPacientesPorMes([FromQuery] int año)
    {
        if (año < 2000 || año > 2100)
        {
            return BadRequest(new { message = "Año inválido" });
        }

        try
        {
            var datos = await _reporteService.GetPacientesPorMesAsync(año);
            return Ok(datos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener pacientes por mes");
            return StatusCode(500, new { message = "Error al obtener datos" });
        }
    }
}

