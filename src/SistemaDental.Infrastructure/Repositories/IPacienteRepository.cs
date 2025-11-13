using SistemaDental.Domain.Entities;

namespace SistemaDental.Infrastructure.Repositories;

public interface IPacienteRepository : IRepository<Paciente>
{
    Task<Paciente?> GetByIdWithTenantAsync(Guid id, Guid tenantId);
    Task<IEnumerable<Paciente>> GetByTenantAsync(Guid tenantId);
    Task<IEnumerable<Paciente>> SearchAsync(Guid tenantId, string searchTerm);
    Task<bool> ExistsByDniAsync(Guid tenantId, string dni);
}

