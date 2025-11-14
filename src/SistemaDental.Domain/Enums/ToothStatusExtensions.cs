namespace SistemaDental.Domain.Enums;

/// <summary>
/// Extensiones para el enum ToothStatus que permiten conversión a/desde valores en español
/// para compatibilidad con el frontend
/// </summary>
public static class ToothStatusExtensions
{
    /// <summary>
    /// Convierte un valor en español a ToothStatus
    /// </summary>
    public static ToothStatus FromSpanish(string estadoEspañol)
    {
        return estadoEspañol?.ToLowerInvariant() switch
        {
            "sano" or "healthy" => ToothStatus.Healthy,
            "curado" or "filled" => ToothStatus.Filled,
            "pendiente" or "in_treatment" or "in treatment" => ToothStatus.InTreatment,
            "caries" or "cavity" => ToothStatus.Cavity,
            "extraido" or "extraído" or "missing" => ToothStatus.Missing,
            "endodoncia" or "root_canal" or "root canal" => ToothStatus.RootCanal,
            "corona" or "crown" => ToothStatus.Crown,
            "implante" or "implant" => ToothStatus.Implant,
            "fracturado" or "fractured" => ToothStatus.Fractured,
            "a_extraer" or "a extraer" or "to_extract" or "to extract" => ToothStatus.ToExtract,
            "puente" or "bridge" => ToothStatus.Bridge,
            _ => ToothStatus.Healthy // Por defecto, sano
        };
    }

    /// <summary>
    /// Convierte ToothStatus a su equivalente en español
    /// </summary>
    public static string ToSpanish(this ToothStatus estado)
    {
        return estado switch
        {
            ToothStatus.Healthy => "sano",
            ToothStatus.Filled => "curado",
            ToothStatus.InTreatment => "pendiente",
            ToothStatus.Cavity => "caries",
            ToothStatus.Missing => "extraido",
            ToothStatus.RootCanal => "endodoncia",
            ToothStatus.Crown => "corona",
            ToothStatus.Implant => "implante",
            ToothStatus.Fractured => "fracturado",
            ToothStatus.ToExtract => "a_extraer",
            ToothStatus.Bridge => "puente",
            _ => "sano"
        };
    }

    /// <summary>
    /// Obtiene el nombre legible en español del estado
    /// </summary>
    public static string GetDisplayName(this ToothStatus estado)
    {
        return estado switch
        {
            ToothStatus.Healthy => "Sano",
            ToothStatus.Filled => "Curado",
            ToothStatus.InTreatment => "Pendiente",
            ToothStatus.Cavity => "Caries",
            ToothStatus.Missing => "Extraído",
            ToothStatus.RootCanal => "Endodoncia",
            ToothStatus.Crown => "Corona",
            ToothStatus.Implant => "Implante",
            ToothStatus.Fractured => "Fracturado",
            ToothStatus.ToExtract => "A Extraer",
            ToothStatus.Bridge => "Puente",
            _ => "Desconocido"
        };
    }
}

