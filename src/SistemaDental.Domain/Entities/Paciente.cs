namespace SistemaDental.Domain.Entities;

public class Paciente
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    // Propiedad calculada para compatibilidad
    public string NombreCompleto => $"{FirstName} {LastName}".Trim();
    public string DniPasaporte { get; set; } = string.Empty;
    public DateTime FechaNacimiento { get; set; }
    public string Telefono { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public string? Alergias { get; set; }
    public string? Observaciones { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaUltimaCita { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    
    // Relaciones
    public Tenant Tenant { get; set; } = null!;
    public ICollection<Cita> Citas { get; set; } = new List<Cita>();
    public ICollection<Odontograma> Odontogramas { get; set; } = new List<Odontograma>();
    public ICollection<Tratamiento> Tratamientos { get; set; } = new List<Tratamiento>();
}

