namespace SistemaDental.Domain.Enums;

public enum EstadoDiente
{
    Sano = 1,
    Curado = 2,
    Pendiente = 3,
    Extraido = 4,
    Caries = 5,
    Endodoncia = 6,
    Corona = 7,
    Implante = 8
}

public static class EstadoDienteExtensions
{
    public static string ToString(this EstadoDiente estado)
    {
        return estado switch
        {
            EstadoDiente.Sano => "Sano",
            EstadoDiente.Curado => "Curado",
            EstadoDiente.Pendiente => "Pendiente",
            EstadoDiente.Extraido => "Extraido",
            EstadoDiente.Caries => "Caries",
            EstadoDiente.Endodoncia => "Endodoncia",
            EstadoDiente.Corona => "Corona",
            EstadoDiente.Implante => "Implante",
            _ => "Desconocido"
        };
    }
}

