using SistemaDental.Domain.Entities;

namespace SistemaDental.Infrastructure.Repositories;

public interface ITenantRepository : IRepository<Tenant>
{
    Task<Tenant?> GetBySubdomainAsync(string subdomain);
    Task<bool> SubdomainExistsAsync(string subdomain);
}

