using SistemaDental.Domain.Entities;

namespace SistemaDental.Infrastructure.Repositories;

public interface IUsuarioRepository : IRepository<Usuario>
{
    Task<Usuario?> GetByEmailAsync(string email, Guid? tenantId = null);
    Task<Usuario?> GetByEmailAndTokenAsync(string email, string token, Guid? tenantId = null);
    Task<Usuario?> GetByEmailAndResetTokenAsync(string email, string token, Guid? tenantId = null);
    Task<IEnumerable<Usuario>> GetByTenantAsync(Guid tenantId);
    Task<bool> EmailExistsAsync(Guid tenantId, string email);
}

