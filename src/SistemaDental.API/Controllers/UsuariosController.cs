using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaDental.Application.DTOs.Auth;
using SistemaDental.Domain.Entities;
using SistemaDental.Infrastructure.Data;
using SistemaDental.Infrastructure.Services;

namespace SistemaDental.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsuariosController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<UsuariosController> _logger;

    public UsuariosController(
        ApplicationDbContext context,
        ITenantService tenantService,
        IPasswordService passwordService,
        ILogger<UsuariosController> logger)
    {
        _context = context;
        _tenantService = tenantService;
        _passwordService = passwordService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserInfo>>> GetAll()
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            var usuarios = await _context.Usuarios
                .Where(u => u.TenantId == tenantId.Value && u.Activo)
                .Select(u => new UserInfo
                {
                    Id = u.Id,
                    Nombre = u.Nombre,
                    Apellido = u.Apellido,
                    Email = u.Email,
                    Rol = u.Rol
                })
                .ToListAsync();

            return Ok(usuarios);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener usuarios");
            return StatusCode(500, new { message = "Error al obtener usuarios" });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserInfo>> Create([FromBody] UsuarioCreateDto dto)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue)
        {
            return Unauthorized();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // Verificar que el email no exista
            var existe = await _context.Usuarios
                .AnyAsync(u => u.TenantId == tenantId.Value && u.Email == dto.Email);

            if (existe)
            {
                return BadRequest(new { message = "El email ya está en uso" });
            }

            var usuario = new Usuario
            {
                TenantId = tenantId.Value,
                Nombre = dto.Nombre,
                Apellido = dto.Apellido,
                Email = dto.Email,
                PasswordHash = _passwordService.HashPassword(dto.Password),
                Rol = dto.Rol,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            await _context.Usuarios.AddAsync(usuario);
            await _context.SaveChangesAsync();

            var userInfo = new UserInfo
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Email = usuario.Email,
                Rol = usuario.Rol
            };

            return CreatedAtAction(nameof(GetAll), new { id = usuario.Id }, userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear usuario");
            return StatusCode(500, new { message = "Error al crear usuario" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserInfo>> GetById(Guid id)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            var usuario = await _context.Usuarios
                .Where(u => u.Id == id && u.TenantId == tenantId.Value && u.Activo)
                .Select(u => new UserInfo
                {
                    Id = u.Id,
                    Nombre = u.Nombre,
                    Apellido = u.Apellido,
                    Email = u.Email,
                    Rol = u.Rol
                })
                .FirstOrDefaultAsync();

            if (usuario == null)
            {
                return NotFound(new { message = "Usuario no encontrado" });
            }

            return Ok(usuario);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener usuario");
            return StatusCode(500, new { message = "Error al obtener usuario" });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserInfo>> Update(Guid id, [FromBody] UsuarioUpdateDto dto)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue)
        {
            return Unauthorized();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId.Value);

            if (usuario == null)
            {
                return NotFound(new { message = "Usuario no encontrado" });
            }

            // Verificar si se está cambiando el email y si ya existe
            if (!string.IsNullOrEmpty(dto.Email) && dto.Email != usuario.Email)
            {
                var emailExiste = await _context.Usuarios
                    .AnyAsync(u => u.TenantId == tenantId.Value && u.Email == dto.Email && u.Id != id);

                if (emailExiste)
                {
                    return BadRequest(new { message = "El email ya está en uso" });
                }
                usuario.Email = dto.Email;
            }

            // Actualizar campos
            if (!string.IsNullOrEmpty(dto.Nombre))
                usuario.Nombre = dto.Nombre;

            if (!string.IsNullOrEmpty(dto.Apellido))
                usuario.Apellido = dto.Apellido;

            if (!string.IsNullOrEmpty(dto.Rol))
                usuario.Rol = dto.Rol;

            if (dto.Activo.HasValue)
                usuario.Activo = dto.Activo.Value;

            usuario.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var userInfo = new UserInfo
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Email = usuario.Email,
                Rol = usuario.Rol
            };

            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar usuario");
            return StatusCode(500, new { message = "Error al actualizar usuario" });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId.Value);

            if (usuario == null)
            {
                return NotFound(new { message = "Usuario no encontrado" });
            }

            // Soft delete: desactivar en lugar de eliminar
            usuario.Activo = false;
            usuario.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Usuario eliminado exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar usuario");
            return StatusCode(500, new { message = "Error al eliminar usuario" });
        }
    }

    [HttpPost("{id}/change-password")]
    public async Task<ActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordDto dto)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue)
        {
            return Unauthorized();
        }

        // Obtener ID del usuario actual desde el token
        var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserIdClaim) || !Guid.TryParse(currentUserIdClaim, out var currentUserId))
        {
            return Unauthorized();
        }

        // Solo el usuario puede cambiar su propia contraseña, o un Admin puede cambiar cualquier contraseña
        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && currentUserId != id)
        {
            return Forbid("Solo puedes cambiar tu propia contraseña");
        }

        try
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId.Value && u.Activo);

            if (usuario == null)
            {
                return NotFound(new { message = "Usuario no encontrado" });
            }

            // Si no es admin, verificar la contraseña actual
            if (!isAdmin)
            {
                if (!_passwordService.VerifyPassword(dto.CurrentPassword, usuario.PasswordHash))
                {
                    return BadRequest(new { message = "La contraseña actual es incorrecta" });
                }
            }

            // Actualizar contraseña
            usuario.PasswordHash = _passwordService.HashPassword(dto.NewPassword);
            usuario.FailedLoginAttempts = 0;
            usuario.LockedUntil = null;
            usuario.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Contraseña actualizada exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar contraseña");
            return StatusCode(500, new { message = "Error al cambiar contraseña" });
        }
    }

    [HttpPost("{id}/admin-reset-password")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> AdminResetPassword(Guid id, [FromBody] AdminChangePasswordDto dto)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId.Value);

            if (usuario == null)
            {
                return NotFound(new { message = "Usuario no encontrado" });
            }

            // Actualizar contraseña
            usuario.PasswordHash = _passwordService.HashPassword(dto.NewPassword);
            usuario.FailedLoginAttempts = 0;
            usuario.LockedUntil = null;
            usuario.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Contraseña reseteada exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al resetear contraseña");
            return StatusCode(500, new { message = "Error al resetear contraseña" });
        }
    }

    [HttpPost("{id}/unlock")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> UnlockAccount(Guid id)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId.Value);

            if (usuario == null)
            {
                return NotFound(new { message = "Usuario no encontrado" });
            }

            usuario.FailedLoginAttempts = 0;
            usuario.LockedUntil = null;
            usuario.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Cuenta desbloqueada exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desbloquear cuenta");
            return StatusCode(500, new { message = "Error al desbloquear cuenta" });
        }
    }
}

public class UsuarioCreateDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
}

