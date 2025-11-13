using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SistemaDental.Application.DTOs.Tratamiento;
using SistemaDental.Domain.Entities;
using SistemaDental.Infrastructure.Repositories;
using SistemaDental.Infrastructure.Services;
using System.Security.Claims;

namespace SistemaDental.Application.Services;

public class TratamientoService : ITratamientoService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantService _tenantService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TratamientoService> _logger;

    public TratamientoService(
        IUnitOfWork unitOfWork,
        ITenantService tenantService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<TratamientoService> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantService = tenantService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<TratamientoDto?> GetByIdAsync(Guid id)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue) return null;

        var tratamiento = await _unitOfWork.Tratamientos.GetByIdWithRelationsAsync(id, tenantId.Value);
        if (tratamiento == null) return null;

        return MapToDto(tratamiento);
    }

    public async Task<IEnumerable<TratamientoDto>> GetAllAsync()
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue) return Enumerable.Empty<TratamientoDto>();

        var tratamientos = await _unitOfWork.Tratamientos.GetByTenantAsync(tenantId.Value);
        return tratamientos.Select(MapToDto);
    }

    public async Task<IEnumerable<TratamientoDto>> GetByPacienteAsync(Guid pacienteId)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue) return Enumerable.Empty<TratamientoDto>();

        var tratamientos = await _unitOfWork.Tratamientos.GetByPacienteAsync(tenantId.Value, pacienteId);
        return tratamientos.Select(MapToDto);
    }

    public async Task<IEnumerable<TratamientoDto>> GetByCitaAsync(Guid citaId)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue) return Enumerable.Empty<TratamientoDto>();

        var tratamientos = await _unitOfWork.Tratamientos.GetByCitaAsync(tenantId.Value, citaId);
        return tratamientos.Select(MapToDto);
    }

    public async Task<TratamientoDto> CreateAsync(TratamientoCreateDto dto)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue)
            throw new InvalidOperationException("Tenant no identificado");

        // Obtener el usuario actual desde el token JWT
        var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var usuarioId))
        {
            throw new UnauthorizedAccessException("Usuario no autenticado");
        }

        // Verificar que el paciente existe
        var paciente = await _unitOfWork.Pacientes.GetByIdWithTenantAsync(dto.PacienteId, tenantId.Value);

        if (paciente == null)
            throw new InvalidOperationException("Paciente no encontrado");

        var tratamiento = new Tratamiento
        {
            TenantId = tenantId.Value,
            PacienteId = dto.PacienteId,
            CitaId = dto.CitaId,
            UsuarioId = usuarioId,
            TreatmentId = dto.TreatmentId,
            TreatmentPerformed = dto.TreatmentPerformed,
            Diagnosis = dto.Diagnosis,
            Costo = dto.Costo,
            Observaciones = dto.Observaciones,
            TreatmentDate = dto.TreatmentDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            FechaCreacion = DateTime.UtcNow
        };

        await _unitOfWork.Tratamientos.AddAsync(tratamiento);
        await _unitOfWork.SaveChangesAsync();

        return await GetByIdAsync(tratamiento.Id) ?? throw new InvalidOperationException("Error al crear tratamiento");
    }

    public async Task<TratamientoDto?> UpdateAsync(Guid id, TratamientoCreateDto dto)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue) return null;

        var tratamiento = await _unitOfWork.Tratamientos.GetByIdWithRelationsAsync(id, tenantId.Value);
        if (tratamiento == null) return null;

        tratamiento.PacienteId = dto.PacienteId;
        tratamiento.CitaId = dto.CitaId;
        tratamiento.TreatmentId = dto.TreatmentId;
        tratamiento.TreatmentPerformed = dto.TreatmentPerformed;
        tratamiento.Diagnosis = dto.Diagnosis;
        tratamiento.Costo = dto.Costo;
        tratamiento.Observaciones = dto.Observaciones;
        if (dto.TreatmentDate.HasValue)
        {
            tratamiento.TreatmentDate = dto.TreatmentDate.Value;
        }
        tratamiento.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Tratamientos.UpdateAsync(tratamiento);
        await _unitOfWork.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue) return false;

        var tratamiento = await _unitOfWork.Tratamientos.GetByIdWithRelationsAsync(id, tenantId.Value);
        if (tratamiento == null) return false;

        await _unitOfWork.Tratamientos.DeleteAsync(tratamiento);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    private static TratamientoDto MapToDto(Tratamiento tratamiento)
    {
        return new TratamientoDto
        {
            Id = tratamiento.Id,
            PacienteId = tratamiento.PacienteId,
            PacienteNombre = tratamiento.Paciente.NombreCompleto,
            CitaId = tratamiento.CitaId,
            UsuarioId = tratamiento.UsuarioId,
            UsuarioNombre = $"{tratamiento.Usuario.Nombre} {tratamiento.Usuario.Apellido}",
            TreatmentId = tratamiento.TreatmentId,
            TreatmentPerformed = tratamiento.TreatmentPerformed,
            Diagnosis = tratamiento.Diagnosis,
            Costo = tratamiento.Costo,
            TreatmentDate = tratamiento.TreatmentDate,
            Observaciones = tratamiento.Observaciones
        };
    }
}

