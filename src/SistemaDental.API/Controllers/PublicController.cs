using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaDental.Application.DTOs.Cita;
using SistemaDental.Application.Services;
using SistemaDental.Domain.Entities;
using SistemaDental.Domain.Enums;
using SistemaDental.Infrastructure.Data;
using SistemaDental.Infrastructure.Repositories;

namespace SistemaDental.API.Controllers;

[ApiController]
[Route("api/public")]
public class PublicController : ControllerBase
{
    private readonly ICitaService _citaService;
    private readonly ITenantRepository _tenantRepository;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PublicController> _logger;

    public PublicController(
        ICitaService citaService,
        ITenantRepository tenantRepository,
        ApplicationDbContext context,
        ILogger<PublicController> logger)
    {
        _citaService = citaService;
        _tenantRepository = tenantRepository;
        _context = context;
        _logger = logger;
    }

    [HttpGet("tenants")]
    public async Task<ActionResult<IEnumerable<object>>> GetTenantsActivos()
    {
        try
        {
            var tenants = await _context.Tenants
                .Where(t => t.Status == TenantStatus.Active || t.Status == TenantStatus.Trial)
                .OrderBy(t => t.Nombre)
                .Select(t => new
                {
                    id = t.Id,
                    nombre = t.Nombre,
                    subdomain = t.Subdominio,
                    email = t.Email,
                    telefono = t.Telefono
                })
                .ToListAsync();

            return Ok(tenants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener tenants activos");
            return StatusCode(500, new { message = "Error al obtener consultorios" });
        }
    }

    [HttpGet("verificar-subdomain")]
    public async Task<ActionResult> VerificarSubdomain([FromQuery] string subdomain)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(subdomain))
            {
                return BadRequest(new { message = "El subdomain es requerido" });
            }

            var tenant = await _tenantRepository.GetBySubdomainAsync(subdomain);
            
            if (tenant == null || !tenant.Activo)
            {
                return Ok(new 
                { 
                    existe = false, 
                    message = "Subdomain no encontrado o inactivo" 
                });
            }

            return Ok(new 
            { 
                existe = true, 
                nombre = tenant.Nombre,
                subdomain = tenant.Subdominio
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar subdomain");
            return StatusCode(500, new { message = "Error al verificar subdomain" });
        }
    }

    [HttpGet("horarios-disponibles")]
    public async Task<ActionResult<IEnumerable<DateTime>>> GetHorariosDisponibles(
        [FromQuery] string subdomain,
        [FromQuery] DateTime fecha,
        [FromQuery] Guid? usuarioId = null)
    {
        try
        {
            // Verificar que el tenant existe
            var tenant = await _tenantRepository.GetBySubdomainAsync(subdomain);
            if (tenant == null || !tenant.Activo)
            {
                return NotFound(new { message = "Consultorio no encontrado" });
            }

            // Obtener horarios disponibles (sin autenticación, pero con verificación de tenant)
            var slots = await GetAvailableSlotsForTenant(tenant.Id, fecha, usuarioId);
            return Ok(slots);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener horarios disponibles");
            return StatusCode(500, new { message = "Error al obtener horarios" });
        }
    }

