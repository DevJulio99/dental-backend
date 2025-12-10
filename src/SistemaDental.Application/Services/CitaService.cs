using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SistemaDental.Application.DTOs.Cita;
using SistemaDental.Domain.Entities;
using SistemaDental.Domain.Enums;
using SistemaDental.Infrastructure.Repositories;
using SistemaDental.Infrastructure.Services;
using System.Security.Claims;

namespace SistemaDental.Application.Services;

public class CitaService : ICitaService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantService _tenantService;
    private readonly ILogger<CitaService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CitaService(
        IUnitOfWork unitOfWork,
        ITenantService tenantService,
        ILogger<CitaService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _tenantService = tenantService;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<CitaDto?> GetByIdAsync(Guid id)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue) return null;

        var cita = await _unitOfWork.Citas.GetByIdWithRelationsAsync(id, tenantId.Value);
        if (cita == null) return null;

        return MapToDto(cita);
    }

    public async Task<IEnumerable<CitaDto>> GetAllAsync()
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue) return Enumerable.Empty<CitaDto>();

        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return Enumerable.Empty<CitaDto>();

        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var usuarioId))
        {
            return Enumerable.Empty<CitaDto>();
        }

        IEnumerable<Cita> citas;

        if (user.IsInRole("Admin"))
        {
            citas = await _unitOfWork.Citas.GetByTenantAsync(tenantId.Value);
        }
        else
        {
            citas = await _unitOfWork.Citas.GetByUsuarioAsync(tenantId.Value, usuarioId);
        }

        return citas.Select(MapToDto).OrderByDescending(c => c.FechaHora);
    }

    public async Task<IEnumerable<CitaDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue) return Enumerable.Empty<CitaDto>();

        var startDateOnly = DateOnly.FromDateTime(startDate);
        var endDateOnly = DateOnly.FromDateTime(endDate);
        
        var citas = await _unitOfWork.Citas.GetByDateRangeAsync(tenantId.Value, startDateOnly, endDateOnly);
        return citas.Select(MapToDto);
    }

    public async Task<IEnumerable<CitaDto>> GetByPacienteAsync(Guid pacienteId)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue) return Enumerable.Empty<CitaDto>();

        var citas = await _unitOfWork.Citas.GetByPacienteAsync(tenantId.Value, pacienteId);
        return citas.Select(MapToDto);
    }

    public async Task<CitaDto> CreateAsync(CitaCreateDto dto)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue)
            throw new InvalidOperationException("Tenant no identificado");

        // Verificar que el paciente existe y pertenece al tenant
        var paciente = await _unitOfWork.Pacientes.GetByIdWithTenantAsync(dto.PacienteId, tenantId.Value);

        if (paciente == null)
            throw new InvalidOperationException("Paciente no encontrado");

        // Calcular EndTime
        var endTime = dto.StartTime.AddMinutes(dto.DuracionMinutos);

        var conflictoPaciente = await _unitOfWork.Citas.HasConflictAsync(
            tenantId.Value,
            dto.AppointmentDate,
            dto.StartTime,
            endTime,
            pacienteId: dto.PacienteId);

        if (conflictoPaciente)
            throw new InvalidOperationException("El paciente ya tiene otra cita agendada en ese horario.");

        // Verificar que no haya conflicto de horario
        var conflicto = await _unitOfWork.Citas.HasConflictAsync(
            tenantId.Value, 
            dto.AppointmentDate, 
            dto.StartTime, 
            endTime, 
            dto.UsuarioId);

        if (conflicto)
            throw new InvalidOperationException("Ya existe una cita en ese horario");

        var cita = new Cita
        {
            TenantId = tenantId.Value,
            PacienteId = dto.PacienteId,
            UsuarioId = dto.UsuarioId,
            AppointmentDate = dto.AppointmentDate,
            StartTime = dto.StartTime,
            EndTime = endTime,
            DuracionMinutos = dto.DuracionMinutos,
            Estado = AppointmentStatus.Scheduled,
            Motivo = dto.Motivo ?? string.Empty,
            Observaciones = dto.Observaciones,
            FechaCreacion = DateTime.UtcNow
        };

        await _unitOfWork.Citas.AddAsync(cita);
        await _unitOfWork.SaveChangesAsync();

        return await GetByIdAsync(cita.Id) ?? throw new InvalidOperationException("Error al crear cita");
    }

    public async Task<CitaDto?> UpdateAsync(Guid id, CitaCreateDto dto)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue) return null;

        var cita = await _unitOfWork.Citas.GetByIdWithRelationsAsync(id, tenantId.Value);
        if (cita == null) return null;

        // Verificar que la cita no esté eliminada o cancelada
        if (cita.DeletedAt != null)
            throw new InvalidOperationException("No se puede actualizar una cita eliminada");

        if (cita.Estado == AppointmentStatus.Cancelled)
            throw new InvalidOperationException("No se puede actualizar una cita cancelada");

        // Verificar que el paciente existe y pertenece al tenant
        var paciente = await _unitOfWork.Pacientes.GetByIdWithTenantAsync(dto.PacienteId, tenantId.Value);
        if (paciente == null)
            throw new InvalidOperationException("Paciente no encontrado");

        // Verificar que el usuario (dentista) existe y pertenece al tenant
        var usuario = await _unitOfWork.Usuarios.GetByIdAsync(dto.UsuarioId);
        if (usuario == null || usuario.TenantId != tenantId.Value)
            throw new InvalidOperationException("Usuario (dentista) no encontrado o no pertenece al tenant");

        var hasTimeChanged = cita.AppointmentDate != dto.AppointmentDate ||
                             cita.StartTime != dto.StartTime ||
                             cita.DuracionMinutos != dto.DuracionMinutos ||
                             cita.UsuarioId != dto.UsuarioId ||
                             cita.PacienteId != dto.PacienteId;

        if (hasTimeChanged)
        {
            var newEndTime = dto.StartTime.AddMinutes(dto.DuracionMinutos);

            var conflictoPaciente = await _unitOfWork.Citas.HasConflictAsync(
                tenantId.Value,
                dto.AppointmentDate,
                dto.StartTime,
                newEndTime,
                pacienteId: dto.PacienteId,
                excludeCitaId: id);

            if (conflictoPaciente)
                throw new InvalidOperationException("El paciente ya tiene otra cita agendada en ese horario.");


            var conflictoDoctor = await _unitOfWork.Citas.HasConflictAsync(
                tenantId.Value,
                dto.AppointmentDate,
                dto.StartTime,
                newEndTime,
                usuarioId: dto.UsuarioId,
                excludeCitaId: id);

            if (conflictoDoctor)
                throw new InvalidOperationException("El profesional ya tiene otra cita en ese horario.");
        }

        cita.PacienteId = dto.PacienteId;
        cita.UsuarioId = dto.UsuarioId;
        cita.AppointmentDate = dto.AppointmentDate;
        cita.StartTime = dto.StartTime;
        cita.EndTime = dto.StartTime.AddMinutes(dto.DuracionMinutos);
        cita.DuracionMinutos = dto.DuracionMinutos;
        cita.Motivo = dto.Motivo ?? string.Empty;
        cita.Observaciones = dto.Observaciones;
        cita.Estado = dto.Estado;
        cita.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Citas.UpdateAsync(cita);
        await _unitOfWork.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> ConfirmAsync(Guid id)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue) return false;

        var cita = await _unitOfWork.Citas.GetByIdWithRelationsAsync(id, tenantId.Value);
        if (cita == null || cita.DeletedAt != null) return false;

        cita.Estado = AppointmentStatus.Confirmed;
        cita.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Citas.UpdateAsync(cita);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> CancelAsync(Guid id)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue) return false;

        var cita = await _unitOfWork.Citas.GetByIdWithRelationsAsync(id, tenantId.Value);
        if (cita == null || cita.DeletedAt != null) return false;

        cita.Estado = AppointmentStatus.Cancelled;
        cita.CancelledAt = DateTime.UtcNow;
        cita.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Citas.UpdateAsync(cita);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue) return false;

        var cita = await _unitOfWork.Citas.GetByIdWithRelationsAsync(id, tenantId.Value);
        if (cita == null) return false;

        // Soft delete
        cita.DeletedAt = DateTime.UtcNow;
        cita.UpdatedAt = DateTime.UtcNow;
        
        await _unitOfWork.Citas.UpdateAsync(cita);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<DateTime>> GetAvailableSlotsAsync(DateTime date, Guid? usuarioId = null)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue)
        {
            _logger.LogWarning("No se pudo obtener TenantId en GetAvailableSlotsAsync");
            return Enumerable.Empty<DateTime>();
        }

        // Obtener horarios configurados del tenant (por defecto 9:00 - 18:00)
        var startHour = 9;
        var endHour = 18;
        var slotDuration = 30; // minutos

        var startDateTime = new DateTime(date.Year, date.Month, date.Day, startHour, 0, 0);
        var endDateTime = new DateTime(date.Year, date.Month, date.Day, endHour, 0, 0);

        var dateOnly = DateOnly.FromDateTime(date);
        
        _logger.LogInformation("Buscando citas ocupadas - TenantId: {TenantId}, Fecha: {Fecha}, UsuarioId: {UsuarioId}", 
            tenantId.Value, dateOnly, usuarioId);
        
        // Obtener citas ocupadas del día
        var citasOcupadas = await _unitOfWork.Citas.GetOcupadasByDateAsync(tenantId.Value, dateOnly, usuarioId);
        
        _logger.LogInformation("Citas ocupadas encontradas: {Cantidad}", citasOcupadas.Count());
        foreach (var cita in citasOcupadas)
        {
            _logger.LogInformation("Cita ocupada - Id: {Id}, Fecha: {Fecha}, Hora: {StartTime}-{EndTime}, Estado: {Estado}, UsuarioId: {UsuarioId}", 
                cita.Id, cita.AppointmentDate, cita.StartTime, cita.EndTime, cita.Estado, cita.UsuarioId);
        }

        var slots = new List<DateTime>();
        var currentSlot = startDateTime;

        while (currentSlot < endDateTime)
        {
            var currentTime = TimeOnly.FromDateTime(currentSlot);
            var slotEndTime = currentTime.AddMinutes(slotDuration);
            
            // Un slot está ocupado si se solapa con alguna cita
            // Solapamiento: el slot empieza antes de que termine la cita Y termina después de que empiece la cita
            var slotOcupado = citasOcupadas.Any(c =>
                currentTime < c.EndTime &&
                slotEndTime > c.StartTime);

            if (!slotOcupado)
            {
                slots.Add(currentSlot);
            }
            else
            {
                _logger.LogDebug("Slot ocupado: {SlotTime} (cita: {CitaStart}-{CitaEnd})", 
                    currentSlot, 
                    citasOcupadas.First(c => currentTime < c.EndTime && slotEndTime > c.StartTime).StartTime,
                    citasOcupadas.First(c => currentTime < c.EndTime && slotEndTime > c.StartTime).EndTime);
            }

            currentSlot = currentSlot.AddMinutes(slotDuration);
        }

        _logger.LogInformation("Slots disponibles generados: {Cantidad}", slots.Count);
        return slots;
    }

    private static CitaDto MapToDto(Cita cita)
    {
        return new CitaDto
        {
            Id = cita.Id,
            PacienteId = cita.PacienteId,
            PacienteNombre = cita.Paciente.NombreCompleto,
            UsuarioId = cita.UsuarioId,
            UsuarioNombre = cita.Usuario != null ? $"{cita.Usuario.Nombre} {cita.Usuario.Apellido}" : string.Empty,
            AppointmentDate = cita.AppointmentDate,
            StartTime = cita.StartTime,
            EndTime = cita.EndTime,
            DuracionMinutos = cita.DuracionMinutos,
            Estado = cita.Estado,
            Motivo = cita.Motivo,
            Observaciones = cita.Observaciones,
            NotificationSent = cita.NotificationSent,
            ReminderSent = cita.ReminderSent,
            CancellationReason = cita.CancellationReason,
            CancelledAt = cita.CancelledAt,
            FechaCreacion = cita.FechaCreacion
        };
    }
}
