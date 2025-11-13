using System.Runtime.Serialization;

namespace SistemaDental.Domain.Enums;

public enum ToothStatus
{
    [EnumMember(Value = "healthy")]
    Healthy,
    
    [EnumMember(Value = "cavity")]
    Cavity,
    
    [EnumMember(Value = "filled")]
    Filled,
    
    [EnumMember(Value = "root_canal")]
    RootCanal,
    
    [EnumMember(Value = "crown")]
    Crown,
    
    [EnumMember(Value = "bridge")]
    Bridge,
    
    [EnumMember(Value = "implant")]
    Implant,
    
    [EnumMember(Value = "missing")]
    Missing,
    
    [EnumMember(Value = "fractured")]
    Fractured,
    
    [EnumMember(Value = "to_extract")]
    ToExtract,
    
    [EnumMember(Value = "in_treatment")]
    InTreatment
}
