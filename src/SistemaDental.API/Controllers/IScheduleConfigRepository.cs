using SistemaDental.Domain.Entities;

namespace SistemaDental.Infrastructure.Repositories
{
    public interface IScheduleConfigRepository
    {
        Task<IEnumerable<ScheduleConfig>> GetByTenantAndUserAsync(Guid tenantId, Guid? userId);
        Task UpsertByTenantAndUserAsync(Guid tenantId, Guid? userId, IEnumerable<ScheduleConfig> configs);
        Task<IEnumerable<ScheduleConfig>> GetActiveSchedulesForTenantAsync(Guid tenantId);
    }
}