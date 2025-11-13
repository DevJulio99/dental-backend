using Microsoft.EntityFrameworkCore;
using SistemaDental.Domain.Entities;
using SistemaDental.Domain.Enums;
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
        // Usar Status directamente ya que Activo es una propiedad calculada que no se puede usar en LINQ
        return await _dbSet
            .FirstOrDefaultAsync(t => t.Subdominio == normalizedSubdomain 
                && (t.Status == TenantStatus.Active || t.Status == TenantStatus.Trial));
    }

    public async Task<bool> SubdomainExistsAsync(string subdomain)
    {
        // Normalizar a minúsculas (todos los subdominios se guardan en minúsculas)
        var normalizedSubdomain = subdomain.ToLower().Trim();
        return await _dbSet.AnyAsync(t => t.Subdominio == normalizedSubdomain);
    }
}

