namespace SistemaDental.Domain.Enums;

public enum EstadoCita
{
    Pendiente = 1,
    Confirmada = 2,
    Completada = 3,
    Cancelada = 4
}

public static class EstadoCitaExtensions
{
    public static string ToString(this EstadoCita estado)
    {
        return estado switch
        {
            EstadoCita.Pendiente => "Pendiente",
            EstadoCita.Confirmada => "Confirmada",
            EstadoCita.Completada => "Completada",
            EstadoCita.Cancelada => "Cancelada",
            _ => "Desconocido"
        };
    }
}

