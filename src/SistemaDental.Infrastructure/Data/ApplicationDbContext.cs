using Microsoft.EntityFrameworkCore;
using Npgsql;
using SistemaDental.Domain.Entities;
using SistemaDental.Domain.Enums;

namespace SistemaDental.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Paciente> Pacientes { get; set; }
    public DbSet<Cita> Citas { get; set; }
    public DbSet<Odontograma> Odontogramas { get; set; }
    public DbSet<Tratamiento> Tratamientos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Registrar enums de PostgreSQL
        modelBuilder.HasPostgresEnum("tenant_status", new[] { "active", "suspended", "inactive", "trial" });
        modelBuilder.HasPostgresEnum("user_status", new[] { "active", "inactive", "suspended" });
        modelBuilder.HasPostgresEnum("user_role", new[] { "super_admin", "tenant_admin", "dentist", "assistant", "receptionist" });
        modelBuilder.HasPostgresEnum("appointment_status", new[] { "scheduled", "confirmed", "in_progress", "completed", "cancelled", "no_show" });
        modelBuilder.HasPostgresEnum("tooth_status", new[] { "healthy", "cavity", "filled", "root_canal", "crown", "bridge", "implant", "missing", "fractured", "to_extract", "in_treatment" });

        // Configuración de Tenant - Mapea a tabla 'tenants'
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("tenants");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.HasIndex(e => e.Subdominio).IsUnique();
            entity.Property(e => e.Nombre).HasColumnName("name").IsRequired().HasMaxLength(200);
            entity.Property(e => e.Subdominio).HasColumnName("slug").IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).HasColumnName("email").IsRequired().HasMaxLength(200);
            entity.Property(e => e.Telefono).HasColumnName("phone").HasMaxLength(50);
            entity.Property(e => e.Direccion).HasColumnName("address");
            // Mapear el enum directamente - Npgsql lo manejará automáticamente
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.FechaCreacion).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            // ConfiguracionHorarios no existe en la BD, se ignora
            entity.Ignore(e => e.ConfiguracionHorarios);
            // Ignorar la propiedad calculada Activo
            entity.Ignore(e => e.Activo);
            entity.Property(e => e.ConfirmacionEmail).HasColumnName("enable_email_notifications");
            entity.Property(e => e.ConfirmacionSMS).HasColumnName("enable_sms_notifications");
        });

        // Configuración de Usuario - Mapea a tabla 'users'
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.HasIndex(e => new { e.TenantId, e.Email }).IsUnique();
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.Nombre).HasColumnName("first_name").IsRequired().HasMaxLength(100);
            entity.Property(e => e.Apellido).HasColumnName("last_name").IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).HasColumnName("email").IsRequired().HasMaxLength(200);
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").IsRequired().HasMaxLength(500);
            // Mapear UserRole con conversión explícita a snake_case para PostgreSQL
            entity.Property(e => e.Role)
                .HasConversion(
                    v => ConvertUserRoleToString(v),
                    v => ConvertStringToUserRole(v))
                .HasColumnName("role")
                .IsRequired();
            // Mapear UserStatus con conversión explícita a snake_case para PostgreSQL
            entity.Property(e => e.Status)
                .HasConversion(
                    v => ConvertUserStatusToString(v),
                    v => ConvertStringToUserStatus(v))
                .HasColumnName("status");
            entity.Property(e => e.FechaCreacion).HasColumnName("created_at");
            entity.Property(e => e.UltimoAcceso).HasColumnName("last_login_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            
            // Campos de perfil profesional
            entity.Property(e => e.ProfessionalLicense).HasColumnName("professional_license").HasMaxLength(100);
            entity.Property(e => e.Specialization).HasColumnName("specialization").HasMaxLength(255);
            entity.Property(e => e.Bio).HasColumnName("bio");
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
            entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(50);
            
            // Ignorar propiedades calculadas
            entity.Ignore(e => e.Rol);
            entity.Ignore(e => e.Activo);
            entity.Ignore(e => e.IsLocked);
            
            // Campos de seguridad
            entity.Property(e => e.FailedLoginAttempts).HasColumnName("failed_login_attempts").HasDefaultValue(0);
            entity.Property(e => e.LockedUntil).HasColumnName("locked_until");
            entity.Property(e => e.PasswordResetToken).HasColumnName("password_reset_token").HasMaxLength(255);
            entity.Property(e => e.PasswordResetExpires).HasColumnName("password_reset_expires");
            entity.Property(e => e.EmailVerified).HasColumnName("email_verified").HasDefaultValue(false);
            entity.Property(e => e.EmailVerificationToken).HasColumnName("email_verification_token").HasMaxLength(255);
            
            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.Usuarios)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuración de Paciente - Mapea a tabla 'patients'
        modelBuilder.Entity<Paciente>(entity =>
        {
            entity.ToTable("patients");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.HasIndex(e => new { e.TenantId, e.DniPasaporte });
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.FirstName).HasColumnName("first_name").IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).HasColumnName("last_name").IsRequired().HasMaxLength(100);
            // NombreCompleto es una propiedad calculada, no se mapea a la BD
            entity.Ignore(e => e.NombreCompleto);
            entity.Property(e => e.TipoDocumento).HasColumnName("document_type").IsRequired().HasMaxLength(50).HasDefaultValue("DNI");
            entity.Property(e => e.DniPasaporte).HasColumnName("document_number").IsRequired().HasMaxLength(50);
            entity.Property(e => e.FechaNacimiento).HasColumnName("date_of_birth");
            entity.Property(e => e.Genero).HasColumnName("gender").HasMaxLength(20);
            entity.Property(e => e.Telefono).HasColumnName("phone").IsRequired().HasMaxLength(50);
            entity.Property(e => e.TelefonoAlternativo).HasColumnName("alternate_phone").HasMaxLength(50);
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255);
            entity.Property(e => e.Direccion).HasColumnName("address");
            entity.Property(e => e.Ciudad).HasColumnName("city").HasMaxLength(100);
            entity.Property(e => e.TipoSangre).HasColumnName("blood_type").HasMaxLength(10);
            entity.Property(e => e.Alergias).HasColumnName("allergies");
            entity.Property(e => e.CondicionesMedicas).HasColumnName("medical_conditions");
            entity.Property(e => e.MedicamentosActuales).HasColumnName("current_medications");
            entity.Property(e => e.ContactoEmergenciaNombre).HasColumnName("emergency_contact_name").HasMaxLength(255);
            entity.Property(e => e.ContactoEmergenciaTelefono).HasColumnName("emergency_contact_phone").HasMaxLength(50);
            entity.Property(e => e.SeguroDental).HasColumnName("dental_insurance").HasMaxLength(255);
            entity.Property(e => e.NumeroSeguro).HasColumnName("insurance_number").HasMaxLength(100);
            entity.Property(e => e.FotoUrl).HasColumnName("photo_url");
            entity.Property(e => e.Observaciones).HasColumnName("notes");
            // La BD tiene is_active (boolean)
            entity.Property(e => e.Activo).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.CreadoPor).HasColumnName("created_by");
            entity.Property(e => e.FechaCreacion).HasColumnName("created_at");
            // FechaUltimaCita no existe en la BD, se calcula desde appointments
            entity.Ignore(e => e.FechaUltimaCita);
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            
            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.Pacientes)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuración de Cita - Mapea a tabla 'appointments'
        modelBuilder.Entity<Cita>(entity =>
        {
            entity.ToTable("appointments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.HasIndex(e => new { e.TenantId, e.AppointmentDate });
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.PacienteId).HasColumnName("patient_id");
            entity.Property(e => e.UsuarioId).HasColumnName("dentist_id").IsRequired();
            entity.Property(e => e.AppointmentDate).HasColumnName("appointment_date");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            // FechaHora es una propiedad calculada, no se mapea directamente
            entity.Ignore(e => e.FechaHora);
            entity.Property(e => e.DuracionMinutos).HasColumnName("duration_minutes").HasDefaultValue(30);
            // Mapear el enum AppointmentStatus directamente al enum de PostgreSQL
            // Usar conversión que convierte a string, y Npgsql manejará el cast al enum
            // Nota: Esto requiere que el interceptor convierta el string a 'valor'::enum_type en el SQL
            entity.Property(e => e.Estado)
                .HasConversion(
                    v => ConvertAppointmentStatusToString(v),
                    v => ConvertStringToAppointmentStatus(v))
                .HasColumnName("status");
            entity.Property(e => e.Motivo).HasColumnName("reason").IsRequired();
            entity.Property(e => e.Observaciones).HasColumnName("notes");
            entity.Property(e => e.NotificationSent).HasColumnName("notification_sent").HasDefaultValue(false);
            entity.Property(e => e.ReminderSent).HasColumnName("reminder_sent").HasDefaultValue(false);
            entity.Property(e => e.CancellationReason).HasColumnName("cancellation_reason");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            // Convertir todos los DateTime a UTC para PostgreSQL
            entity.Property(e => e.FechaCreacion)
                .HasColumnName("created_at")
                .HasConversion(
                    v => ConvertToUtc(v),
                    v => ConvertToUtc(v));
            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasConversion(
                    v => ConvertToUtcNullable(v),
                    v => ConvertToUtcNullable(v));
            entity.Property(e => e.CancelledAt)
                .HasColumnName("cancelled_at")
                .HasConversion(
                    v => ConvertToUtcNullable(v),
                    v => ConvertToUtcNullable(v));
            entity.Property(e => e.CancelledBy).HasColumnName("cancelled_by");
            entity.Property(e => e.DeletedAt)
                .HasColumnName("deleted_at")
                .HasConversion(
                    v => ConvertToUtcNullable(v),
                    v => ConvertToUtcNullable(v));
            
            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.Citas)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Paciente)
                .WithMany(p => p.Citas)
                .HasForeignKey(e => e.PacienteId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Usuario)
                .WithMany(u => u.Citas)
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuración de Odontograma - Mapea a tabla 'odontogram_records'
        modelBuilder.Entity<Odontograma>(entity =>
        {
            entity.ToTable("odontogram_records");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.HasIndex(e => new { e.TenantId, e.PacienteId, e.NumeroDiente, e.FechaRegistro });
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.PacienteId).HasColumnName("patient_id");
            entity.Property(e => e.NumeroDiente).HasColumnName("tooth_number");
            // Usamos una conversión a string, y el interceptor se encargará del cast a enum
            entity.Property(e => e.Estado)
                .HasConversion(
                    v => v.ToLower(),
                    v => v)
                .HasColumnName("status")
                .IsRequired();
            entity.Property(e => e.Observaciones).HasColumnName("notes");
            entity.Property(e => e.FechaRegistro).HasColumnName("record_date");
            // FechaRegistroDateTime es una propiedad calculada
            entity.Ignore(e => e.FechaRegistroDateTime);
            entity.Property(e => e.UsuarioId).HasColumnName("recorded_by").IsRequired();
            entity.Property(e => e.ClinicalRecordId).HasColumnName("clinical_record_id");
            
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Paciente)
                .WithMany(p => p.Odontogramas)
                .HasForeignKey(e => e.PacienteId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Usuario)
                .WithMany()
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuración de Tratamiento - Mapea a tabla 'clinical_records' (tratamientos realizados)
        // Nota: La tabla 'treatments' es un catálogo de tratamientos disponibles
        modelBuilder.Entity<Tratamiento>(entity =>
        {
            entity.ToTable("clinical_records");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.PacienteId).HasColumnName("patient_id");
            entity.Property(e => e.CitaId).HasColumnName("appointment_id");
            entity.Property(e => e.UsuarioId).HasColumnName("dentist_id").IsRequired();
            entity.Property(e => e.TreatmentId).HasColumnName("treatment_id");
            entity.Property(e => e.TreatmentDate).HasColumnName("treatment_date");
            // FechaRealizacion es una propiedad calculada
            entity.Ignore(e => e.FechaRealizacion);
            entity.Property(e => e.Diagnosis).HasColumnName("diagnosis");
            entity.Property(e => e.TreatmentPerformed).HasColumnName("treatment_performed").IsRequired();
            entity.Property(e => e.Observaciones).HasColumnName("observations");
            entity.Property(e => e.Costo).HasColumnName("cost");
            entity.Property(e => e.FechaCreacion).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Paciente)
                .WithMany(p => p.Tratamientos)
                .HasForeignKey(e => e.PacienteId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Cita)
                .WithMany(c => c.Tratamientos)
                .HasForeignKey(e => e.CitaId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasOne(e => e.Usuario)
                .WithMany(u => u.Tratamientos)
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
            });
    }

    // Métodos auxiliares para convertir UserRole a/desde string
    private static string ConvertUserRoleToString(UserRole role)
    {
        return role switch
        {
            UserRole.SuperAdmin => "super_admin",
            UserRole.TenantAdmin => "tenant_admin",
            UserRole.Dentist => "dentist",
            UserRole.Assistant => "assistant",
            UserRole.Receptionist => "receptionist",
            _ => "assistant"
        };
    }

    private static UserRole ConvertStringToUserRole(string value)
    {
        return value switch
        {
            "super_admin" => UserRole.SuperAdmin,
            "tenant_admin" => UserRole.TenantAdmin,
            "dentist" => UserRole.Dentist,
            "assistant" => UserRole.Assistant,
            "receptionist" => UserRole.Receptionist,
            _ => UserRole.Assistant
        };
    }

    // Métodos auxiliares para convertir UserStatus a/desde string
    private static string ConvertUserStatusToString(UserStatus status)
    {
        return status switch
        {
            UserStatus.Active => "active",
            UserStatus.Inactive => "inactive",
            UserStatus.Suspended => "suspended",
            _ => "active"
        };
    }

    private static UserStatus ConvertStringToUserStatus(string value)
    {
        return value switch
        {
            "active" => UserStatus.Active,
            "inactive" => UserStatus.Inactive,
            "suspended" => UserStatus.Suspended,
            _ => UserStatus.Active
        };
    }

    // Métodos auxiliares para convertir AppointmentStatus a/desde string
    // Estos métodos son necesarios porque EF Core no puede usar expresiones switch en árboles de expresión
    private static string ConvertAppointmentStatusToString(AppointmentStatus status)
    {
        return status switch
        {
            AppointmentStatus.Scheduled => "scheduled",
            AppointmentStatus.Confirmed => "confirmed",
            AppointmentStatus.InProgress => "in_progress",
            AppointmentStatus.Completed => "completed",
            AppointmentStatus.Cancelled => "cancelled",
            AppointmentStatus.NoShow => "no_show",
            _ => "scheduled"
        };
    }

    private static AppointmentStatus ConvertStringToAppointmentStatus(string value)
    {
        return value switch
        {
            "scheduled" => AppointmentStatus.Scheduled,
            "confirmed" => AppointmentStatus.Confirmed,
            "in_progress" => AppointmentStatus.InProgress,
            "completed" => AppointmentStatus.Completed,
            "cancelled" => AppointmentStatus.Cancelled,
            "no_show" => AppointmentStatus.NoShow,
            _ => AppointmentStatus.Scheduled
        };
    }

    // Método auxiliar para convertir DateTime a UTC
    // PostgreSQL requiere que los DateTime sean UTC cuando se escriben a timestamp with time zone
    private static DateTime ConvertToUtc(DateTime dateTime)
    {
        if (dateTime.Kind == DateTimeKind.Utc)
            return dateTime;
        
        if (dateTime.Kind == DateTimeKind.Unspecified)
            return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        
        return dateTime.ToUniversalTime();
    }

    // Método auxiliar para convertir DateTime? a UTC
    private static DateTime? ConvertToUtcNullable(DateTime? dateTime)
    {
        if (!dateTime.HasValue)
            return null;
        
        return ConvertToUtc(dateTime.Value);
    }
}

