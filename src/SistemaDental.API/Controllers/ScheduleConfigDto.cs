namespace SistemaDental.Application.DTOs.ScheduleConfig
{
    public class ScheduleConfigDto
    {
        public Guid Id { get; set; }
        public int DayOfWeek { get; set; }
        public bool IsWorkingDay { get; set; }
        public TimeSpan? MorningStartTime { get; set; }
        public TimeSpan? MorningEndTime { get; set; }
        public TimeSpan? AfternoonStartTime { get; set; }
        public TimeSpan? AfternoonEndTime { get; set; }
        public int AppointmentDuration { get; set; }
        public bool IsActive { get; set; }
    }
}