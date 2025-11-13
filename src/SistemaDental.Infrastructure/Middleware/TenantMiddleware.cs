using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SistemaDental.Infrastructure.Services;

namespace SistemaDental.Infrastructure.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(
        RequestDelegate next,
        ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Resolver el servicio scoped dentro del método InvokeAsync
        var tenantService = context.RequestServices.GetRequiredService<ITenantService>();

        // Intentar obtener el tenant desde el subdominio
        var host = context.Request.Host.Host;
        var subdominio = host.Split('.').FirstOrDefault();

        // Si no hay subdominio, intentar desde el header
        if (string.IsNullOrEmpty(subdominio) || subdominio == "localhost" || subdominio == "127.0.0.1")
        {
            subdominio = context.Request.Headers["X-Tenant-Subdomain"].FirstOrDefault();
        }

        // Si aún no hay subdominio, intentar desde query string (para desarrollo)
        if (string.IsNullOrEmpty(subdominio))
        {
            subdominio = context.Request.Query["tenant"].FirstOrDefault();
        }

        if (!string.IsNullOrEmpty(subdominio))
        {
            // Aquí deberías buscar el tenant en la base de datos
            // Por ahora, lo dejamos para que el controlador de autenticación lo maneje
            // El tenant se establecerá después de la autenticación
        }

        await _next(context);
    }
}

