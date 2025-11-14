using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaDental.Application.DTOs.Odontograma;
using SistemaDental.Application.Services;

namespace SistemaDental.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OdontogramasController : ControllerBase
{
    private readonly IOdontogramaService _odontogramaService;
    private readonly ILogger<OdontogramasController> _logger;

    public OdontogramasController(IOdontogramaService odontogramaService, ILogger<OdontogramasController> logger)
    {
        _odontogramaService = odontogramaService;
        _logger = logger;
    }

    [HttpGet("paciente/{pacienteId}")]
    public async Task<ActionResult<IEnumerable<OdontogramaDto>>> GetByPaciente(
        Guid pacienteId,
        [FromQuery] DateOnly? fechaDesde = null,
        [FromQuery] DateOnly? fechaHasta = null)
    {
        try
        {
            IEnumerable<OdontogramaDto> odontogramas;
            
            if (fechaDesde.HasValue || fechaHasta.HasValue)
            {
                odontogramas = await _odontogramaService.GetByPacienteConFiltrosAsync(pacienteId, fechaDesde, fechaHasta);
            }
            else
            {
                odontogramas = await _odontogramaService.GetByPacienteAsync(pacienteId);
            }
            
            return Ok(odontogramas);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error de validación al obtener odontogramas");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener odontogramas");
            return StatusCode(500, new { message = "Error al obtener odontogramas" });
        }
    }

    [HttpGet("paciente/{pacienteId}/estado-actual")]
    public async Task<ActionResult<Dictionary<int, OdontogramaDto?>>> GetEstadoActualDientes(Guid pacienteId)
    {
        try
        {
            var estados = await _odontogramaService.GetEstadoActualDientesAsync(pacienteId);
            return Ok(estados);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener estado actual de dientes");
            return StatusCode(500, new { message = "Error al obtener estado de dientes" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<OdontogramaDto>> Create([FromBody] OdontogramaCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var odontograma = await _odontogramaService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetByPaciente), new { pacienteId = dto.PacienteId }, odontograma);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear odontograma");
            return StatusCode(500, new { message = "Error al crear odontograma" });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<OdontogramaDto>> Update(Guid id, [FromBody] OdontogramaCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var odontograma = await _odontogramaService.UpdateAsync(id, dto);
            if (odontograma == null)
            {
                return NotFound(new { message = "Odontograma no encontrado" });
            }
            return Ok(odontograma);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error de validación al actualizar odontograma");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar odontograma");
            return StatusCode(500, new { message = "Error al actualizar odontograma" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _odontogramaService.DeleteAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Odontograma no encontrado" });
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar odontograma");
            return StatusCode(500, new { message = "Error al eliminar odontograma" });
        }
    }

    [HttpGet("paciente/{pacienteId}/diente/{numeroDiente}/historial")]
    public async Task<ActionResult<IEnumerable<OdontogramaDto>>> GetHistorialByDiente(Guid pacienteId, int numeroDiente)
    {
        try
        {
            var historial = await _odontogramaService.GetHistorialByDienteAsync(pacienteId, numeroDiente);
            return Ok(historial);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error de validación al obtener historial de diente");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener historial de diente");
            return StatusCode(500, new { message = "Error al obtener historial de diente" });
        }
    }

    [HttpGet("paciente/{pacienteId}/historial-agrupado")]
    public async Task<ActionResult<Dictionary<int, IEnumerable<OdontogramaDto>>>> GetHistorialAgrupado(Guid pacienteId)
    {
        try
        {
            var historial = await _odontogramaService.GetHistorialAgrupadoAsync(pacienteId);
            return Ok(historial);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener historial agrupado");
            return StatusCode(500, new { message = "Error al obtener historial agrupado" });
        }
    }

    [HttpGet("paciente/{pacienteId}/estado-en-fecha")]
    public async Task<ActionResult<Dictionary<int, OdontogramaDto?>>> GetEstadoDientesEnFecha(
        Guid pacienteId,
        [FromQuery] DateOnly fecha)
    {
        try
        {
            var estados = await _odontogramaService.GetEstadoDientesEnFechaAsync(pacienteId, fecha);
            return Ok(estados);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener estado de dientes en fecha");
            return StatusCode(500, new { message = "Error al obtener estado de dientes en fecha" });
        }
    }

    [HttpPost("batch")]
    public async Task<ActionResult<IEnumerable<OdontogramaDto>>> CreateBatch([FromBody] IEnumerable<OdontogramaCreateDto> dtos)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var odontogramas = await _odontogramaService.CreateBatchAsync(dtos);
            return CreatedAtAction(nameof(GetByPaciente), new { pacienteId = dtos.FirstOrDefault()?.PacienteId }, odontogramas);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error de validación al crear odontogramas en batch");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear odontogramas en batch");
            return StatusCode(500, new { message = "Error al crear odontogramas en batch" });
        }
    }
}

