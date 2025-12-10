using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SistemaDental.Domain.Enums;
using SistemaDental.Infrastructure.Repositories;

namespace SistemaDental.API.BackgroundServices;

public class CitaStatusUpdateService : BackgroundService
{
    private readonly ILogger<CitaStatusUpdateService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public CitaStatusUpdateService(ILogger<CitaStatusUpdateService> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Servicio de actualización de estado de citas iniciado.");

        // Esperar un breve momento antes de la primera ejecución para asegurar que la aplicación esté completamente iniciada
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); 

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Ejecutando ciclo de actualización de estado de citas.");

            await ActualizarCitasVencidas(stoppingToken);

            // Esperamos 5 minutos antes de la siguiente ejecución
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
        _logger.LogInformation("Servicio de actualización de estado de citas detenido.");
    }

    private async Task ActualizarCitasVencidas(CancellationToken stoppingToken)
    {
        // Es importante crear un "scope" porque este servicio es un Singleton,
        // pero los servicios que usa (como el DbContext y IUnitOfWork) son Scoped.
        using (var scope = _scopeFactory.CreateScope())
        {
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<CitaStatusUpdateService>>();

            try
            {
                // Definir el umbral de tiempo: fecha y hora local del servidor menos 15 minutos de tolerancia.
                // Se usa DateTime.Now para comparar con las horas de las citas que están guardadas en hora local.
                var umbralDateTime = DateTime.Now.AddMinutes(-15);
                var umbralFecha = DateOnly.FromDateTime(umbralDateTime);
                var umbralTiempo = TimeOnly.FromDateTime(umbralDateTime);

                var citasParaActualizar = await unitOfWork.Citas.FindAsync(c =>
                    (c.Estado == AppointmentStatus.Scheduled || c.Estado == AppointmentStatus.Confirmed) &&
                    c.DeletedAt == null &&
                    (c.AppointmentDate < umbralFecha || (c.AppointmentDate == umbralFecha && c.StartTime < umbralTiempo))
                );

                if (citasParaActualizar.Any())
                {
                    logger.LogInformation("Se encontraron {Count} citas para marcar como 'NoShow'.", citasParaActualizar.Count());
                    foreach (var cita in citasParaActualizar)
                    {
                        cita.Estado = AppointmentStatus.NoShow;
                        cita.UpdatedAt = DateTime.UtcNow;
                        await unitOfWork.Citas.UpdateAsync(cita);
                    }
                    await unitOfWork.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ocurrió un error al actualizar el estado de las citas.");
            }
        }
    }
}