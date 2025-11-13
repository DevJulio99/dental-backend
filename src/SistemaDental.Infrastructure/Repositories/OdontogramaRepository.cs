using Microsoft.EntityFrameworkCore;
using SistemaDental.Domain.Entities;
using SistemaDental.Infrastructure.Data;
using SistemaDental.Infrastructure.Services;

namespace SistemaDental.Infrastructure.Repositories;

public class OdontogramaRepository : Repository<Odontograma>, IOdontogramaRepository
{
    public OdontogramaRepository(ApplicationDbContext context, ITenantService tenantService)
        : base(context, tenantService)
    {
    }

    public async Task<Odontograma?> GetByIdWithRelationsAsync(Guid id, Guid tenantId)
    {
        return await _dbSet
            .Include(o => o.Usuario)
            .Where(o => o.Id == id && o.TenantId == tenantId)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Odontograma>> GetByPacienteAsync(Guid tenantId, Guid pacienteId)
    {
        return await _dbSet
            .Include(o => o.Usuario)
            .Where(o => o.TenantId == tenantId && o.PacienteId == pacienteId)
            .OrderByDescending(o => o.FechaRegistro)
            .ToListAsync();
    }

    public async Task<IEnumerable<Odontograma>> GetByTenantAsync(Guid tenantId)
    {
        return await _dbSet
            .Where(o => o.TenantId == tenantId)
            .OrderByDescending(o => o.FechaRegistro)
            .ToListAsync();
    }

    public async Task<Odontograma?> GetByDienteAsync(Guid tenantId, Guid pacienteId, int numeroDiente)
    {
        return await _dbSet
            .Where(o => o.TenantId == tenantId && 
                       o.PacienteId == pacienteId && 
                       o.NumeroDiente == numeroDiente)
            .OrderByDescending(o => o.FechaRegistro)
            .FirstOrDefaultAsync();
    }

    public async Task<Odontograma?> GetByDienteLatestAsync(Guid tenantId, Guid pacienteId, int numeroDiente)
    {
        return await _dbSet
            .Include(o => o.Usuario)
            .Where(o => o.TenantId == tenantId && 
                       o.PacienteId == pacienteId && 
                       o.NumeroDiente == numeroDiente)
            .OrderByDescending(o => o.FechaRegistro)
            .FirstOrDefaultAsync();
    }
}

