using Dapper;
using Intranet.Data;
using Intranet.Models;
using Intranet.Repositories.Interfaces;

namespace Intranet.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly ConexionDb _db;
    private const string ColumnasUsuarioAdmin =
        "id, usuario, password_hash, nombre_completo, activo";

    public UsuarioRepository(ConexionDb db) => _db = db;

    public async Task<UsuarioAdmin?> ObtenerPorUsuarioAsync(string usuario)
    {
        using var con = _db.CrearConexion();
        return await con.QueryFirstOrDefaultAsync<UsuarioAdmin>(
            $"SELECT {ColumnasUsuarioAdmin} FROM usuarios_admin WHERE usuario = @usuario AND activo = 1",
            new { usuario });
    }

    public async Task<UsuarioAdmin?> ObtenerPorIdAsync(int id)
    {
        using var con = _db.CrearConexion();
        return await con.QueryFirstOrDefaultAsync<UsuarioAdmin>(
            $"SELECT {ColumnasUsuarioAdmin} FROM usuarios_admin WHERE id = @id AND activo = 1",
            new { id });
    }

    public async Task<bool> ExistenUsuariosActivosAsync()
    {
        using var con = _db.CrearConexion();
        var total = await con.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM usuarios_admin WHERE activo = 1");
        return total > 0;
    }

    public async Task<bool> ActualizarPasswordHashAsync(int id, string passwordHash)
    {
        using var con = _db.CrearConexion();
        var filas = await con.ExecuteAsync(
            "UPDATE usuarios_admin SET password_hash = @passwordHash WHERE id = @id AND activo = 1",
            new { id, passwordHash });

        return filas == 1;
    }
}
