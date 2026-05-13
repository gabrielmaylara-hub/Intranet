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
          u.rol, u.area_publicacion_id, a.nombre AS area_publicacion_nombre,
          u.fecha_creacion, u.fecha_actualizacion, u.ultimo_acceso";

    public UsuarioRepository(ConexionDb db) => _db = db;

    public async Task<IEnumerable<UsuarioAdmin>> ListarAsync()
    {
        using var con = _db.CrearConexion();
        return await con.QueryAsync<UsuarioAdmin>(
            $@"SELECT {ColumnasUsuarioAdmin}
               FROM usuarios_admin u
               LEFT JOIN areas_publicacion a ON a.id = u.area_publicacion_id
               ORDER BY u.activo DESC, u.rol ASC, u.usuario ASC");
    }

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
               WHERE u.id = @id",
            new { id });
    }

    public async Task<bool> ExistenUsuariosActivosAsync()
    {
        using var con = _db.CrearConexion();
        var total = await con.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM usuarios_admin WHERE activo = 1");
        return total > 0;
    }

    public async Task<bool> ExisteUsuarioAsync(string usuario, int? excluirId = null)
    {
        using var con = _db.CrearConexion();
        var total = await con.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*)
              FROM usuarios_admin
              WHERE usuario = @usuario
                AND (@excluirId IS NULL OR id <> @excluirId)",
            new { usuario = usuario.Trim(), excluirId });

        return total > 0;
    }

    public async Task<bool> ExisteAreaPublicacionAsync(int areaId)
    {
        using var con = _db.CrearConexion();
        var total = await con.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM areas_publicacion WHERE id = @areaId AND activa = 1",
            new { areaId });

        return total > 0;
    }

    public async Task<int> CrearAsync(UsuarioAdmin usuario)
    {
        using var con = _db.CrearConexion();
        return await con.ExecuteScalarAsync<int>(
            @"INSERT INTO usuarios_admin
                  (usuario, password_hash, nombre_completo, activo, rol, area_publicacion_id)
              VALUES
                  (@Usuario, @PasswordHash, @NombreCompleto, @Activo, @Rol, @AreaPublicacionId);
              SELECT LAST_INSERT_ID();",
            usuario);
    }

    public async Task<bool> ActualizarAsync(UsuarioAdmin usuario)
    {
        using var con = _db.CrearConexion();
        var filas = await con.ExecuteAsync(
            @"UPDATE usuarios_admin
              SET nombre_completo = @NombreCompleto,
                  activo = @Activo,
                  rol = @Rol,
                  area_publicacion_id = @AreaPublicacionId,
                  fecha_actualizacion = CURRENT_TIMESTAMP
              WHERE id = @Id",
            usuario);

        return filas == 1;
    }

    public async Task<bool> ActivarAsync(int id)
    {
        using var con = _db.CrearConexion();
        var filas = await con.ExecuteAsync(
            @"UPDATE usuarios_admin
              SET activo = 1,
                  fecha_actualizacion = CURRENT_TIMESTAMP
              WHERE id = @id",
            new { id });

        return filas == 1;
    }

    public async Task<bool> DesactivarAsync(int id)
    {
        using var con = _db.CrearConexion();
        var filas = await con.ExecuteAsync(
            @"UPDATE usuarios_admin
              SET activo = 0,
                  fecha_actualizacion = CURRENT_TIMESTAMP
              WHERE id = @id",
            new { id });

        return filas == 1;
    }

    public async Task<bool> ResetearPasswordAsync(int id, string nuevoHash)
    {
        using var con = _db.CrearConexion();
        var filas = await con.ExecuteAsync(
            @"UPDATE usuarios_admin
              SET password_hash = @nuevoHash,
                  fecha_actualizacion = CURRENT_TIMESTAMP
              WHERE id = @id",
            new { id, nuevoHash });

        return filas == 1;
    }

    public async Task<bool> ActualizarPasswordHashAsync(int id, string passwordHash)
    {
        using var con = _db.CrearConexion();
        var filas = await con.ExecuteAsync(
            @"UPDATE usuarios_admin
              SET password_hash = @passwordHash,
                  fecha_actualizacion = CURRENT_TIMESTAMP
              WHERE id = @id AND activo = 1",
            new { id, passwordHash });

        return filas == 1;
    }

    public async Task<int> ContarAdminsGeneralesActivosAsync(int? excluirId = null)
    {
        using var con = _db.CrearConexion();
        return await con.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*)
              FROM usuarios_admin
              WHERE activo = 1
                AND rol = 'admin_general'
                AND (@excluirId IS NULL OR id <> @excluirId)",
            new { excluirId });
    }

    public async Task<bool> RegistrarUltimoAccesoAsync(int id)
    {
        using var con = _db.CrearConexion();
        var filas = await con.ExecuteAsync(
            "UPDATE usuarios_admin SET ultimo_acceso = CURRENT_TIMESTAMP WHERE id = @id",
            new { id });

        return filas == 1;
    }
}
