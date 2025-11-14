using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using SistemaDental.Application.Services;
using SistemaDental.Application.Validators;
using SistemaDental.Infrastructure.Data;
using SistemaDental.Infrastructure.Middleware;
using SistemaDental.Infrastructure.Repositories;
using SistemaDental.Infrastructure.Services;
using SistemaDental.API.Middleware;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configuración de servicios
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Sistema Dental API",
        Version = "v1",
        Description = "API para sistema de agendamiento odontológico multi-tenant"
    });

    // Configurar JWT en Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando el esquema Bearer. Ejemplo: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configuración de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configuración de Entity Framework Core con PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Configurar Npgsql Data Source con mapeo de enums
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
// MapEnum sin traductor - especificando explícitamente cada mapeo
dataSourceBuilder.MapEnum<SistemaDental.Domain.Enums.TenantStatus>("tenant_status");
dataSourceBuilder.MapEnum<SistemaDental.Domain.Enums.UserStatus>("user_status");
dataSourceBuilder.MapEnum<SistemaDental.Domain.Enums.UserRole>("user_role");
dataSourceBuilder.MapEnum<SistemaDental.Domain.Enums.AppointmentStatus>("appointment_status");
dataSourceBuilder.MapEnum<SistemaDental.Domain.Enums.ToothStatus>("tooth_status");
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    var logger = serviceProvider.GetService<ILogger<SistemaDental.Infrastructure.Data.PostgresEnumInterceptor>>();
    options.UseNpgsql(dataSource)
           .AddInterceptors(new SistemaDental.Infrastructure.Data.PostgresEnumInterceptor(logger));
});

// Configuración de JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key no configurada");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "SistemaDental";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "SistemaDental";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Servicios de infraestructura
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();

// Unit of Work y Repositorios
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<IPacienteRepository, PacienteRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<ICitaRepository, CitaRepository>();
builder.Services.AddScoped<IOdontogramaRepository, OdontogramaRepository>();
builder.Services.AddScoped<ITratamientoRepository, TratamientoRepository>();

// Servicios de aplicación
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPacienteService, PacienteService>();
builder.Services.AddScoped<ICitaService, CitaService>();
builder.Services.AddScoped<IOdontogramaService, OdontogramaService>();
builder.Services.AddScoped<ITratamientoService, TratamientoService>();
builder.Services.AddScoped<IReporteService, ReporteService>();

// HttpContextAccessor para acceder al contexto HTTP
builder.Services.AddHttpContextAccessor();

// Validadores FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<PacienteCreateDtoValidator>();

// Logging
builder.Services.AddLogging();

var app = builder.Build();

// Configuración del pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sistema Dental API v1");
        c.RoutePrefix = string.Empty; // Swagger UI en la raíz
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Middleware personalizado
app.UseMiddleware<TenantMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<JwtTenantMiddleware>();

app.MapControllers();

// Aplicar migraciones automáticamente en desarrollo
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        try
        {
            dbContext.Database.Migrate();
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Error al aplicar migraciones");
        }
    }
}

app.Run();

