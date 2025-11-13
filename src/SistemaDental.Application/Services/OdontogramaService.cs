using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SistemaDental.Application.DTOs.Odontograma;
using SistemaDental.Domain.Entities;
using SistemaDental.Infrastructure.Repositories;
using SistemaDental.Infrastructure.Services;
using System.Security.Claims;

namespace SistemaDental.Application.Services;

public class OdontogramaService : IOdontogramaService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantService _tenantService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<OdontogramaService> _logger;

    public OdontogramaService(
        IUnitOfWork unitOfWork,
        ITenantService tenantService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<OdontogramaService> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantService = tenantService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<IEnumerable<OdontogramaDto>> GetByPacienteAsync(Guid pacienteId)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue) return Enumerable.Empty<OdontogramaDto>();

        var odontogramas = await _unitOfWork.Odontogramas.GetByPacienteAsync(tenantId.Value, pacienteId);
        return odontogramas.Select(MapToDto);
    }

    public async Task<OdontogramaDto> CreateAsync(OdontogramaCreateDto dto)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue)
            throw new InvalidOperationException("Tenant no identificado");

        // Verificar que el paciente existe
        var paciente = await _unitOfWork.Pacientes.GetByIdWithTenantAsync(dto.PacienteId, tenantId.Value);

        if (paciente == null)
            throw new InvalidOperationException("Paciente no encontrado");

        // Obtener UsuarioId del contexto HTTP (JWT)
        var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var usuarioId))
            throw new InvalidOperationException("Usuario no identificado");

        var odontograma = new Odontograma
        {
            TenantId = tenantId.Value,
            PacienteId = dto.PacienteId,
            NumeroDiente = dto.NumeroDiente,
            Estado = dto.Estado,
            Observaciones = dto.Observaciones,
            FechaRegistro = dto.FechaRegistro ?? DateOnly.FromDateTime(DateTime.UtcNow),
            UsuarioId = usuarioId
        };

        await _unitOfWork.Odontogramas.AddAsync(odontograma);
        await _unitOfWork.SaveChangesAsync();

        return await GetByIdAsync(odontograma.Id) ?? throw new InvalidOperationException("Error al crear odontograma");
    }

    public async Task<OdontogramaDto?> UpdateAsync(Guid id, OdontogramaCreateDto dto)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue) return null;

        var odontograma = await _unitOfWork.Odontogramas.GetByIdWithRelationsAsync(id, tenantId.Value);
        if (odontograma == null) return null;

        odontograma.NumeroDiente = dto.NumeroDiente;
        odontograma.Estado = dto.Estado;
        odontograma.Observaciones = dto.Observaciones;
        if (dto.FechaRegistro.HasValue)
        {
            odontograma.FechaRegistro = dto.FechaRegistro.Value;
        }

        await _unitOfWork.Odontogramas.UpdateAsync(odontograma);
        await _unitOfWork.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue) return false;

        var odontograma = await _unitOfWork.Odontogramas.GetByIdWithRelationsAsync(id, tenantId.Value);
        if (odontograma == null) return false;

        // Los odontogramas no tienen soft delete, se eliminan físicamente
        await _unitOfWork.Odontogramas.DeleteAsync(odontograma);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<Dictionary<int, OdontogramaDto?>> GetEstadoActualDientesAsync(Guid pacienteId)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue) return new Dictionary<int, OdontogramaDto?>();

        // Obtener el estado más reciente de cada diente
        var dientes = new List<int> { 11, 12, 13, 14, 15, 16, 17, 18, 21, 22, 23, 24, 25, 26, 27, 28,
                                      31, 32, 33, 34, 35, 36, 37, 38, 41, 42, 43, 44, 45, 46, 47, 48 };

        var resultado = new Dictionary<int, OdontogramaDto?>();

        foreach (var numeroDiente in dientes)
        {
            var odontograma = await _unitOfWork.Odontogramas.GetByDienteLatestAsync(tenantId.Value, pacienteId, numeroDiente);
            resultado[numeroDiente] = odontograma != null ? MapToDto(odontograma) : null;
        }

        return resultado;
    }

    private async Task<OdontogramaDto?> GetByIdAsync(Guid id)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue) return null;

        var odontograma = await _unitOfWork.Odontogramas.GetByIdWithRelationsAsync(id, tenantId.Value);
        if (odontograma == null) return null;

        return MapToDto(odontograma);
    }

    private static OdontogramaDto MapToDto(Odontograma odontograma)
    {
        return new OdontogramaDto
        {
            Id = odontograma.Id,
            PacienteId = odontograma.PacienteId,
            NumeroDiente = odontograma.NumeroDiente,
            Estado = odontograma.Estado,
            Observaciones = odontograma.Observaciones,
            FechaRegistro = odontograma.FechaRegistro,
            UsuarioId = odontograma.UsuarioId,
            UsuarioNombre = odontograma.Usuario != null ? $"{odontograma.Usuario.Nombre} {odontograma.Usuario.Apellido}" : string.Empty
        };
    }
}

