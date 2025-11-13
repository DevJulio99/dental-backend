namespace SistemaDental.Application.DTOs.Auth;

public class UsuarioUpdateDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Rol { get; set; }
    public bool? Activo { get; set; }
}




