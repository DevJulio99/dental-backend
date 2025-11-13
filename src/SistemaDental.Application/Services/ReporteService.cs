using Microsoft.Extensions.Logging;
using SistemaDental.Application.DTOs.Reportes;
using SistemaDental.Infrastructure.Repositories;
using SistemaDental.Infrastructure.Services;

namespace SistemaDental.Application.Services;

public class ReporteService : IReporteService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantService _tenantService;
    private readonly ILogger<ReporteService> _logger;

    public ReporteService(
        IUnitOfWork unitOfWork,
        ITenantService tenantService,
        ILogger<ReporteService> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantService = tenantService;
        _logger = logger;
    }

    public async Task<ReporteCitasDto> GetReporteCitasAsync(DateTime? fechaInicio = null, DateTime? fechaFin = null)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue)
            return new ReporteCitasDto();

        IEnumerable<Domain.Entities.Cita> citas;
        
        if (fechaInicio.HasValue || fechaFin.HasValue)
        {
            var fechaInicioDate = fechaInicio.HasValue ? DateOnly.FromDateTime(fechaInicio.Value) : DateOnly.MinValue;
            var fechaFinDate = fechaFin.HasValue ? DateOnly.FromDateTime(fechaFin.Value) : DateOnly.MaxValue;
            citas = await _unitOfWork.Citas.GetByDateRangeAsync(tenantId.Value, fechaInicioDate, fechaFinDate);
        }
        else
        {
            citas = await _unitOfWork.Citas.GetByTenantAsync(tenantId.Value);
        }

        var reporte = new ReporteCitasDto
        {
            TotalCitas = citas.Count(),
            CitasPendientes = citas.Count(c => c.Estado == "scheduled"),
            CitasConfirmadas = citas.Count(c => c.Estado == "confirmed"),
            CitasCompletadas = citas.Count(c => c.Estado == "completed"),
            CitasCanceladas = citas.Count(c => c.Estado == "cancelled")
        };

        // Agrupar citas por fecha
        var citasPorFecha = citas
            .GroupBy(c => c.AppointmentDate)
            .Select(g => new CitaPorFechaDto
            {
                Fecha = g.Key.ToDateTime(TimeOnly.MinValue),
                Cantidad = g.Count()
            })
            .OrderBy(x => x.Fecha)
            .ToList();

        reporte.CitasPorFecha = citasPorFecha;

        return reporte;
    }

    public async Task<ReporteTratamientosDto> GetReporteTratamientosAsync(DateTime? fechaInicio = null, DateTime? fechaFin = null)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue)
            return new ReporteTratamientosDto();

        var tratamientos = await _unitOfWork.Tratamientos.GetByTenantAsync(tenantId.Value);
        
        // Filtrar por fechas si se proporcionan
        if (fechaInicio.HasValue)
        {
            var fechaInicioDate = DateOnly.FromDateTime(fechaInicio.Value);
            tratamientos = tratamientos.Where(t => t.TreatmentDate >= fechaInicioDate);
        }

        if (fechaFin.HasValue)
        {
            var fechaFinDate = DateOnly.FromDateTime(fechaFin.Value);
            tratamientos = tratamientos.Where(t => t.TreatmentDate <= fechaFinDate);
        }

        var tratamientosComunes = tratamientos
            .GroupBy(t => t.TreatmentPerformed)
            .Select(g => new TratamientoComunDto
            {
                Nombre = g.Key,
                Cantidad = g.Count(),
                TotalIngresos = g.Sum(t => t.Costo ?? 0)
            })
            .OrderByDescending(t => t.Cantidad)
            .Take(10)
            .ToList();

        var reporte = new ReporteTratamientosDto
        {
            TratamientosMasComunes = tratamientosComunes,
            TotalIngresos = tratamientos.Sum(t => t.Costo ?? 0),
            TotalTratamientos = tratamientos.Count()
        };

        return reporte;
    }

    public async Task<int> GetTotalPacientesAsync()
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue) return 0;

        var pacientes = await _unitOfWork.Pacientes.GetByTenantAsync(tenantId.Value);
        return pacientes.Count();
    }

    public async Task<Dictionary<string, int>> GetPacientesPorMesAsync(int año)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue) return new Dictionary<string, int>();

        var pacientes = await _unitOfWork.Pacientes.GetByTenantAsync(tenantId.Value);
        pacientes = pacientes.Where(p => p.FechaCreacion.Year == año);

        var resultado = new Dictionary<string, int>();

        for (int mes = 1; mes <= 12; mes++)
        {
            var nombreMes = new DateTime(año, mes, 1).ToString("MMMM", new System.Globalization.CultureInfo("es-ES"));
            resultado[nombreMes] = pacientes.Count(p => p.FechaCreacion.Month == mes);
        }

        return resultado;
    }
}

