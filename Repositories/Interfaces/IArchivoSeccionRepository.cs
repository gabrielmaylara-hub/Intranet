using Intranet.Models;

namespace Intranet.Repositories.Interfaces;

public interface IArchivoSeccionRepository
{
    Task<IEnumerable<ArchivoSeccion>> ObtenerPorSeccionAsync(string seccion, bool soloActivos = false);
    Task<IEnumerable<ArchivoSeccion>> ObtenerTodosAsync();
    Task<ArchivoSeccion?> ObtenerPorIdAsync(int id);
    Task<int> InsertarAsync(ArchivoSeccion archivo);
    Task<int> ActualizarAsync(ArchivoSeccion archivo);
    Task<int> EliminarAsync(int id);
    Task CambiarEstadoAsync(int id, bool activo);
}
