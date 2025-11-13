namespace SistemaDental.Application.DTOs.Auth;

public class UsuarioUpdateDto
{
    public string? Nombre { get; set; }
    public string? Apellido { get; set; }
    public string? Email { get; set; }
    public string? Rol { get; set; }
    public bool? Activo { get; set; }
    
    // Campos opcionales de perfil profesional
    public string? ProfessionalLicense { get; set; }
    public string? Specialization { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Phone { get; set; }
}






