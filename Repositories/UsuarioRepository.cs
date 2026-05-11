using Dapper;
using Intranet.Data;
using Intranet.Models;
using Intranet.Repositories.Interfaces;

namespace Intranet.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly ConexionDb _db;

    public UsuarioRepository(ConexionDb db) => _db = db;

    public async Task<UsuarioAdmin?> ObtenerPorUsuarioAsync(string usuario)
    {
        using var con = _db.CrearConexion();
        return await con.QueryFirstOrDefaultAsync<UsuarioAdmin>(
            "SELECT * FROM usuarios_admin WHERE usuario = @usuario AND activo = 1",
            new { usuario });
    }

    public async Task<bool> ExistenUsuariosActivosAsync()
    {
        using var con = _db.CrearConexion();
        var total = await con.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM usuarios_admin WHERE activo = 1");
        return total > 0;
    }
}
