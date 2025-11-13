using SistemaDental.Application.DTOs.Odontograma;

namespace SistemaDental.Application.Services;

public interface IOdontogramaService
{
    Task<IEnumerable<OdontogramaDto>> GetByPacienteAsync(Guid pacienteId);
    Task<OdontogramaDto> CreateAsync(OdontogramaCreateDto dto);
    Task<OdontogramaDto?> UpdateAsync(Guid id, OdontogramaCreateDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<Dictionary<int, OdontogramaDto?>> GetEstadoActualDientesAsync(Guid pacienteId);
}

