using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaDental.Application.DTOs.Paciente;
using SistemaDental.Application.Services;

namespace SistemaDental.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PacientesController : ControllerBase
{
    private readonly IPacienteService _pacienteService;
    private readonly ILogger<PacientesController> _logger;

    public PacientesController(IPacienteService pacienteService, ILogger<PacientesController> logger)
    {
        _pacienteService = pacienteService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PacienteDto>>> GetAll()
    {
        try
        {
            var pacientes = await _pacienteService.GetAllAsync();
            return Ok(pacientes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener pacientes");
            return StatusCode(500, new { message = "Error al obtener pacientes" });
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<PacienteDto>>> Search([FromQuery] string term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return BadRequest(new { message = "Término de búsqueda requerido" });
        }

        try
        {
            var pacientes = await _pacienteService.SearchAsync(term);
            return Ok(pacientes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar pacientes");
            return StatusCode(500, new { message = "Error al buscar pacientes" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PacienteDto>> GetById(Guid id)
    {
        try
        {
            var paciente = await _pacienteService.GetByIdAsync(id);
            if (paciente == null)
            {
                return NotFound(new { message = "Paciente no encontrado" });
            }
            return Ok(paciente);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener paciente");
            return StatusCode(500, new { message = "Error al obtener paciente" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<PacienteDto>> Create([FromBody] PacienteCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var paciente = await _pacienteService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = paciente.Id }, paciente);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear paciente");
            return StatusCode(500, new { message = "Error al crear paciente" });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PacienteDto>> Update(Guid id, [FromBody] PacienteCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var paciente = await _pacienteService.UpdateAsync(id, dto);
            if (paciente == null)
            {
                return NotFound(new { message = "Paciente no encontrado" });
            }
            return Ok(paciente);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar paciente");
            return StatusCode(500, new { message = "Error al actualizar paciente" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _pacienteService.DeleteAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Paciente no encontrado" });
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar paciente");
            return StatusCode(500, new { message = "Error al eliminar paciente" });
        }
    }
}

