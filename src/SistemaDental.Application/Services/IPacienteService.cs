using SistemaDental.Application.DTOs.Paciente;

namespace SistemaDental.Application.Services;

public interface IPacienteService
{
    Task<PacienteDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<PacienteDto>> GetAllAsync();
    Task<IEnumerable<PacienteDto>> SearchAsync(string searchTerm);
    Task<PacienteDto> CreateAsync(PacienteCreateDto dto);
    Task<PacienteDto?> UpdateAsync(Guid id, PacienteCreateDto dto);
    Task<bool> DeleteAsync(Guid id);
}

