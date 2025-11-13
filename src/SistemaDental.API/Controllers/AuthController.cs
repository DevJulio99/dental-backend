using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using SistemaDental.Application.DTOs.Auth;
using SistemaDental.Application.DTOs.Tenant;
using SistemaDental.Application.Services;
using SistemaDental.Infrastructure.Repositories;
using SistemaDental.Infrastructure.Services;

namespace SistemaDental.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantService _tenantService;
    private readonly ILogger<AuthController> _logger;
    private readonly IWebHostEnvironment _environment;

    public AuthController(
        IAuthService authService,
        ITenantRepository tenantRepository,
        ITenantService tenantService,
        ILogger<AuthController> logger,
        IWebHostEnvironment environment)
    {
        _authService = authService;
        _tenantRepository = tenantRepository;
        _tenantService = tenantService;
        _logger = logger;
        _environment = environment;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new { message = "Email y contraseña son requeridos" });
        }

        // Si se proporciona subdomain, establecer el tenant
        if (!string.IsNullOrEmpty(request.Subdomain))
        {
            var tenant = await _tenantRepository.GetBySubdomainAsync(request.Subdomain);
            if (tenant == null)
            {
                return Unauthorized(new { message = "Subdominio no válido" });
            }
            _tenantService.SetCurrentTenant(tenant.Id);
        }

        var response = await _authService.LoginAsync(request);

        if (response == null)
        {
            return Unauthorized(new { message = "Credenciales inválidas" });
        }

        // Establecer el tenant en el servicio para futuras operaciones
        _tenantService.SetCurrentTenant(response.Tenant.Id);

        return Ok(response);
    }

    [HttpPost("register")]
    public async Task<ActionResult> Register([FromBody] TenantCreateDto dto)
    {
        // Verificar validación de FluentValidation
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e => new { Field = x.Key, Message = e.ErrorMessage }))
                .ToList();
            
            _logger.LogWarning("Validación fallida en registro de tenant: {Errors}", 
                string.Join(", ", errors.Select(e => $"{e.Field}: {e.Message}")));
            
            return BadRequest(new { 
                message = "Error de validación en los datos proporcionados",
                errors = errors
            });
        }

        if (string.IsNullOrEmpty(dto.Subdominio) || string.IsNullOrEmpty(dto.Email))
        {
            return BadRequest(new { message = "Subdominio y email son requeridos" });
        }

        _logger.LogInformation("Intento de registro de tenant: Subdominio={Subdomain}, Email={Email}", 
            dto.Subdominio, dto.Email);

        try
        {
            var result = await _authService.RegisterTenantAsync(dto);

            if (!result)
            {
                _logger.LogWarning("Registro de tenant falló: Subdominio={Subdomain}", dto.Subdominio);
                return BadRequest(new { message = "No se pudo registrar el consultorio. El subdominio puede estar en uso o hay un error en los datos proporcionados." });
            }

            _logger.LogInformation("Tenant registrado exitosamente: Subdominio={Subdomain}", dto.Subdominio);
            return Ok(new { message = "Consultorio registrado exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar registro de tenant: Subdominio={Subdomain}", dto.Subdominio);
            return StatusCode(500, new { message = "Error interno al registrar el consultorio. Por favor, revisa los logs del servidor." });
        }
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        if (string.IsNullOrEmpty(dto.Email))
        {
            return BadRequest(new { message = "Email es requerido" });
        }

        // Si se proporciona subdomain, establecer el tenant
        if (!string.IsNullOrEmpty(dto.Subdomain))
        {
            var tenant = await _tenantRepository.GetBySubdomainAsync(dto.Subdomain);
            if (tenant == null)
            {
                return BadRequest(new { message = "Subdominio no válido" });
            }
            _tenantService.SetCurrentTenant(tenant.Id);
        }

        var token = await _authService.ForgotPasswordAsync(dto);

        // Por seguridad, siempre retornamos éxito aunque el email no exista
        if (token != null)
        {
            // En desarrollo, devolvemos el token en la respuesta para facilitar las pruebas
            if (_environment.IsDevelopment())
            {
                return Ok(new 
                { 
                    message = "Token de reset generado exitosamente. En producción, este token se enviaría por email.",
                    token = token,
                    expiresIn = "1 hora"
                });
            }
            
            // En producción, no devolvemos el token (se enviaría por email)
            return Ok(new { message = "Si el email existe, se ha enviado un token de reset por email." });
        }

        // Por seguridad, retornamos el mismo mensaje aunque el email no exista
        return Ok(new { message = "Si el email existe, se ha enviado un token de reset." });
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        if (string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Token) || string.IsNullOrEmpty(dto.NewPassword))
        {
            return BadRequest(new { message = "Email, token y nueva contraseña son requeridos" });
        }

        // Si se proporciona subdomain, establecer el tenant
        if (!string.IsNullOrEmpty(dto.Subdomain))
        {
            var tenant = await _tenantRepository.GetBySubdomainAsync(dto.Subdomain);
            if (tenant == null)
            {
                return BadRequest(new { message = "Subdominio no válido" });
            }
            _tenantService.SetCurrentTenant(tenant.Id);
        }

        var result = await _authService.ResetPasswordAsync(dto);

        if (!result)
        {
            return BadRequest(new { message = "Token inválido o expirado" });
        }

        return Ok(new { message = "Contraseña reseteada exitosamente" });
    }

    [HttpPost("verify-email")]
    public async Task<ActionResult> VerifyEmail([FromBody] VerifyEmailDto dto)
    {
        if (string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Token))
        {
            return BadRequest(new { message = "Email y token son requeridos" });
        }

        var result = await _authService.VerifyEmailAsync(dto);

        if (!result)
        {
            return BadRequest(new { message = "Token inválido o email ya verificado" });
        }

        return Ok(new { message = "Email verificado exitosamente" });
    }

    [HttpPost("resend-verification")]
    public async Task<ActionResult> ResendVerification([FromBody] ForgotPasswordDto dto)
    {
        if (string.IsNullOrEmpty(dto.Email))
        {
            return BadRequest(new { message = "Email es requerido" });
        }

        // Si se proporciona subdomain, establecer el tenant
        if (!string.IsNullOrEmpty(dto.Subdomain))
        {
            var tenant = await _tenantRepository.GetBySubdomainAsync(dto.Subdomain);
            if (tenant == null)
            {
                return BadRequest(new { message = "Subdominio no válido" });
            }
            _tenantService.SetCurrentTenant(tenant.Id);
        }

        var result = await _authService.GenerateEmailVerificationTokenAsync(dto.Email);

        // Por seguridad, siempre retornamos éxito
        if (result)
        {
            return Ok(new { message = "Si el email existe y no está verificado, se ha enviado un token. Revisa los logs del servidor para obtener el token en desarrollo." });
        }

        return Ok(new { message = "Si el email existe y no está verificado, se ha enviado un token. Revisa los logs del servidor para obtener el token en desarrollo." });
    }
}

