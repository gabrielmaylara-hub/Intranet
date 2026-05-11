using Intranet.Models;

namespace Intranet.Repositories.Interfaces;

public interface IConfiguracionRepository
{
    Task<string?> ObtenerValorAsync(string clave);
    Task<Dictionary<string, string>> ObtenerTodosAsync();
    Task<IEnumerable<ConfiguracionSitio>> ObtenerConDetalleAsync();
    Task GuardarAsync(string clave, string valor);
    Task GuardarMultiplesAsync(Dictionary<string, string> valores);
}
