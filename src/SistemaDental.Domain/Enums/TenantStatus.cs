using System.Runtime.Serialization;

namespace SistemaDental.Domain.Enums;

public enum TenantStatus
{
    [EnumMember(Value = "active")]
    Active,
    
    [EnumMember(Value = "suspended")]
    Suspended,
    
    [EnumMember(Value = "inactive")]
    Inactive,
    
    [EnumMember(Value = "trial")]
    Trial
}
