using Microsoft.EntityFrameworkCore;
using SistemaDental.Domain.Entities;
using SistemaDental.Infrastructure.Data;
using SistemaDental.Infrastructure.Services;

namespace SistemaDental.Infrastructure.Repositories;

public class CitaRepository : Repository<Cita>, ICitaRepository
{
    public CitaRepository(ApplicationDbContext context, ITenantService tenantService)
        : base(context, tenantService)
    {
    }

    public async Task<Cita?> GetByIdWithRelationsAsync(Guid id, Guid tenantId)
    {
        return await _dbSet
            .Include(c => c.Paciente)
            .Include(c => c.Usuario)
            .Where(c => c.Id == id && c.TenantId == tenantId && c.DeletedAt == null)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Cita>> GetByTenantAsync(Guid tenantId)
    {
        return await _dbSet
            .Include(c => c.Paciente)
            .Include(c => c.Usuario)
            .Where(c => c.TenantId == tenantId && c.DeletedAt == null)
            .OrderBy(c => c.AppointmentDate)
            .ThenBy(c => c.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Cita>> GetByPacienteAsync(Guid tenantId, Guid pacienteId)
    {
        return await _dbSet
            .Include(c => c.Paciente)
            .Include(c => c.Usuario)
            .Where(c => c.TenantId == tenantId && c.PacienteId == pacienteId && c.DeletedAt == null)
            .OrderByDescending(c => c.AppointmentDate)
            .ThenByDescending(c => c.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Cita>> GetByUsuarioAsync(Guid tenantId, Guid usuarioId)
    {
        return await _dbSet
            .Where(c => c.TenantId == tenantId && c.UsuarioId == usuarioId && c.DeletedAt == null)
            .OrderByDescending(c => c.AppointmentDate)
            .ThenBy(c => c.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Cita>> GetByDateRangeAsync(Guid tenantId, DateOnly startDate, DateOnly endDate)
    {
        return await _dbSet
            .Include(c => c.Paciente)
            .Include(c => c.Usuario)
            .Where(c => c.TenantId == tenantId && 
                       c.DeletedAt == null &&
                       c.AppointmentDate >= startDate && 
                       c.AppointmentDate <= endDate)
            .OrderBy(c => c.AppointmentDate)
            .ThenBy(c => c.StartTime)
            .ToListAsync();
    }

    public async Task<bool> HasConflictAsync(Guid tenantId, DateOnly date, TimeOnly startTime, TimeOnly endTime, Guid usuarioId, Guid? excludeCitaId = null)
    {
        var query = _dbSet
            .Where(c => c.TenantId == tenantId &&
                       c.DeletedAt == null &&
                       c.Estado != "cancelled" &&
                       c.AppointmentDate == date &&
                       c.StartTime < endTime &&
                       c.EndTime > startTime &&
                       c.UsuarioId == usuarioId);

        if (excludeCitaId.HasValue)
        {
            query = query.Where(c => c.Id != excludeCitaId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<IEnumerable<Cita>> GetOcupadasByDateAsync(Guid tenantId, DateOnly date, Guid? usuarioId = null)
    {
        var query = _dbSet
            .Where(c => c.TenantId == tenantId &&
                       c.DeletedAt == null &&
                       c.AppointmentDate == date &&
                       c.Estado != "cancelled");

        if (usuarioId.HasValue)
        {
            query = query.Where(c => c.UsuarioId == usuarioId.Value);
        }

        return await query.ToListAsync();
    }
}

