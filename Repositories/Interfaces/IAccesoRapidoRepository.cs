using Intranet.Models;

namespace Intranet.Repositories.Interfaces;

public interface IAccesoRapidoRepository
{
    Task<IEnumerable<AccesoRapido>> ObtenerTodosAsync(bool soloActivos = false);
    Task<AccesoRapido?> ObtenerPorIdAsync(int id);
    Task<int> InsertarAsync(AccesoRapido acceso);
    Task ActualizarAsync(AccesoRapido acceso);
    Task EliminarAsync(int id);
    Task CambiarEstadoAsync(int id, bool activo);
    Task ActualizarOrdenAsync(IEnumerable<(int Id, int Orden)> items);
}
