using Intranet.Models;

namespace Intranet.Repositories.Interfaces;

public interface ITutorialRepository
{
    Task<IEnumerable<Tutorial>> ObtenerTodosAsync(bool soloActivos = false);
    Task<IEnumerable<Tutorial>> ObtenerPorAreaPublicacionAsync(int areaPublicacionId, bool soloActivos = false);
    Task<Tutorial?> ObtenerPorIdAsync(int id);
    Task<int> InsertarAsync(Tutorial tutorial);
    Task<int> ActualizarAsync(Tutorial tutorial);
    Task<int> EliminarAsync(int id);
    Task CambiarEstadoAsync(int id, bool activo, int? actualizadoPorUsuarioId = null);
}
