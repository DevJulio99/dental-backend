using Microsoft.Extensions.Logging;
using SistemaDental.Application.DTOs.Paciente;
using SistemaDental.Domain.Entities;
using SistemaDental.Infrastructure.Repositories;
using SistemaDental.Infrastructure.Services;

namespace SistemaDental.Application.Services;

public class PacienteService : IPacienteService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantService _tenantService;
    private readonly ILogger<PacienteService> _logger;

    public PacienteService(
        IUnitOfWork unitOfWork,
        ITenantService tenantService,
        ILogger<PacienteService> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantService = tenantService;
        _logger = logger;
    }

    public async Task<PacienteDto?> GetByIdAsync(Guid id)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue) return null;

        var paciente = await _unitOfWork.Pacientes.GetByIdWithTenantAsync(id, tenantId.Value);
        if (paciente == null) return null;

        return MapToDto(paciente);
    }

    public async Task<IEnumerable<PacienteDto>> GetAllAsync()
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue) return Enumerable.Empty<PacienteDto>();

        var pacientes = await _unitOfWork.Pacientes.GetByTenantAsync(tenantId.Value);
        return pacientes.Select(MapToDto);
    }

    public async Task<IEnumerable<PacienteDto>> SearchAsync(string searchTerm)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue) return Enumerable.Empty<PacienteDto>();

        var pacientes = await _unitOfWork.Pacientes.SearchAsync(tenantId.Value, searchTerm);
        return pacientes.Select(MapToDto);
    }

    public async Task<PacienteDto> CreateAsync(PacienteCreateDto dto)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue)
            throw new InvalidOperationException("Tenant no identificado");

        // Verificar que el DNI no exista
        if (await _unitOfWork.Pacientes.ExistsByDniAsync(tenantId.Value, dto.DniPasaporte))
        {
            throw new InvalidOperationException("Ya existe un paciente con este DNI/Pasaporte");
        }

        var paciente = new Paciente
        {
            TenantId = tenantId.Value,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            DniPasaporte = dto.DniPasaporte,
            FechaNacimiento = dto.FechaNacimiento,
            Telefono = dto.Telefono,
            Email = dto.Email,
            Direccion = dto.Direccion,
            Alergias = dto.Alergias,
            Observaciones = dto.Observaciones,
            FechaCreacion = DateTime.UtcNow
        };

        await _unitOfWork.Pacientes.AddAsync(paciente);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(paciente);
    }

    public async Task<PacienteDto?> UpdateAsync(Guid id, PacienteCreateDto dto)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue) return null;

        var paciente = await _unitOfWork.Pacientes.GetByIdWithTenantAsync(id, tenantId.Value);
        if (paciente == null) return null;

        paciente.FirstName = dto.FirstName;
        paciente.LastName = dto.LastName;
        paciente.DniPasaporte = dto.DniPasaporte;
        paciente.FechaNacimiento = dto.FechaNacimiento;
        paciente.Telefono = dto.Telefono;
        paciente.Email = dto.Email;
        paciente.Direccion = dto.Direccion;
        paciente.Alergias = dto.Alergias;
        paciente.Observaciones = dto.Observaciones;
        paciente.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Pacientes.UpdateAsync(paciente);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(paciente);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue) return false;

        var paciente = await _unitOfWork.Pacientes.GetByIdWithTenantAsync(id, tenantId.Value);
        if (paciente == null) return false;

        // Soft delete: establecer DeletedAt en lugar de eliminar físicamente
        paciente.DeletedAt = DateTime.UtcNow;
        paciente.UpdatedAt = DateTime.UtcNow;
        
        await _unitOfWork.Pacientes.UpdateAsync(paciente);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    private static PacienteDto MapToDto(Paciente paciente)
    {
        return new PacienteDto
        {
            Id = paciente.Id,
            FirstName = paciente.FirstName,
            LastName = paciente.LastName,
            DniPasaporte = paciente.DniPasaporte,
            FechaNacimiento = paciente.FechaNacimiento,
            Telefono = paciente.Telefono,
            Email = paciente.Email,
            Direccion = paciente.Direccion,
            Alergias = paciente.Alergias,
            Observaciones = paciente.Observaciones,
            FechaCreacion = paciente.FechaCreacion,
            FechaUltimaCita = paciente.FechaUltimaCita
        };
    }
}

