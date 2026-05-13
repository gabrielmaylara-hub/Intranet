using Dapper;
using Intranet.Data;
using Intranet.Models;
using Intranet.Repositories.Interfaces;

namespace Intranet.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly ConexionDb _db;
    private const string ColumnasUsuarioAdmin =
        @"u.id, u.usuario, u.password_hash, u.nombre_completo, u.activo,
          u.rol, u.area_publicacion_id, a.nombre AS area_publicacion_nombre";

    public UsuarioRepository(ConexionDb db) => _db = db;

    public async Task<UsuarioAdmin?> ObtenerPorUsuarioAsync(string usuario)
    {
        using var con = _db.CrearConexion();
        return await con.QueryFirstOrDefaultAsync<UsuarioAdmin>(
            $@"SELECT {ColumnasUsuarioAdmin}
               FROM usuarios_admin u
               LEFT JOIN areas_publicacion a ON a.id = u.area_publicacion_id
               WHERE u.usuario = @usuario AND u.activo = 1",
            new { usuario });
    }

    public async Task<UsuarioAdmin?> ObtenerPorIdAsync(int id)
    {
        using var con = _db.CrearConexion();
        return await con.QueryFirstOrDefaultAsync<UsuarioAdmin>(
            $@"SELECT {ColumnasUsuarioAdmin}
               FROM usuarios_admin u
               LEFT JOIN areas_publicacion a ON a.id = u.area_publicacion_id
               WHERE u.id = @id AND u.activo = 1",
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
