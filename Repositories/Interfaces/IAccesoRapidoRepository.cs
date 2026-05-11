using Intranet.Models;

namespace Intranet.Repositories.Interfaces;

public interface IAccesoRapidoRepository
{
    Task<IEnumerable<AccesoRapido>> ObtenerTodosAsync(bool soloActivos = false);
    Task<AccesoRapido?> ObtenerPorIdAsync(int id);
    Task<int> InsertarAsync(AccesoRapido acceso);
    Task<int> ActualizarAsync(AccesoRapido acceso);
    Task<int> EliminarAsync(int id);
    Task CambiarEstadoAsync(int id, bool activo);
    Task<int> ActualizarOrdenAsync(IEnumerable<(int Id, int Orden)> items);
}
