using Microsoft.EntityFrameworkCore;
using SistemaDental.Domain.Entities;
using SistemaDental.Infrastructure.Data;
using SistemaDental.Infrastructure.Services;

namespace SistemaDental.Infrastructure.Repositories;

public class PacienteRepository : Repository<Paciente>, IPacienteRepository
{
    public PacienteRepository(ApplicationDbContext context, ITenantService tenantService)
        : base(context, tenantService)
    {
    }

    public async Task<Paciente?> GetByIdWithTenantAsync(Guid id, Guid tenantId)
    {
        return await _dbSet
            .Where(p => p.Id == id && p.TenantId == tenantId && p.DeletedAt == null)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Paciente>> GetByTenantAsync(Guid tenantId)
    {
        return await _dbSet
            .Where(p => p.TenantId == tenantId && p.DeletedAt == null)
            .OrderByDescending(p => p.FechaCreacion)
            .ToListAsync();
    }

    public async Task<IEnumerable<Paciente>> SearchAsync(Guid tenantId, string searchTerm)
    {
        var term = searchTerm.ToLower();
        return await _dbSet
            .Where(p => p.TenantId == tenantId && p.DeletedAt == null &&
                       (p.FirstName.ToLower().Contains(term) ||
                        p.LastName.ToLower().Contains(term) ||
                        p.DniPasaporte.ToLower().Contains(term) ||
                        (p.Email != null && p.Email.ToLower().Contains(term)) ||
                        p.Telefono.Contains(term)))
            .OrderByDescending(p => p.FechaCreacion)
            .ToListAsync();
    }

    public async Task<bool> ExistsByDniAsync(Guid tenantId, string dni)
    {
        return await _dbSet
            .AnyAsync(p => p.TenantId == tenantId && p.DniPasaporte == dni && p.DeletedAt == null);
    }
}

