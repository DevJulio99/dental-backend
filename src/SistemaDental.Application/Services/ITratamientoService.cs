using SistemaDental.Application.DTOs.Tratamiento;

namespace SistemaDental.Application.Services;

public interface ITratamientoService
{
    Task<TratamientoDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<TratamientoDto>> GetAllAsync();
    Task<IEnumerable<TratamientoDto>> GetByPacienteAsync(Guid pacienteId);
    Task<IEnumerable<TratamientoDto>> GetByCitaAsync(Guid citaId);
    Task<TratamientoDto> CreateAsync(TratamientoCreateDto dto);
    Task<TratamientoDto?> UpdateAsync(Guid id, TratamientoCreateDto dto);
    Task<bool> DeleteAsync(Guid id);
}

