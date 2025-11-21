using System.Text.Json.Serialization;
using SistemaDental.API.Converters;

namespace SistemaDental.Application.DTOs.ScheduleConfig
{
    public class ScheduleConfigUpsertDto
    {
        public int DayOfWeek { get; set; }
        public bool IsWorkingDay { get; set; }
        [JsonConverter(typeof(TimeSpanConverter))]
        public TimeSpan? MorningStartTime { get; set; }
        [JsonConverter(typeof(TimeSpanConverter))]
        public TimeSpan? MorningEndTime { get; set; }
        [JsonConverter(typeof(TimeSpanConverter))]
        public TimeSpan? AfternoonStartTime { get; set; }
        [JsonConverter(typeof(TimeSpanConverter))]
        public TimeSpan? AfternoonEndTime { get; set; }
        public int AppointmentDuration { get; set; }
    }
}