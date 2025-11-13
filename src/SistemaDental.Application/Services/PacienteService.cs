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
            TipoDocumento = dto.TipoDocumento,
            DniPasaporte = dto.DniPasaporte,
            FechaNacimiento = dto.FechaNacimiento,
            Genero = dto.Genero,
            Telefono = dto.Telefono,
            TelefonoAlternativo = dto.TelefonoAlternativo,
            Email = dto.Email,
            Direccion = dto.Direccion,
            Ciudad = dto.Ciudad,
            TipoSangre = dto.TipoSangre,
            Alergias = dto.Alergias,
            CondicionesMedicas = dto.CondicionesMedicas,
            MedicamentosActuales = dto.MedicamentosActuales,
            ContactoEmergenciaNombre = dto.ContactoEmergenciaNombre,
            ContactoEmergenciaTelefono = dto.ContactoEmergenciaTelefono,
            SeguroDental = dto.SeguroDental,
            NumeroSeguro = dto.NumeroSeguro,
            FotoUrl = dto.FotoUrl,
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
        paciente.TipoDocumento = dto.TipoDocumento;
        paciente.DniPasaporte = dto.DniPasaporte;
        paciente.FechaNacimiento = dto.FechaNacimiento;
        paciente.Genero = dto.Genero;
        paciente.Telefono = dto.Telefono;
        paciente.TelefonoAlternativo = dto.TelefonoAlternativo;
        paciente.Email = dto.Email;
        paciente.Direccion = dto.Direccion;
        paciente.Ciudad = dto.Ciudad;
        paciente.TipoSangre = dto.TipoSangre;
        paciente.Alergias = dto.Alergias;
        paciente.CondicionesMedicas = dto.CondicionesMedicas;
        paciente.MedicamentosActuales = dto.MedicamentosActuales;
        paciente.ContactoEmergenciaNombre = dto.ContactoEmergenciaNombre;
        paciente.ContactoEmergenciaTelefono = dto.ContactoEmergenciaTelefono;
        paciente.SeguroDental = dto.SeguroDental;
        paciente.NumeroSeguro = dto.NumeroSeguro;
        paciente.FotoUrl = dto.FotoUrl;
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
            TipoDocumento = paciente.TipoDocumento,
            DniPasaporte = paciente.DniPasaporte,
            FechaNacimiento = paciente.FechaNacimiento,
            Genero = paciente.Genero,
            Telefono = paciente.Telefono,
            TelefonoAlternativo = paciente.TelefonoAlternativo,
            Email = paciente.Email,
            Direccion = paciente.Direccion,
            Ciudad = paciente.Ciudad,
            TipoSangre = paciente.TipoSangre,
            Alergias = paciente.Alergias,
            CondicionesMedicas = paciente.CondicionesMedicas,
            MedicamentosActuales = paciente.MedicamentosActuales,
            ContactoEmergenciaNombre = paciente.ContactoEmergenciaNombre,
            ContactoEmergenciaTelefono = paciente.ContactoEmergenciaTelefono,
            SeguroDental = paciente.SeguroDental,
            NumeroSeguro = paciente.NumeroSeguro,
            FotoUrl = paciente.FotoUrl,
            Observaciones = paciente.Observaciones,
            FechaCreacion = paciente.FechaCreacion,
            FechaUltimaCita = paciente.FechaUltimaCita
        };
    }
}

