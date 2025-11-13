using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SistemaDental.Application.DTOs.Auth;
using SistemaDental.Application.DTOs.Tenant;
using SistemaDental.Domain.Entities;
using SistemaDental.Infrastructure.Repositories;
using SistemaDental.Infrastructure.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SistemaDental.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUnitOfWork unitOfWork,
        IPasswordService passwordService,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        try
        {
            Tenant? tenant = null;

            // Si se proporciona subdomain, buscar el tenant
            if (!string.IsNullOrEmpty(request.Subdomain))
            {
                tenant = await _unitOfWork.Tenants.GetBySubdomainAsync(request.Subdomain);
                if (tenant == null || !tenant.Activo)
                {
                    return null;
                }
            }

            // Buscar usuario por email
            var usuario = await _unitOfWork.Usuarios.GetByEmailAsync(request.Email, tenant?.Id);

            if (usuario == null)
            {
                return null;
            }

            // Si se proporcionó tenant, verificar que el usuario pertenezca a ese tenant
            if (tenant != null && usuario.TenantId != tenant.Id)
            {
                return null;
            }

            // Verificar si la cuenta está bloqueada
            if (usuario.IsLocked)
            {
                _logger.LogWarning($"Intento de login en cuenta bloqueada: {request.Email}");
                return null;
            }

            // Verificar contraseña
            if (!_passwordService.VerifyPassword(request.Password, usuario.PasswordHash))
            {
                // Incrementar intentos fallidos
                usuario.FailedLoginAttempts++;
                
                // Bloquear cuenta después de 5 intentos fallidos (30 minutos)
                if (usuario.FailedLoginAttempts >= 5)
                {
                    usuario.LockedUntil = DateTime.UtcNow.AddMinutes(30);
                    _logger.LogWarning($"Cuenta bloqueada por intentos fallidos: {request.Email}");
                }
                
                await _unitOfWork.Usuarios.UpdateAsync(usuario);
                await _unitOfWork.SaveChangesAsync();
                return null;
            }

            // Login exitoso: resetear intentos fallidos y desbloquear cuenta
            usuario.FailedLoginAttempts = 0;
            usuario.LockedUntil = null;
            usuario.UltimoAcceso = DateTime.UtcNow;
            await _unitOfWork.Usuarios.UpdateAsync(usuario);
            await _unitOfWork.SaveChangesAsync();

            // Generar token JWT
            var token = GenerateJwtToken(usuario);

            return new LoginResponse
            {
                Token = token,
                RefreshToken = Guid.NewGuid().ToString(), // Simplificado, en producción usar refresh tokens reales
                ExpiresAt = DateTime.UtcNow.AddHours(8),
                User = new UserInfo
                {
                    Id = usuario.Id,
                    Nombre = usuario.Nombre,
                    Apellido = usuario.Apellido,
                    Email = usuario.Email,
                    Rol = usuario.Rol
                },
                Tenant = new TenantInfo
                {
                    Id = usuario.Tenant.Id,
                    Nombre = usuario.Tenant.Nombre,
                    Subdominio = usuario.Tenant.Subdominio
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al realizar login");
            return null;
        }
    }

    public async Task<bool> RegisterTenantAsync(TenantCreateDto dto)
    {
        try
        {
            // Normalizar subdominio a minúsculas
            var normalizedSubdomain = dto.Subdominio.ToLower().Trim();
            
            _logger.LogInformation("Iniciando registro de tenant. Subdominio normalizado: {Subdomain}", normalizedSubdomain);
            
            // Verificar que el subdominio no exista (case-insensitive)
            var exists = await _unitOfWork.Tenants.SubdomainExistsAsync(normalizedSubdomain);
            _logger.LogInformation("Verificación de subdominio existente: {Exists} para {Subdomain}", exists, normalizedSubdomain);
            
            if (exists)
            {
                _logger.LogWarning("Intento de registro con subdominio existente: {Subdomain}", normalizedSubdomain);
                return false;
            }

            // Iniciar transacción para asegurar atomicidad
            await _unitOfWork.BeginTransactionAsync();
            
            try
            {
                // Crear tenant
                var tenant = new Tenant
                {
                    Nombre = dto.Nombre,
                    Subdominio = normalizedSubdomain,
                    Email = dto.Email,
                    Telefono = dto.Telefono,
                    Direccion = dto.Direccion,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow,
                    ConfirmacionEmail = true,
                    ConfirmacionSMS = false
                };

                _logger.LogInformation("Creando tenant en base de datos...");
                await _unitOfWork.Tenants.AddAsync(tenant);
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Tenant creado exitosamente con ID: {TenantId}", tenant.Id);

                // Crear usuario administrador
                var admin = new Usuario
                {
                    TenantId = tenant.Id,
                    Nombre = dto.AdminNombre,
                    Apellido = dto.AdminApellido,
                    Email = dto.AdminEmail,
                    PasswordHash = _passwordService.HashPassword(dto.AdminPassword),
                    Rol = "Admin",
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow,
                    EmailVerified = false, // Requiere verificación
                    EmailVerificationToken = Guid.NewGuid().ToString() // Generar token inicial
                };

                _logger.LogInformation("Creando usuario administrador en base de datos. Email: {Email}, TenantId: {TenantId}", 
                    dto.AdminEmail, tenant.Id);
                await _unitOfWork.Usuarios.AddAsync(admin);
                _logger.LogInformation("Usuario agregado al contexto, guardando cambios...");
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Usuario admin creado exitosamente con ID: {UserId}", admin.Id);

                // Confirmar transacción
                await _unitOfWork.CommitTransactionAsync();
                
                // TODO: Enviar email de bienvenida con token de verificación
                _logger.LogInformation("Usuario admin creado: {Email} - Token de verificación: {Token}", 
                    dto.AdminEmail, admin.EmailVerificationToken);

                return true;
            }
            catch
            {
                // Si algo falla, revertir la transacción
                await _unitOfWork.RollbackTransactionAsync();
                throw; // Re-lanzar la excepción para que sea capturada por los catch externos
            }
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Error de base de datos al registrar tenant. Subdominio: {Subdomain}, Email: {Email}, AdminEmail: {AdminEmail}. InnerException: {InnerException}, StackTrace: {StackTrace}", 
                dto.Subdominio, dto.Email, dto.AdminEmail, dbEx.InnerException?.Message, dbEx.StackTrace);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar tenant. Subdominio: {Subdomain}, Email: {Email}, AdminEmail: {AdminEmail}. StackTrace: {StackTrace}", 
                dto.Subdominio, dto.Email, dto.AdminEmail, ex.StackTrace);
            return false;
        }
    }

    public async Task<string?> ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        try
        {
            Tenant? tenant = null;

            // Si se proporciona subdomain, buscar el tenant
            if (!string.IsNullOrEmpty(dto.Subdomain))
            {
                tenant = await _unitOfWork.Tenants.GetBySubdomainAsync(dto.Subdomain);
                if (tenant == null || !tenant.Activo)
                {
                    return null;
                }
            }

            // Buscar usuario por email
            var usuario = await _unitOfWork.Usuarios.GetByEmailAsync(dto.Email, tenant?.Id);

            if (usuario == null)
            {
                // Por seguridad, no revelar si el email existe o no
                // Retornamos null para indicar que no se generó token
                return null;
            }

            // Si se proporcionó tenant, verificar que el usuario pertenezca a ese tenant
            if (tenant != null && usuario.TenantId != tenant.Id)
            {
                return null;
            }

            // Generar token de reset (válido por 1 hora)
            var token = Guid.NewGuid().ToString();
            usuario.PasswordResetToken = token;
            usuario.PasswordResetExpires = DateTime.UtcNow.AddHours(1);
            usuario.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Usuarios.UpdateAsync(usuario);
            await _unitOfWork.SaveChangesAsync();

            // TODO: Enviar email con el token (implementar servicio de email)
            _logger.LogInformation($"Token de reset generado para: {dto.Email} - Token: {token}");

            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar solicitud de reset de contraseña");
            return null;
        }
    }

    public async Task<bool> GenerateEmailVerificationTokenAsync(string email)
    {
        try
        {
            var usuario = await _unitOfWork.Usuarios.GetByEmailAsync(email);

            if (usuario == null || usuario.EmailVerified)
            {
                return false;
            }

            // Generar token de verificación (válido por 24 horas)
            var token = Guid.NewGuid().ToString();
            usuario.EmailVerificationToken = token;
            usuario.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Usuarios.UpdateAsync(usuario);
            await _unitOfWork.SaveChangesAsync();

            // TODO: Enviar email con el token (implementar servicio de email)
            _logger.LogInformation($"Token de verificación generado para: {email} - Token: {token}");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar token de verificación");
            return false;
        }
    }

    public async Task<bool> VerifyEmailAsync(VerifyEmailDto dto)
    {
        try
        {
            var usuario = await _unitOfWork.Usuarios.GetByEmailAndTokenAsync(dto.Email, dto.Token);

            if (usuario == null)
            {
                return false;
            }

            usuario.EmailVerified = true;
            usuario.EmailVerificationToken = null;
            usuario.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Usuarios.UpdateAsync(usuario);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar email");
            return false;
        }
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordDto dto)
    {
        try
        {
            Tenant? tenant = null;

            // Si se proporciona subdomain, buscar el tenant
            if (!string.IsNullOrEmpty(dto.Subdomain))
            {
                tenant = await _unitOfWork.Tenants.GetBySubdomainAsync(dto.Subdomain);
                if (tenant == null || !tenant.Activo)
                {
                    _logger.LogWarning($"Intento de reset de contraseña con subdomain inválido: {dto.Subdomain}");
                    return false;
                }
            }

            // Buscar usuario por email y token válido
            var usuario = await _unitOfWork.Usuarios.GetByEmailAndResetTokenAsync(dto.Email, dto.Token, tenant?.Id);

            if (usuario == null)
            {
                _logger.LogWarning($"Intento de reset de contraseña fallido para email: {dto.Email}. Token inválido o expirado.");
                return false;
            }

            // Actualizar contraseña
            usuario.PasswordHash = _passwordService.HashPassword(dto.NewPassword);
            usuario.PasswordResetToken = null;
            usuario.PasswordResetExpires = null;
            usuario.FailedLoginAttempts = 0;
            usuario.LockedUntil = null;
            usuario.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Usuarios.UpdateAsync(usuario);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"Contraseña reseteada exitosamente para usuario: {dto.Email}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al resetear contraseña");
            return false;
        }
    }

    private string GenerateJwtToken(Usuario usuario)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key no configurada")));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Name, $"{usuario.Nombre} {usuario.Apellido}"),
            new Claim(ClaimTypes.Role, usuario.Rol),
            new Claim("TenantId", usuario.TenantId.ToString())
        };

        // Obtener horas de expiración desde configuración (default: 8 horas)
        var expirationHours = 8;
        if (int.TryParse(_configuration["Jwt:ExpirationHours"], out var configHours) && configHours > 0)
        {
            expirationHours = configHours;
        }

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expirationHours),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

