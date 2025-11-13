using SistemaDental.Application.DTOs.Cita;

namespace SistemaDental.Application.Services;

public interface ICitaService
{
    Task<CitaDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<CitaDto>> GetAllAsync();
    Task<IEnumerable<CitaDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<CitaDto>> GetByPacienteAsync(Guid pacienteId);
    Task<CitaDto> CreateAsync(CitaCreateDto dto);
    Task<CitaDto?> UpdateAsync(Guid id, CitaCreateDto dto);
    Task<bool> ConfirmAsync(Guid id);
    Task<bool> CancelAsync(Guid id);
    Task<bool> DeleteAsync(Guid id);
    Task<IEnumerable<DateTime>> GetAvailableSlotsAsync(DateTime date, Guid? usuarioId = null);
}

