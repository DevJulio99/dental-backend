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

    public async Task<IEnumerable<Odontograma>> GetByPacienteAsync(Guid tenantId, Guid pacienteId, DateOnly? fechaDesde, DateOnly? fechaHasta)
    {
        var query = _dbSet
            .Include(o => o.Usuario)
            .Where(o => o.TenantId == tenantId && o.PacienteId == pacienteId);

        if (fechaDesde.HasValue)
        {
            query = query.Where(o => o.FechaRegistro >= fechaDesde.Value);
        }

        if (fechaHasta.HasValue)
        {
            query = query.Where(o => o.FechaRegistro <= fechaHasta.Value);
        }

        return await query
            .OrderByDescending(o => o.FechaRegistro)
            .ThenByDescending(o => o.CreatedAt)
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

    public async Task<IEnumerable<Odontograma>> GetHistorialByDienteAsync(Guid tenantId, Guid pacienteId, int numeroDiente)
    {
        return await _dbSet
            .Include(o => o.Usuario)
            .Where(o => o.TenantId == tenantId && 
                       o.PacienteId == pacienteId && 
                       o.NumeroDiente == numeroDiente)
            .OrderByDescending(o => o.FechaRegistro)
            .ThenByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<Dictionary<int, Odontograma?>> GetEstadoDientesEnFechaAsync(Guid tenantId, Guid pacienteId, DateOnly fecha)
    {
        // Obtener todos los dientes válidos
        var dientes = new List<int> { 11, 12, 13, 14, 15, 16, 17, 18, 21, 22, 23, 24, 25, 26, 27, 28,
                                      31, 32, 33, 34, 35, 36, 37, 38, 41, 42, 43, 44, 45, 46, 47, 48 };

        // Obtener todos los registros hasta la fecha especificada en una sola consulta
        var registros = await _dbSet
            .Include(o => o.Usuario)
            .Where(o => o.TenantId == tenantId && 
                       o.PacienteId == pacienteId && 
                       o.FechaRegistro <= fecha &&
                       dientes.Contains(o.NumeroDiente))
            .OrderByDescending(o => o.FechaRegistro)
            .ThenByDescending(o => o.CreatedAt)
            .ToListAsync();

        // Agrupar por número de diente y tomar el más reciente de cada uno
        var resultado = new Dictionary<int, Odontograma?>();
        
        foreach (var numeroDiente in dientes)
        {
            resultado[numeroDiente] = registros
                .Where(r => r.NumeroDiente == numeroDiente)
                .FirstOrDefault();
        }

        return resultado;
    }
}

