namespace SistemaDental.Domain.Enums;

public enum RolUsuario
{
    Admin = 1,
    Odontologo = 2,
    Asistente = 3
}

public static class RolUsuarioExtensions
{
    public static string ToString(this RolUsuario rol)
    {
        return rol switch
        {
            RolUsuario.Admin => "Admin",
            RolUsuario.Odontologo => "Odontologo",
            RolUsuario.Asistente => "Asistente",
            _ => "Desconocido"
        };
    }
}

