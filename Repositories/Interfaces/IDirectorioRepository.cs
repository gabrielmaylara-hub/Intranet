using Intranet.Models;

namespace Intranet.Repositories.Interfaces;

public interface IDirectorioRepository
{
    Task<IEnumerable<DirectorioEntrada>> ObtenerTodosAsync(bool soloActivos = false);
    Task<DirectorioEntrada?> ObtenerPorIdAsync(int id);
    Task<int> InsertarAsync(DirectorioEntrada entrada);
    Task ActualizarAsync(DirectorioEntrada entrada);
    Task EliminarAsync(int id);
    Task CambiarEstadoAsync(int id, bool activo);

    Task<IEnumerable<DirectorioArea>> ObtenerAreasAsync(bool soloActivas = false);
    Task<DirectorioArea?> ObtenerAreaPorIdAsync(int id);
    Task<DirectorioArea?> ObtenerAreaPorNombreAsync(string nombre);
    Task<int> InsertarAreaAsync(DirectorioArea area);
    Task ActualizarAreaAsync(DirectorioArea area);
    Task AsegurarAreaAsync(string nombre);
    Task ActualizarAreaDesdeImportacionAsync(
        string nombre,
        string? titular,
        string? ubicacion,
        string? correo);
}
