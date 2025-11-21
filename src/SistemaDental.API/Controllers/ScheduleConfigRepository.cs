using Microsoft.EntityFrameworkCore;
using SistemaDental.Domain.Enums;
using SistemaDental.Domain.Entities;
using SistemaDental.Infrastructure.Data;

namespace SistemaDental.Infrastructure.Repositories
{
    public class ScheduleConfigRepository : IScheduleConfigRepository
    {
        private readonly ApplicationDbContext _context;

        public ScheduleConfigRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ScheduleConfig>> GetByTenantAndUserAsync(Guid tenantId, Guid? userId)
        {
            var query = _context.ScheduleConfigs
                .Where(sc => sc.TenantId == tenantId && sc.IsActive);

            if (userId.HasValue)
            {
                // Si se especifica un usuario, buscar su configuración específica
                query = query.Where(sc => sc.UserId == userId.Value);
            }
            else
            {
                // Si no se especifica usuario, buscar la configuración general del tenant (sin usuario asignado)
                query = query.Where(sc => sc.UserId == null);
            }

            return await query.OrderBy(sc => sc.DayOfWeek).ToListAsync();
        }

        public async Task UpsertByTenantAndUserAsync(Guid tenantId, Guid? userId, IEnumerable<ScheduleConfig> configs)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var daysToUpdate = configs.Select(c => c.DayOfWeek).ToList();

                // Obtener las configuraciones existentes para los días especificados
                var existingConfigs = await _context.ScheduleConfigs
                    .Where(sc => sc.TenantId == tenantId && sc.UserId == userId && daysToUpdate.Contains(sc.DayOfWeek))
                    .ToListAsync();

                foreach (var config in configs)
                {
                    var existing = existingConfigs.FirstOrDefault(ec => ec.DayOfWeek == config.DayOfWeek);
                    if (existing != null)
                    {
                        // Actualizar
                        existing.IsWorkingDay = config.IsWorkingDay;
                        existing.MorningStartTime = config.MorningStartTime;
                        existing.MorningEndTime = config.MorningEndTime;
                        existing.AfternoonStartTime = config.AfternoonStartTime;
                        existing.AfternoonEndTime = config.AfternoonEndTime;
                        existing.AppointmentDuration = config.AppointmentDuration;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        await _context.ScheduleConfigs.AddAsync(config);
                    }
                }
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<IEnumerable<ScheduleConfig>> GetActiveSchedulesForTenantAsync(Guid tenantId)
        {
            return await _context.ScheduleConfigs
                .Include(sc => sc.User) // Incluir el usuario para filtrar por rol
                .Where(sc => sc.TenantId == tenantId
                             && sc.IsActive
                             && sc.IsWorkingDay
                             && sc.User != null // Solo configuraciones de usuarios
                             && sc.User.Role == UserRole.Dentist // Solo Odontólogos
                             && sc.User.Status == UserStatus.Active) // Solo usuarios activos
                .ToListAsync();
        }
    }
}