using SistemaDental.Application.DTOs.ScheduleConfig;

namespace SistemaDental.Application.Services
{
    public interface IScheduleConfigService
    {
        Task<IEnumerable<ScheduleConfigDto>> GetScheduleAsync(Guid tenantId, Guid? userId);
        Task<WorkHoursDto> GetConsolidatedWorkHoursAsync(Guid tenantId);
        Task UpsertScheduleAsync(Guid tenantId, Guid? userId, IEnumerable<ScheduleConfigUpsertDto> configs);
    }
}