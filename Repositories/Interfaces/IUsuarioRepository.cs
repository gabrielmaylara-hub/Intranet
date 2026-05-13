using Intranet.Models;

namespace Intranet.Repositories.Interfaces;

public interface IUsuarioRepository
{
    Task<IEnumerable<UsuarioAdmin>> ListarAsync();
    Task<UsuarioAdmin?> ObtenerPorUsuarioAsync(string usuario);
    Task<UsuarioAdmin?> ObtenerPorIdAsync(int id);
    Task<bool> ExistenUsuariosActivosAsync();
    Task<bool> ExisteUsuarioAsync(string usuario, int? excluirId = null);
    Task<bool> ExisteAreaPublicacionAsync(int areaId);
    Task<int> CrearAsync(UsuarioAdmin usuario);
    Task<bool> ActualizarAsync(UsuarioAdmin usuario);
    Task<bool> ActivarAsync(int id);
    Task<bool> DesactivarAsync(int id);
    Task<bool> ResetearPasswordAsync(int id, string nuevoHash);
    Task<bool> ActualizarPasswordHashAsync(int id, string passwordHash);
    Task<int> ContarAdminsGeneralesActivosAsync(int? excluirId = null);
    Task<bool> RegistrarUltimoAccesoAsync(int id);
}
