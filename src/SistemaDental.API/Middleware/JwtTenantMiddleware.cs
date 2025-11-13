using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SistemaDental.Infrastructure.Services;

namespace SistemaDental.API.Middleware;

public class JwtTenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtTenantMiddleware> _logger;

    public JwtTenantMiddleware(RequestDelegate next, ILogger<JwtTenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Resolver el servicio scoped dentro del método InvokeAsync
        var tenantService = context.RequestServices.GetRequiredService<ITenantService>();

        // Si el usuario está autenticado, obtener el TenantId del token JWT
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var tenantIdClaim = context.User.FindFirst("TenantId")?.Value;

            if (!string.IsNullOrEmpty(tenantIdClaim) && Guid.TryParse(tenantIdClaim, out var tenantId))
            {
                tenantService.SetCurrentTenant(tenantId);
                _logger.LogDebug("TenantId establecido desde JWT: {TenantId}", tenantId);
            }
            else
            {
                _logger.LogWarning("No se pudo obtener TenantId del token JWT. Claims disponibles: {Claims}", 
                    string.Join(", ", context.User.Claims.Select(c => $"{c.Type}={c.Value}")));
            }
        }
        else
        {
            _logger.LogDebug("Usuario no autenticado, no se puede obtener TenantId");
        }

        await _next(context);
    }
}

