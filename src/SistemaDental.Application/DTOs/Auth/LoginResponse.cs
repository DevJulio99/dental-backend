namespace SistemaDental.Application.DTOs.Auth;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserInfo User { get; set; } = null!;
    public TenantInfo Tenant { get; set; } = null!;
}

public class UserInfo
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    
    // Campos opcionales de perfil profesional
    public string? ProfessionalLicense { get; set; }
    public string? Specialization { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Phone { get; set; }
}

public class TenantInfo
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Subdominio { get; set; } = string.Empty;
}

