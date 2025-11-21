using SistemaDental.Domain.Entities;
using SistemaDental.Application.DTOs.ScheduleConfig;
using SistemaDental.Infrastructure.Repositories;

namespace SistemaDental.Application.Services
{
    public class ScheduleConfigService : IScheduleConfigService
    {
        private readonly IScheduleConfigRepository _scheduleConfigRepository;

        public ScheduleConfigService(IScheduleConfigRepository scheduleConfigRepository)
        {
            _scheduleConfigRepository = scheduleConfigRepository;
        }

        public async Task<IEnumerable<ScheduleConfigDto>> GetScheduleAsync(Guid tenantId, Guid? userId)
        {
            var configs = await _scheduleConfigRepository.GetByTenantAndUserAsync(tenantId, userId);

            return configs.Select(config => new ScheduleConfigDto
            {
                Id = config.Id,
                DayOfWeek = config.DayOfWeek,
                IsWorkingDay = config.IsWorkingDay,
                MorningStartTime = config.MorningStartTime,
                MorningEndTime = config.MorningEndTime,
                AfternoonStartTime = config.AfternoonStartTime,
                AfternoonEndTime = config.AfternoonEndTime,
                AppointmentDuration = config.AppointmentDuration,
                IsActive = config.IsActive
            });
        }

        public async Task UpsertScheduleAsync(Guid tenantId, Guid? userId, IEnumerable<ScheduleConfigUpsertDto> configs)
        {
            var entities = configs.Select(dto => new ScheduleConfig
            {
                TenantId = tenantId,
                UserId = userId,
                DayOfWeek = dto.DayOfWeek,
                IsWorkingDay = dto.IsWorkingDay,
                MorningStartTime = dto.MorningStartTime,
                MorningEndTime = dto.MorningEndTime,
                AfternoonStartTime = dto.AfternoonStartTime,
                AfternoonEndTime = dto.AfternoonEndTime,
                AppointmentDuration = dto.AppointmentDuration,
                IsActive = true, // Siempre se guarda como activo
                CreatedAt = DateTime.UtcNow
            });

            await _scheduleConfigRepository.UpsertByTenantAndUserAsync(tenantId, userId, entities);
        }

        public async Task<WorkHoursDto> GetConsolidatedWorkHoursAsync(Guid tenantId)
        {
            var schedules = await _scheduleConfigRepository.GetActiveSchedulesForTenantAsync(tenantId);

            if (!schedules.Any())
            {
                // Devuelve un horario por defecto si no se encuentra ninguna configuración
                return new WorkHoursDto { WorkDayStart = "09:00", WorkDayEnd = "18:00" };
            }

            TimeSpan? minStartTime = null;
            TimeSpan? maxEndTime = null;

            foreach (var schedule in schedules)
            {
                // Encontrar la hora de inicio más temprana
                if (schedule.MorningStartTime.HasValue)
                {
                    if (!minStartTime.HasValue || schedule.MorningStartTime.Value < minStartTime.Value)
                    {
                        minStartTime = schedule.MorningStartTime.Value;
                    }
                }

                // Encontrar la hora de finalización más tardía (considerando turno tarde o mañana)
                var lastTimeOfDay = schedule.AfternoonEndTime ?? schedule.MorningEndTime;
                if (lastTimeOfDay.HasValue)
                {
                    if (!maxEndTime.HasValue || lastTimeOfDay.Value > maxEndTime.Value)
                    {
                        maxEndTime = lastTimeOfDay.Value;
                    }
                }
            }

            return new WorkHoursDto
            {
                WorkDayStart = minStartTime?.ToString(@"hh\:mm") ?? "09:00",
                WorkDayEnd = maxEndTime?.ToString(@"hh\:mm") ?? "18:00"
            };
        }
    }
}