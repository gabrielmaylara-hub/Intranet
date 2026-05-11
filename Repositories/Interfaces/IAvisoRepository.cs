using Intranet.Models;

namespace Intranet.Repositories.Interfaces;

public interface IAvisoRepository
{
    Task<IEnumerable<Aviso>> ObtenerTodosAsync(bool soloActivos = false);
    Task<Aviso?> ObtenerPorIdAsync(int id);
    Task<int> InsertarAsync(Aviso aviso);
    Task ActualizarAsync(Aviso aviso);
    Task EliminarAsync(int id);
    Task CambiarEstadoAsync(int id, bool activo);
}
