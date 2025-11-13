using SistemaDental.Domain.Enums;

namespace SistemaDental.Domain.Entities;

public class Usuario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Assistant;
    public UserStatus Status { get; set; } = UserStatus.Active;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? UltimoAcceso { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Campos de seguridad
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockedUntil { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetExpires { get; set; }
    public bool EmailVerified { get; set; } = false;
    public string? EmailVerificationToken { get; set; }
    
    // Propiedades calculadas para compatibilidad con código existente
    public string Rol
    {
        get => Role switch
        {
            UserRole.TenantAdmin => "Admin",
            UserRole.Dentist => "Odontologo",
            UserRole.Assistant => "Asistente",
            UserRole.Receptionist => "Asistente",
            UserRole.SuperAdmin => "SuperAdmin",
            _ => "Asistente"
        };
        set => Role = value switch
        {
            "Admin" => UserRole.TenantAdmin,
            "Odontologo" => UserRole.Dentist,
            "Asistente" => UserRole.Assistant,
            "SuperAdmin" => UserRole.SuperAdmin,
            _ => UserRole.Assistant
        };
    }
    
    public bool Activo
    {
        get => Status == UserStatus.Active;
        set => Status = value ? UserStatus.Active : UserStatus.Inactive;
    }
    
    // Relaciones
    public Tenant Tenant { get; set; } = null!;
    public ICollection<Cita> Citas { get; set; } = new List<Cita>();
    public ICollection<Tratamiento> Tratamientos { get; set; } = new List<Tratamiento>();
    
    // Propiedades calculadas
    public bool IsLocked => LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;
}

