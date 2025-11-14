using SistemaDental.Domain.Entities;

namespace SistemaDental.Infrastructure.Repositories;

public interface IOdontogramaRepository : IRepository<Odontograma>
{
    Task<Odontograma?> GetByIdWithRelationsAsync(Guid id, Guid tenantId);
    Task<IEnumerable<Odontograma>> GetByPacienteAsync(Guid tenantId, Guid pacienteId);
    Task<IEnumerable<Odontograma>> GetByPacienteAsync(Guid tenantId, Guid pacienteId, DateOnly? fechaDesde, DateOnly? fechaHasta);
    Task<IEnumerable<Odontograma>> GetByTenantAsync(Guid tenantId);
    Task<Odontograma?> GetByDienteAsync(Guid tenantId, Guid pacienteId, int numeroDiente);
    Task<Odontograma?> GetByDienteLatestAsync(Guid tenantId, Guid pacienteId, int numeroDiente);
    Task<IEnumerable<Odontograma>> GetHistorialByDienteAsync(Guid tenantId, Guid pacienteId, int numeroDiente);
    Task<Dictionary<int, Odontograma?>> GetEstadoDientesEnFechaAsync(Guid tenantId, Guid pacienteId, DateOnly fecha);
}

