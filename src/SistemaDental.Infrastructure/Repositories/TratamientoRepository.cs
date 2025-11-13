using Microsoft.EntityFrameworkCore;
using SistemaDental.Domain.Entities;
using SistemaDental.Infrastructure.Data;
using SistemaDental.Infrastructure.Services;

namespace SistemaDental.Infrastructure.Repositories;

public class TratamientoRepository : Repository<Tratamiento>, ITratamientoRepository
{
    public TratamientoRepository(ApplicationDbContext context, ITenantService tenantService)
        : base(context, tenantService)
    {
    }

    public async Task<Tratamiento?> GetByIdWithRelationsAsync(Guid id, Guid tenantId)
    {
        return await _dbSet
            .Include(t => t.Paciente)
            .Include(t => t.Usuario)
            .Where(t => t.Id == id && t.TenantId == tenantId)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Tratamiento>> GetByTenantAsync(Guid tenantId)
    {
        return await _dbSet
            .Include(t => t.Paciente)
            .Include(t => t.Usuario)
            .Where(t => t.TenantId == tenantId)
            .OrderByDescending(t => t.TreatmentDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Tratamiento>> GetByPacienteAsync(Guid tenantId, Guid pacienteId)
    {
        return await _dbSet
            .Include(t => t.Usuario)
            .Where(t => t.TenantId == tenantId && t.PacienteId == pacienteId)
            .OrderByDescending(t => t.TreatmentDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Tratamiento>> GetByCitaAsync(Guid tenantId, Guid citaId)
    {
        return await _dbSet
            .Include(t => t.Paciente)
            .Include(t => t.Usuario)
            .Where(t => t.TenantId == tenantId && t.CitaId == citaId)
            .OrderByDescending(t => t.TreatmentDate)
            .ToListAsync();
    }
}

