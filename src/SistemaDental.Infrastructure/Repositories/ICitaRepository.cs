using SistemaDental.Domain.Entities;

namespace SistemaDental.Infrastructure.Repositories;

public interface ICitaRepository : IRepository<Cita>
{
    Task<Cita?> GetByIdWithRelationsAsync(Guid id, Guid tenantId);
    Task<IEnumerable<Cita>> GetByTenantAsync(Guid tenantId);
    Task<IEnumerable<Cita>> GetByPacienteAsync(Guid tenantId, Guid pacienteId);
    Task<IEnumerable<Cita>> GetByUsuarioAsync(Guid tenantId, Guid usuarioId);
    Task<IEnumerable<Cita>> GetByDateRangeAsync(Guid tenantId, DateOnly startDate, DateOnly endDate);
    Task<bool> HasConflictAsync(Guid tenantId, DateOnly date, TimeOnly startTime, TimeOnly endTime, Guid usuarioId, Guid? excludeCitaId = null);
    Task<IEnumerable<Cita>> GetOcupadasByDateAsync(Guid tenantId, DateOnly date, Guid? usuarioId = null);
}

