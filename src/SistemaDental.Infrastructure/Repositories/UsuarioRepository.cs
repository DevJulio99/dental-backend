using Microsoft.EntityFrameworkCore;
using SistemaDental.Domain.Entities;
using SistemaDental.Infrastructure.Data;
using SistemaDental.Infrastructure.Services;

namespace SistemaDental.Infrastructure.Repositories;

public class UsuarioRepository : Repository<Usuario>, IUsuarioRepository
{
    public UsuarioRepository(ApplicationDbContext context, ITenantService tenantService)
        : base(context, tenantService)
    {
    }

    public async Task<Usuario?> GetByEmailAsync(string email, Guid? tenantId = null)
    {
        var query = _dbSet
            .Include(u => u.Tenant)
            .Where(u => u.Email == email && u.Activo);
        
        if (tenantId.HasValue)
        {
            query = query.Where(u => u.TenantId == tenantId.Value);
        }
        
        return await query.FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Usuario>> GetByTenantAsync(Guid tenantId)
    {
        return await _dbSet
            .Where(u => u.TenantId == tenantId && u.Activo)
            .OrderBy(u => u.Nombre)
            .ThenBy(u => u.Apellido)
            .ToListAsync();
    }

    public async Task<Usuario?> GetByEmailAndTokenAsync(string email, string token, Guid? tenantId = null)
    {
        var query = _dbSet
            .Where(u => u.Email == email 
                && u.EmailVerificationToken == token
                && !u.EmailVerified
                && u.Activo);
        
        if (tenantId.HasValue)
        {
            query = query.Where(u => u.TenantId == tenantId.Value);
        }
        
        return await query.FirstOrDefaultAsync();
    }

    public async Task<Usuario?> GetByEmailAndResetTokenAsync(string email, string token, Guid? tenantId = null)
    {
        var query = _dbSet
            .Where(u => u.Email == email 
                && u.PasswordResetToken == token
                && u.PasswordResetExpires.HasValue
                && u.PasswordResetExpires.Value > DateTime.UtcNow
                && u.Activo);
        
        if (tenantId.HasValue)
        {
            query = query.Where(u => u.TenantId == tenantId.Value);
        }
        
        return await query.FirstOrDefaultAsync();
    }

    public async Task<bool> EmailExistsAsync(Guid tenantId, string email)
    {
        return await _dbSet
            .AnyAsync(u => u.TenantId == tenantId && u.Email == email);
    }
}

