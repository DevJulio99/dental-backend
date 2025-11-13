using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal;
using SistemaDental.Domain.Entities;

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
            // Mapear Activo: true = status = 'active', false = status = 'trial' (para nuevos tenants)
            // El tipo de columna es tenant_status (enum de PostgreSQL), necesitamos hacer cast explícito
            entity.Property(e => e.Activo)
                .HasConversion(
                    v => v ? "active" : "trial",
                    v => v == "active" || v == "trial")
                .HasColumnName("status")
                .HasColumnType("tenant_status");
            entity.Property(e => e.FechaCreacion).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            // ConfiguracionHorarios no existe en la BD, se ignora
            entity.Ignore(e => e.ConfiguracionHorarios);
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
            // Mapear Rol: convertir entre valores de enum/string
            // El tipo de columna es user_role (enum de PostgreSQL), necesitamos hacer cast explícito
            entity.Property(e => e.Rol)
                .HasConversion(
                    v => v == "Admin" ? "tenant_admin" : v == "Odontologo" ? "dentist" : v == "Asistente" ? "assistant" : v.ToLower(),
                    v => v == "tenant_admin" ? "Admin" : v == "dentist" ? "Odontologo" : v == "assistant" ? "Asistente" : v == "receptionist" ? "Asistente" : v)
                .HasColumnName("role")
                .HasColumnType("user_role")
                .IsRequired();
            // Mapear Activo: true = status = 'active', false = status = 'inactive'
            // El tipo de columna es user_status (enum de PostgreSQL), necesitamos hacer cast explícito
            entity.Property(e => e.Activo)
                .HasConversion(
                    v => v ? "active" : "inactive",
                    v => v == "active")
                .HasColumnName("status")
                .HasColumnType("user_status");
            entity.Property(e => e.FechaCreacion).HasColumnName("created_at");
            entity.Property(e => e.UltimoAcceso).HasColumnName("last_login_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            
            // Campos de seguridad
            entity.Property(e => e.FailedLoginAttempts).HasColumnName("failed_login_attempts").HasDefaultValue(0);
            entity.Property(e => e.LockedUntil).HasColumnName("locked_until");
            entity.Property(e => e.PasswordResetToken).HasColumnName("password_reset_token").HasMaxLength(255);
            entity.Property(e => e.PasswordResetExpires).HasColumnName("password_reset_expires");
            entity.Property(e => e.EmailVerified).HasColumnName("email_verified").HasDefaultValue(false);
            entity.Property(e => e.EmailVerificationToken).HasColumnName("email_verification_token").HasMaxLength(255);
            
            // Ignorar propiedad calculada
            entity.Ignore(e => e.IsLocked);
            
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
            entity.Property(e => e.DniPasaporte).HasColumnName("document_number").IsRequired().HasMaxLength(50);
            entity.Property(e => e.FechaNacimiento).HasColumnName("date_of_birth");
            entity.Property(e => e.Telefono).HasColumnName("phone").IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255);
            entity.Property(e => e.Direccion).HasColumnName("address");
            entity.Property(e => e.Alergias).HasColumnName("allergies");
            entity.Property(e => e.Observaciones).HasColumnName("notes");
            // La BD tiene is_active (boolean)
            entity.Property(e => e.Activo).HasColumnName("is_active").HasDefaultValue(true);
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
            // El tipo de columna es appointment_status (enum de PostgreSQL), necesitamos hacer cast explícito
            entity.Property(e => e.Estado)
                .HasConversion(
                    v => v.ToLower(),
                    v => v)
                .HasColumnName("status")
                .HasColumnType("appointment_status");
            entity.Property(e => e.Motivo).HasColumnName("reason").IsRequired();
            entity.Property(e => e.Observaciones).HasColumnName("notes");
            entity.Property(e => e.NotificationSent).HasColumnName("notification_sent").HasDefaultValue(false);
            entity.Property(e => e.ReminderSent).HasColumnName("reminder_sent").HasDefaultValue(false);
            entity.Property(e => e.CancellationReason).HasColumnName("cancellation_reason");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.FechaCreacion).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.CancelledAt).HasColumnName("cancelled_at");
            entity.Property(e => e.CancelledBy).HasColumnName("cancelled_by");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            
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
            // El tipo de columna es tooth_status (enum de PostgreSQL), necesitamos hacer cast explícito
            entity.Property(e => e.Estado)
                .HasConversion(
                    v => v.ToLower(),
                    v => v)
                .HasColumnName("status")
                .HasColumnType("tooth_status")
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
}

