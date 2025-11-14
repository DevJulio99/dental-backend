using SistemaDental.Application.DTOs.Odontograma;

namespace SistemaDental.Application.Services;

/// <summary>
/// Interfaz para el servicio de gestión de odontogramas
/// </summary>
public interface IOdontogramaService
{
    /// <summary>
    /// Obtiene todos los registros de odontograma de un paciente
    /// </summary>
    /// <param name="pacienteId">ID del paciente</param>
    /// <returns>Lista de registros de odontograma ordenados por fecha descendente</returns>
    Task<IEnumerable<OdontogramaDto>> GetByPacienteAsync(Guid pacienteId);

    /// <summary>
    /// Crea un nuevo registro de odontograma
    /// </summary>
    /// <param name="dto">Datos del odontograma a crear</param>
    /// <returns>Odontograma creado</returns>
    /// <exception cref="InvalidOperationException">Si el paciente no existe, el número de diente es inválido o el usuario no está identificado</exception>
    Task<OdontogramaDto> CreateAsync(OdontogramaCreateDto dto);

    /// <summary>
    /// Actualiza un registro de odontograma existente
    /// </summary>
    /// <param name="id">ID del odontograma a actualizar</param>
    /// <param name="dto">Datos actualizados del odontograma</param>
    /// <returns>Odontograma actualizado o null si no se encuentra</returns>
    Task<OdontogramaDto?> UpdateAsync(Guid id, OdontogramaCreateDto dto);

    /// <summary>
    /// Elimina un registro de odontograma
    /// </summary>
    /// <param name="id">ID del odontograma a eliminar</param>
    /// <returns>True si se eliminó correctamente, False si no se encontró</returns>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// Obtiene el estado actual (más reciente) de todos los dientes de un paciente
    /// </summary>
    /// <param name="pacienteId">ID del paciente</param>
    /// <returns>Diccionario con el número de diente como clave y su estado actual como valor</returns>
    Task<Dictionary<int, OdontogramaDto?>> GetEstadoActualDientesAsync(Guid pacienteId);

    /// <summary>
    /// Obtiene el historial completo de un diente específico
    /// </summary>
    /// <param name="pacienteId">ID del paciente</param>
    /// <param name="numeroDiente">Número del diente</param>
    /// <returns>Lista de registros históricos del diente ordenados por fecha descendente</returns>
    Task<IEnumerable<OdontogramaDto>> GetHistorialByDienteAsync(Guid pacienteId, int numeroDiente);

    /// <summary>
    /// Obtiene el historial agrupado por diente de un paciente
    /// </summary>
    /// <param name="pacienteId">ID del paciente</param>
    /// <returns>Diccionario con el número de diente como clave y lista de registros históricos como valor</returns>
    Task<Dictionary<int, IEnumerable<OdontogramaDto>>> GetHistorialAgrupadoAsync(Guid pacienteId);

    /// <summary>
    /// Obtiene los registros de odontograma de un paciente filtrados por rango de fechas
    /// </summary>
    /// <param name="pacienteId">ID del paciente</param>
    /// <param name="fechaDesde">Fecha desde (opcional)</param>
    /// <param name="fechaHasta">Fecha hasta (opcional)</param>
    /// <returns>Lista de registros filtrados por fecha</returns>
    Task<IEnumerable<OdontogramaDto>> GetByPacienteConFiltrosAsync(Guid pacienteId, DateOnly? fechaDesde, DateOnly? fechaHasta);

    /// <summary>
    /// Obtiene el estado de todos los dientes en una fecha específica
    /// </summary>
    /// <param name="pacienteId">ID del paciente</param>
    /// <param name="fecha">Fecha para la cual se desea obtener el estado</param>
    /// <returns>Diccionario con el número de diente como clave y su estado en esa fecha como valor</returns>
    Task<Dictionary<int, OdontogramaDto?>> GetEstadoDientesEnFechaAsync(Guid pacienteId, DateOnly fecha);

    /// <summary>
    /// Crea múltiples registros de odontograma en una sola operación
    /// </summary>
    /// <param name="dtos">Lista de datos de odontogramas a crear</param>
    /// <returns>Lista de odontogramas creados</returns>
    Task<IEnumerable<OdontogramaDto>> CreateBatchAsync(IEnumerable<OdontogramaCreateDto> dtos);
}

