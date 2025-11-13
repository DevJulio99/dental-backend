using System.Runtime.Serialization;

namespace SistemaDental.Domain.Enums;

public enum UserRole
{
    [EnumMember(Value = "super_admin")]
    SuperAdmin,
    
    [EnumMember(Value = "tenant_admin")]
    TenantAdmin,
    
    [EnumMember(Value = "dentist")]
    Dentist,
    
    [EnumMember(Value = "assistant")]
    Assistant,
    
    [EnumMember(Value = "receptionist")]
    Receptionist
}
