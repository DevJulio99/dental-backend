using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using SistemaDental.Infrastructure.Services;

namespace SistemaDental.API.Middleware;

public class JwtTenantMiddleware
{
    private readonly RequestDelegate _next;

    public JwtTenantMiddleware(RequestDelegate next)
    {
        _next = next;
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
            }
        }

        await _next(context);
    }
}

