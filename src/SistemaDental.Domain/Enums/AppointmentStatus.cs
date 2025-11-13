using System.Runtime.Serialization;

namespace SistemaDental.Domain.Enums;

public enum AppointmentStatus
{
    [EnumMember(Value = "scheduled")]
    Scheduled,
    
    [EnumMember(Value = "confirmed")]
    Confirmed,
    
    [EnumMember(Value = "in_progress")]
    InProgress,
    
    [EnumMember(Value = "completed")]
    Completed,
    
    [EnumMember(Value = "cancelled")]
    Cancelled,
    
    [EnumMember(Value = "no_show")]
    NoShow
}
