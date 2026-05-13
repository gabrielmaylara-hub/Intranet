using Intranet.Models;

namespace Intranet.Repositories.Interfaces;

public interface IConfiguracionRepository
{
    Task<string?> ObtenerValorAsync(string clave);
    Task<Dictionary<string, string>> ObtenerTodosAsync();
    Task<IEnumerable<ConfiguracionSitio>> ObtenerConDetalleAsync();
    Task<IEnumerable<SitioEnlace>> ObtenerEnlacesAsync(string grupo, bool soloActivos = false);
    Task GuardarAsync(string clave, string valor);
    Task GuardarMultiplesAsync(Dictionary<string, string> valores);
    Task GuardarEnlacesAsync(string grupo, IEnumerable<SitioEnlace> enlaces);
}
