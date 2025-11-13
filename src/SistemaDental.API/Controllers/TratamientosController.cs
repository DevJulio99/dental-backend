using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaDental.Application.DTOs.Tratamiento;
using SistemaDental.Application.Services;

namespace SistemaDental.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TratamientosController : ControllerBase
{
    private readonly ITratamientoService _tratamientoService;
    private readonly ILogger<TratamientosController> _logger;

    public TratamientosController(ITratamientoService tratamientoService, ILogger<TratamientosController> logger)
    {
        _tratamientoService = tratamientoService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TratamientoDto>>> GetAll()
    {
        try
        {
            var tratamientos = await _tratamientoService.GetAllAsync();
            return Ok(tratamientos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener tratamientos");
            return StatusCode(500, new { message = "Error al obtener tratamientos" });
        }
    }

    [HttpGet("paciente/{pacienteId}")]
    public async Task<ActionResult<IEnumerable<TratamientoDto>>> GetByPaciente(Guid pacienteId)
    {
        try
        {
            var tratamientos = await _tratamientoService.GetByPacienteAsync(pacienteId);
            return Ok(tratamientos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener tratamientos del paciente");
            return StatusCode(500, new { message = "Error al obtener tratamientos" });
        }
    }

    [HttpGet("cita/{citaId}")]
    public async Task<ActionResult<IEnumerable<TratamientoDto>>> GetByCita(Guid citaId)
    {
        try
        {
            var tratamientos = await _tratamientoService.GetByCitaAsync(citaId);
            return Ok(tratamientos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener tratamientos de la cita");
            return StatusCode(500, new { message = "Error al obtener tratamientos" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TratamientoDto>> GetById(Guid id)
    {
        try
        {
            var tratamiento = await _tratamientoService.GetByIdAsync(id);
            if (tratamiento == null)
            {
                return NotFound(new { message = "Tratamiento no encontrado" });
            }
            return Ok(tratamiento);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener tratamiento");
            return StatusCode(500, new { message = "Error al obtener tratamiento" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<TratamientoDto>> Create([FromBody] TratamientoCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var tratamiento = await _tratamientoService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = tratamiento.Id }, tratamiento);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear tratamiento");
            return StatusCode(500, new { message = "Error al crear tratamiento" });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TratamientoDto>> Update(Guid id, [FromBody] TratamientoCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var tratamiento = await _tratamientoService.UpdateAsync(id, dto);
            if (tratamiento == null)
            {
                return NotFound(new { message = "Tratamiento no encontrado" });
            }
            return Ok(tratamiento);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar tratamiento");
            return StatusCode(500, new { message = "Error al actualizar tratamiento" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _tratamientoService.DeleteAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Tratamiento no encontrado" });
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar tratamiento");
            return StatusCode(500, new { message = "Error al eliminar tratamiento" });
        }
    }
}

