using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SistemaDental.Application.DTOs.Odontograma;
using SistemaDental.Domain.Entities;
using SistemaDental.Domain.Enums;
using SistemaDental.Infrastructure.Repositories;
using SistemaDental.Infrastructure.Services;
using System.Security.Claims;

namespace SistemaDental.Application.Services;

/// <summary>
/// Servicio para la gestión de odontogramas
/// </summary>
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

    /// <inheritdoc />
    public async Task<IEnumerable<OdontogramaDto>> GetByPacienteAsync(Guid pacienteId)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue) return Enumerable.Empty<OdontogramaDto>();

        var odontogramas = await _unitOfWork.Odontogramas.GetByPacienteAsync(tenantId.Value, pacienteId);
        return odontogramas.Select(MapToDto);
    }

    /// <inheritdoc />
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

        // Validar número de diente
        if (!IsValidToothNumber(dto.NumeroDiente))
        {
            throw new InvalidOperationException($"El número de diente {dto.NumeroDiente} no es válido. Debe estar entre 11-18, 21-28, 31-38 o 41-48.");
        }

        var odontograma = new Odontograma
        {
            TenantId = tenantId.Value,
            PacienteId = dto.PacienteId,
            NumeroDiente = dto.NumeroDiente,
            Estado = dto.EstadoEnum, // Usar la propiedad que convierte el string a enum
            Observaciones = dto.Observaciones,
            FechaRegistro = dto.FechaRegistro ?? DateOnly.FromDateTime(DateTime.UtcNow),
            UsuarioId = usuarioId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Odontogramas.AddAsync(odontograma);
        await _unitOfWork.SaveChangesAsync();

        return await GetByIdAsync(odontograma.Id) ?? throw new InvalidOperationException("Error al crear odontograma");
    }

    /// <inheritdoc />
    public async Task<OdontogramaDto?> UpdateAsync(Guid id, OdontogramaCreateDto dto)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue) return null;

        var odontograma = await _unitOfWork.Odontogramas.GetByIdWithRelationsAsync(id, tenantId.Value);
        if (odontograma == null) return null;

        // Validar número de diente
        if (!IsValidToothNumber(dto.NumeroDiente))
        {
            throw new InvalidOperationException($"El número de diente {dto.NumeroDiente} no es válido. Debe estar entre 11-18, 21-28, 31-38 o 41-48.");
        }

        odontograma.NumeroDiente = dto.NumeroDiente;
        odontograma.Estado = dto.EstadoEnum; // Usar la propiedad que convierte el string a enum
        odontograma.Observaciones = dto.Observaciones;
        if (dto.FechaRegistro.HasValue)
        {
            odontograma.FechaRegistro = dto.FechaRegistro.Value;
        }

        await _unitOfWork.Odontogramas.UpdateAsync(odontograma);
        await _unitOfWork.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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
            UsuarioNombre = odontograma.Usuario != null ? $"{odontograma.Usuario.Nombre} {odontograma.Usuario.Apellido}" : string.Empty,
            CreatedAt = odontograma.CreatedAt
        };
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OdontogramaDto>> GetHistorialByDienteAsync(Guid pacienteId, int numeroDiente)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue) return Enumerable.Empty<OdontogramaDto>();

        // Validar número de diente
        if (!IsValidToothNumber(numeroDiente))
        {
            throw new InvalidOperationException($"El número de diente {numeroDiente} no es válido. Debe estar entre 11-18, 21-28, 31-38 o 41-48.");
        }

        var historial = await _unitOfWork.Odontogramas.GetHistorialByDienteAsync(tenantId.Value, pacienteId, numeroDiente);
        return historial.Select(MapToDto);
    }

    /// <inheritdoc />
    public async Task<Dictionary<int, IEnumerable<OdontogramaDto>>> GetHistorialAgrupadoAsync(Guid pacienteId)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue) return new Dictionary<int, IEnumerable<OdontogramaDto>>();

        var odontogramas = await _unitOfWork.Odontogramas.GetByPacienteAsync(tenantId.Value, pacienteId);
        var odontogramasDto = odontogramas.Select(MapToDto);

        // Agrupar por número de diente
        var resultado = odontogramasDto
            .GroupBy(o => o.NumeroDiente)
            .ToDictionary(g => g.Key, g => g.AsEnumerable());

        // Asegurar que todos los dientes tengan una entrada (aunque esté vacía)
        var dientes = new List<int> { 11, 12, 13, 14, 15, 16, 17, 18, 21, 22, 23, 24, 25, 26, 27, 28,
                                      31, 32, 33, 34, 35, 36, 37, 38, 41, 42, 43, 44, 45, 46, 47, 48 };

        foreach (var numeroDiente in dientes)
        {
            if (!resultado.ContainsKey(numeroDiente))
            {
                resultado[numeroDiente] = Enumerable.Empty<OdontogramaDto>();
            }
        }

        return resultado;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OdontogramaDto>> GetByPacienteConFiltrosAsync(Guid pacienteId, DateOnly? fechaDesde, DateOnly? fechaHasta)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue) return Enumerable.Empty<OdontogramaDto>();

        // Validar que fechaDesde <= fechaHasta si ambas están presentes
        if (fechaDesde.HasValue && fechaHasta.HasValue && fechaDesde.Value > fechaHasta.Value)
        {
            throw new InvalidOperationException("La fecha desde no puede ser mayor que la fecha hasta");
        }

        var odontogramas = await _unitOfWork.Odontogramas.GetByPacienteAsync(tenantId.Value, pacienteId, fechaDesde, fechaHasta);
        return odontogramas.Select(MapToDto);
    }

    /// <inheritdoc />
    public async Task<Dictionary<int, OdontogramaDto?>> GetEstadoDientesEnFechaAsync(Guid pacienteId, DateOnly fecha)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue) return new Dictionary<int, OdontogramaDto?>();

        var estados = await _unitOfWork.Odontogramas.GetEstadoDientesEnFechaAsync(tenantId.Value, pacienteId, fecha);
        
        var resultado = new Dictionary<int, OdontogramaDto?>();
        foreach (var kvp in estados)
        {
            resultado[kvp.Key] = kvp.Value != null ? MapToDto(kvp.Value) : null;
        }

        return resultado;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OdontogramaDto>> CreateBatchAsync(IEnumerable<OdontogramaCreateDto> dtos)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue)
            throw new InvalidOperationException("Tenant no identificado");

        // Obtener UsuarioId del contexto HTTP (JWT)
        var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var usuarioId))
            throw new InvalidOperationException("Usuario no identificado");

        var listaDtos = dtos.ToList();

        if (!listaDtos.Any())
        {
            return Enumerable.Empty<OdontogramaDto>();
        }

        // Verificar que todos los DTOs pertenezcan al mismo paciente
        var pacienteId = listaDtos.First().PacienteId;
        if (listaDtos.Any(d => d.PacienteId != pacienteId))
        {
            throw new InvalidOperationException("Todos los registros deben pertenecer al mismo paciente");
        }

        // Verificar que el paciente existe
        var paciente = await _unitOfWork.Pacientes.GetByIdWithTenantAsync(pacienteId, tenantId.Value);
        if (paciente == null)
            throw new InvalidOperationException("Paciente no encontrado");

        // Validar y crear cada odontograma
        var odontogramasCreados = new List<Odontograma>();
        
        foreach (var dto in listaDtos)
        {
            // Validar número de diente
            if (!IsValidToothNumber(dto.NumeroDiente))
            {
                throw new InvalidOperationException($"El número de diente {dto.NumeroDiente} no es válido. Debe estar entre 11-18, 21-28, 31-38 o 41-48.");
            }

            var odontograma = new Odontograma
            {
                TenantId = tenantId.Value,
                PacienteId = dto.PacienteId,
                NumeroDiente = dto.NumeroDiente,
                Estado = dto.EstadoEnum,
                Observaciones = dto.Observaciones,
                FechaRegistro = dto.FechaRegistro ?? DateOnly.FromDateTime(DateTime.UtcNow),
                UsuarioId = usuarioId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Odontogramas.AddAsync(odontograma);
            odontogramasCreados.Add(odontograma);
        }

        // Guardar todos los cambios en una sola transacción
        await _unitOfWork.SaveChangesAsync();

        // Obtener los odontogramas con relaciones cargadas
        var resultado = new List<OdontogramaDto>();
        foreach (var odontograma in odontogramasCreados)
        {
            var odontogramaCompleto = await _unitOfWork.Odontogramas.GetByIdWithRelationsAsync(odontograma.Id, tenantId.Value);
            if (odontogramaCompleto != null)
            {
                resultado.Add(MapToDto(odontogramaCompleto));
            }
        }

        return resultado;
    }

    /// <summary>
    /// Valida que el número de diente esté en el rango válido de la numeración dental
    /// </summary>
    private static bool IsValidToothNumber(int numeroDiente)
    {
        // Cuadrantes: 11-18 (superior derecho), 21-28 (superior izquierdo),
        //             31-38 (inferior izquierdo), 41-48 (inferior derecho)
        return (numeroDiente >= 11 && numeroDiente <= 18) ||
               (numeroDiente >= 21 && numeroDiente <= 28) ||
               (numeroDiente >= 31 && numeroDiente <= 38) ||
               (numeroDiente >= 41 && numeroDiente <= 48);
    }
}

