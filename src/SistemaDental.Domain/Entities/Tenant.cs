using SistemaDental.Domain.Enums;

namespace SistemaDental.Domain.Entities;

public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nombre { get; set; } = string.Empty;
    public string Subdominio { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public TenantStatus Status { get; set; } = TenantStatus.Trial;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Configuración del consultorio
    public string? ConfiguracionHorarios { get; set; } // JSON con horarios semanales
    public bool ConfirmacionEmail { get; set; } = true;
    public bool ConfirmacionSMS { get; set; } = false;
    
    // Propiedad calculada para compatibilidad con código existente
    public bool Activo
    {
        get => Status == TenantStatus.Active || Status == TenantStatus.Trial;
        set => Status = value ? TenantStatus.Active : TenantStatus.Inactive;
    }
    
    // Relaciones
    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    public ICollection<Paciente> Pacientes { get; set; } = new List<Paciente>();
    public ICollection<Cita> Citas { get; set; } = new List<Cita>();
}

