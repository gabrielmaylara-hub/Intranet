using Intranet.Models;

namespace Intranet.Repositories.Interfaces;

public interface IAreaPublicacionRepository
{
    Task<IEnumerable<AreaPublicacion>> ObtenerTodasAsync();
    Task<IEnumerable<AreaPublicacion>> ObtenerActivasAsync();
    Task<AreaPublicacion?> ObtenerPorIdAsync(int id);
    Task<bool> ExisteNombreAsync(string nombre, int? excluirId = null);
    Task<int> CrearAsync(AreaPublicacion area);
    Task<bool> ActualizarAsync(AreaPublicacion area);
    Task<bool> DesactivarAsync(int id);
    Task<bool> ActivarAsync(int id);
    Task<bool> PuedeEliminarAsync(int id);
    Task<bool> EliminarAsync(int id);
    Task<int> ActualizarOrdenAsync(IEnumerable<(int Id, int Orden)> items);
}
