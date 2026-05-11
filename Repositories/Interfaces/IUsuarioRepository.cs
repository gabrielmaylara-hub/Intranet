using Intranet.Models;

namespace Intranet.Repositories.Interfaces;

public interface IUsuarioRepository
{
    Task<UsuarioAdmin?> ObtenerPorUsuarioAsync(string usuario);
    Task<bool> ExistenUsuariosActivosAsync();
}
