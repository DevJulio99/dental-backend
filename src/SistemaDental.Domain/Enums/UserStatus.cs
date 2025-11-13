using System.Runtime.Serialization;

namespace SistemaDental.Domain.Enums;

public enum UserStatus
{
    [EnumMember(Value = "active")]
    Active,
    
    [EnumMember(Value = "inactive")]
    Inactive,
    
    [EnumMember(Value = "suspended")]
    Suspended
}