    [HttpPost("reservar-cita")]
    public async Task<ActionResult<CitaDto>> ReservarCita(
        [FromQuery] string subdomain,
        [FromBody] CitaPublicCreateDto dto)
    {
        try
        {
            // Verificar que el tenant existe
            var tenant = await _tenantRepository.GetBySubdomainAsync(subdomain);
            if (tenant == null || !tenant.Activo)
            {
                return NotFound(new { message = "Consultorio no encontrado" });
            }

            // Verificar que el paciente existe o crearlo
            var paciente = await _context.Pacientes
                .Where(p => p.TenantId == tenant.Id && p.DniPasaporte == dto.DniPasaporte)
                .FirstOrDefaultAsync();

            if (paciente == null)
            {
                // Dividir NombreCompleto en FirstName y LastName
                var nombreParts = dto.NombreCompleto.Split(' ', 2);
                var firstName = nombreParts.Length > 0 ? nombreParts[0] : string.Empty;
                var lastName = nombreParts.Length > 1 ? nombreParts[1] : string.Empty;

                // Crear paciente si no existe
                paciente = new Paciente
                {
                    TenantId = tenant.Id,
                    FirstName = firstName,
                    LastName = lastName,
                    TipoDocumento = dto.TipoDocumento ?? "DNI",
                    DniPasaporte = dto.DniPasaporte,
                    FechaNacimiento = dto.FechaNacimiento,
                    Telefono = dto.Telefono,
                    Email = dto.Email,
                    FechaCreacion = DateTime.UtcNow
                };
                await _context.Pacientes.AddAsync(paciente);
                await _context.SaveChangesAsync();
            }

            // Convertir FechaHora a AppointmentDate y StartTime
            var appointmentDate = DateOnly.FromDateTime(dto.FechaHora);
            var startTime = TimeOnly.FromDateTime(dto.FechaHora);
            var endTime = startTime.AddMinutes(30);

            // Verificar disponibilidad
            var slots = await GetAvailableSlotsForTenant(tenant.Id, dto.FechaHora.Date, null);
            if (!slots.Contains(dto.FechaHora))
            {
                return BadRequest(new { message = "El horario seleccionado no está disponible" });
            }

            // Obtener un odontólogo del tenant (o usar el primero disponible)
            // Nota: Los roles en la BD son "dentist" y "tenant_admin", pero se convierten a "Odontologo" y "Admin" en la entidad
            var usuarios = await _context.Usuarios
                .Where(u => u.TenantId == tenant.Id && u.Status == UserStatus.Active)
                .ToListAsync();
            
            var odontologo = usuarios.FirstOrDefault(u => 
                u.Rol == "Odontologo" || u.Rol == "Admin");

            if (odontologo == null)
            {
                return BadRequest(new { message = "No hay odontólogos disponibles en este consultorio" });
            }

            // Crear cita directamente en la base de datos
            var cita = new Cita
            {
                TenantId = tenant.Id,
                PacienteId = paciente.Id,
                UsuarioId = odontologo.Id,
                AppointmentDate = appointmentDate,
                StartTime = startTime,
                EndTime = endTime,
                DuracionMinutos = 30,
                Estado = SistemaDental.Domain.Enums.AppointmentStatus.Scheduled,
                Motivo = dto.Motivo ?? "Consulta general",
                FechaCreacion = DateTime.UtcNow
            };

            await _context.Citas.AddAsync(cita);
            await _context.SaveChangesAsync();

            // Retornar la cita creada usando el servicio para obtener el DTO completo
            var citaDto = await _citaService.GetByIdAsync(cita.Id);
            if (citaDto == null)
            {
                return StatusCode(500, new { message = "Error al obtener la cita creada" });
            }

            return Ok(citaDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reservar cita");
            return StatusCode(500, new { message = "Error al reservar cita" });
        }
    }

    private async Task<List<DateTime>> GetAvailableSlotsForTenant(Guid tenantId, DateTime date, Guid? usuarioId)
    {
        var startHour = 9;
        var endHour = 18;
        var slotDuration = 30;

        var startDateTime = new DateTime(date.Year, date.Month, date.Day, startHour, 0, 0);
        var endDateTime = new DateTime(date.Year, date.Month, date.Day, endHour, 0, 0);

        var dateOnly = DateOnly.FromDateTime(date);
        
        var citasOcupadas = await _context.Citas
            .Where(c => c.TenantId == tenantId &&
                       c.DeletedAt == null &&
                       c.AppointmentDate == dateOnly &&
                       c.Estado != AppointmentStatus.Cancelled &&
                       (usuarioId == null || c.UsuarioId == usuarioId.Value))
            .ToListAsync();

        var slots = new List<DateTime>();
        var currentSlot = startDateTime;

        while (currentSlot < endDateTime)
        {
            var currentTime = TimeOnly.FromDateTime(currentSlot);
            var slotEndTime = currentTime.AddMinutes(slotDuration);
            
            var slotOcupado = citasOcupadas.Any(c =>
                currentTime < c.EndTime &&
                slotEndTime > c.StartTime);

            if (!slotOcupado)
            {
                slots.Add(currentSlot);
            }

            currentSlot = currentSlot.AddMinutes(slotDuration);
        }

        return slots;
    }
}

public class CitaPublicCreateDto
{
    public string NombreCompleto { get; set; } = string.Empty;
    public string? TipoDocumento { get; set; } = "DNI";
    public string DniPasaporte { get; set; } = string.Empty;
    public DateTime FechaNacimiento { get; set; }
    public string Telefono { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTime FechaHora { get; set; }
    public string? Motivo { get; set; }
}

