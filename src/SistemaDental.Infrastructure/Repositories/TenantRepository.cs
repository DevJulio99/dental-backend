using Microsoft.EntityFrameworkCore;
using SistemaDental.Domain.Entities;
using SistemaDental.Infrastructure.Data;
using SistemaDental.Infrastructure.Services;

namespace SistemaDental.Infrastructure.Repositories;

public class TenantRepository : Repository<Tenant>, ITenantRepository
{
    public TenantRepository(ApplicationDbContext context, ITenantService tenantService)
        : base(context, tenantService)
    {
    }

    public async Task<Tenant?> GetBySubdomainAsync(string subdomain)
    {
        // Normalizar a minúsculas (todos los subdominios se guardan en minúsculas)
        var normalizedSubdomain = subdomain.ToLower().Trim();
        // Buscar por slug (que es el campo subdomain en la BD)
        return await _dbSet
            .FirstOrDefaultAsync(t => t.Subdominio == normalizedSubdomain && t.Activo);
    }

    public async Task<bool> SubdomainExistsAsync(string subdomain)
    {
        // Normalizar a minúsculas (todos los subdominios se guardan en minúsculas)
        var normalizedSubdomain = subdomain.ToLower().Trim();
        return await _dbSet.AnyAsync(t => t.Subdominio == normalizedSubdomain);
    }
}

