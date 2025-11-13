using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaDental.Application.DTOs.Cita;
using SistemaDental.Application.Services;

namespace SistemaDental.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CitasController : ControllerBase
{
    private readonly ICitaService _citaService;
    private readonly ILogger<CitasController> _logger;

    public CitasController(ICitaService citaService, ILogger<CitasController> logger)
    {
        _citaService = citaService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CitaDto>>> GetAll()
    {
        try
        {
            var citas = await _citaService.GetAllAsync();
            return Ok(citas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener citas");
            return StatusCode(500, new { message = "Error al obtener citas" });
        }
    }

    [HttpGet("rango")]
    public async Task<ActionResult<IEnumerable<CitaDto>>> GetByDateRange(
        [FromQuery] DateTime fechaInicio,
        [FromQuery] DateTime fechaFin)
    {
        try
        {
            var citas = await _citaService.GetByDateRangeAsync(fechaInicio, fechaFin);
            return Ok(citas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener citas por rango");
            return StatusCode(500, new { message = "Error al obtener citas" });
        }
    }

    [HttpGet("paciente/{pacienteId}")]
    public async Task<ActionResult<IEnumerable<CitaDto>>> GetByPaciente(Guid pacienteId)
    {
        try
        {
            var citas = await _citaService.GetByPacienteAsync(pacienteId);
            return Ok(citas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener citas del paciente");
            return StatusCode(500, new { message = "Error al obtener citas" });
        }
    }

    [HttpGet("disponibles")]
    public async Task<ActionResult<IEnumerable<DateTime>>> GetAvailableSlots(
        [FromQuery] DateTime fecha,
        [FromQuery] Guid? usuarioId = null)
    {
        try
        {
            var slots = await _citaService.GetAvailableSlotsAsync(fecha, usuarioId);
            return Ok(slots);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener horarios disponibles");
            return StatusCode(500, new { message = "Error al obtener horarios disponibles" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CitaDto>> GetById(Guid id)
    {
        try
        {
            var cita = await _citaService.GetByIdAsync(id);
            if (cita == null)
            {
                return NotFound(new { message = "Cita no encontrada" });
            }
            return Ok(cita);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener cita");
            return StatusCode(500, new { message = "Error al obtener cita" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<CitaDto>> Create([FromBody] CitaCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var cita = await _citaService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = cita.Id }, cita);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear cita");
            return StatusCode(500, new { message = "Error al crear cita" });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CitaDto>> Update(Guid id, [FromBody] CitaCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var cita = await _citaService.UpdateAsync(id, dto);
            if (cita == null)
            {
                return NotFound(new { message = "Cita no encontrada" });
            }
            return Ok(cita);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar cita");
            return StatusCode(500, new { message = "Error al actualizar cita" });
        }
    }

    [HttpPost("{id}/confirmar")]
    public async Task<ActionResult> Confirm(Guid id)
    {
        try
        {
            var result = await _citaService.ConfirmAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Cita no encontrada" });
            }
            return Ok(new { message = "Cita confirmada" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al confirmar cita");
            return StatusCode(500, new { message = "Error al confirmar cita" });
        }
    }

    [HttpPost("{id}/cancelar")]
    public async Task<ActionResult> Cancel(Guid id)
    {
        try
        {
            var result = await _citaService.CancelAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Cita no encontrada" });
            }
            return Ok(new { message = "Cita cancelada" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cancelar cita");
            return StatusCode(500, new { message = "Error al cancelar cita" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _citaService.DeleteAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Cita no encontrada" });
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar cita");
            return StatusCode(500, new { message = "Error al eliminar cita" });
        }
    }
}

