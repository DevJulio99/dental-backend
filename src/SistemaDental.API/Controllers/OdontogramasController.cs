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
    public async Task<ActionResult<IEnumerable<OdontogramaDto>>> GetByPaciente(Guid pacienteId)
    {
        try
        {
            var odontogramas = await _odontogramaService.GetByPacienteAsync(pacienteId);
            return Ok(odontogramas);
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
}

