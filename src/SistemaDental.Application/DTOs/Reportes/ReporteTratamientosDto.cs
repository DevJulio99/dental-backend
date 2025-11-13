namespace SistemaDental.Application.DTOs.Reportes;

public class ReporteTratamientosDto
{
    public List<TratamientoComunDto> TratamientosMasComunes { get; set; } = new();
    public decimal TotalIngresos { get; set; }
    public int TotalTratamientos { get; set; }
}

public class TratamientoComunDto
{
    public string Nombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal TotalIngresos { get; set; }
}

