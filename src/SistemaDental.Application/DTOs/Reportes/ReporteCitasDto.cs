namespace SistemaDental.Application.DTOs.Reportes;

public class ReporteCitasDto
{
    public int TotalCitas { get; set; }
    public int CitasPendientes { get; set; }
    public int CitasConfirmadas { get; set; }
    public int CitasCompletadas { get; set; }
    public int CitasCanceladas { get; set; }
    public List<CitaPorFechaDto> CitasPorFecha { get; set; } = new();
}

public class CitaPorFechaDto
{
    public DateTime Fecha { get; set; }
    public int Cantidad { get; set; }
}

