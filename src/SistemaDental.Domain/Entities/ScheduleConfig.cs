namespace SistemaDental.Domain.Entities
{
    public class ScheduleConfig
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid? UserId { get; set; } // Puede ser nulo para configuraciones a nivel de tenant
        public int DayOfWeek { get; set; }
        public bool IsWorkingDay { get; set; }
        public TimeSpan? MorningStartTime { get; set; }
        public TimeSpan? MorningEndTime { get; set; }
        public TimeSpan? AfternoonStartTime { get; set; }
        public TimeSpan? AfternoonEndTime { get; set; }
        public int AppointmentDuration { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public virtual Tenant Tenant { get; set; }
        public virtual Usuario User { get; set; }
    }
}
