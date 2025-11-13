using SistemaDental.Domain.Entities;

namespace SistemaDental.Infrastructure.Repositories;

public interface ITratamientoRepository : IRepository<Tratamiento>
{
    Task<Tratamiento?> GetByIdWithRelationsAsync(Guid id, Guid tenantId);
    Task<IEnumerable<Tratamiento>> GetByTenantAsync(Guid tenantId);
    Task<IEnumerable<Tratamiento>> GetByPacienteAsync(Guid tenantId, Guid pacienteId);
    Task<IEnumerable<Tratamiento>> GetByCitaAsync(Guid tenantId, Guid citaId);
}

