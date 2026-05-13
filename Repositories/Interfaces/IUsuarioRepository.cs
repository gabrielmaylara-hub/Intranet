using Intranet.Models;

namespace Intranet.Repositories.Interfaces;

public interface IUsuarioRepository
{
    Task<UsuarioAdmin?> ObtenerPorUsuarioAsync(string usuario);
    Task<UsuarioAdmin?> ObtenerPorIdAsync(int id);
    Task<bool> ExistenUsuariosActivosAsync();
    Task<bool> ActualizarPasswordHashAsync(int id, string passwordHash);
}
