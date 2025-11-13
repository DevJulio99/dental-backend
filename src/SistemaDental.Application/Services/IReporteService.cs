using SistemaDental.Application.DTOs.Reportes;

namespace SistemaDental.Application.Services;

public interface IReporteService
{
    Task<ReporteCitasDto> GetReporteCitasAsync(DateTime? fechaInicio = null, DateTime? fechaFin = null);
    Task<ReporteTratamientosDto> GetReporteTratamientosAsync(DateTime? fechaInicio = null, DateTime? fechaFin = null);
    Task<int> GetTotalPacientesAsync();
    Task<Dictionary<string, int>> GetPacientesPorMesAsync(int año);
}

