using Dapper;
using Intranet.Data;
using Intranet.Models;
using Intranet.Repositories.Interfaces;

namespace Intranet.Repositories;

public class TutorialRepository : ITutorialRepository
{
    private readonly ConexionDb _db;
    private const string ColumnasTutorial =
        @"t.id, t.titulo, t.descripcion, t.archivo_path, t.miniatura_path, t.orden,
          t.activo, t.fecha_creacion,
          t.area_publicacion_id, ap.nombre AS area_publicacion_nombre,
          t.creado_por_usuario_id, t.actualizado_por_usuario_id, t.fecha_actualizacion";

    public TutorialRepository(ConexionDb db) => _db = db;

    public async Task<IEnumerable<Tutorial>> ObtenerTodosAsync(bool soloActivos = false)
    {
        using var con = _db.CrearConexion();
        var filtro = soloActivos ? "WHERE t.activo = 1" : "";
        return await con.QueryAsync<Tutorial>(
            $@"SELECT {ColumnasTutorial}
               FROM tutoriales t
               LEFT JOIN areas_publicacion ap ON ap.id = t.area_publicacion_id
               {filtro}
               ORDER BY t.orden ASC, t.fecha_creacion DESC");
    }

    public async Task<IEnumerable<Tutorial>> ObtenerPorAreaPublicacionAsync(
        int areaPublicacionId,
        bool soloActivos = false)
    {
        using var con = _db.CrearConexion();
        var filtroActivo = soloActivos ? "AND t.activo = 1" : "";
        return await con.QueryAsync<Tutorial>(
            $@"SELECT {ColumnasTutorial}
               FROM tutoriales t
               LEFT JOIN areas_publicacion ap ON ap.id = t.area_publicacion_id
               WHERE t.area_publicacion_id = @areaPublicacionId
                 {filtroActivo}
               ORDER BY t.orden ASC, t.fecha_creacion DESC",
            new { areaPublicacionId });
    }

    public async Task<Tutorial?> ObtenerPorIdAsync(int id)
    {
        using var con = _db.CrearConexion();
        return await con.QueryFirstOrDefaultAsync<Tutorial>(
            $@"SELECT {ColumnasTutorial}
               FROM tutoriales t
               LEFT JOIN areas_publicacion ap ON ap.id = t.area_publicacion_id
               WHERE t.id = @id",
            new { id });
    }

    public async Task<int> InsertarAsync(Tutorial tutorial)
    {
        using var con = _db.CrearConexion();
        return await con.ExecuteScalarAsync<int>(
            @"INSERT INTO tutoriales
                  (titulo, descripcion, archivo_path, miniatura_path, orden, activo,
                   area_publicacion_id, creado_por_usuario_id)
              VALUES
                  (@Titulo, @Descripcion, @ArchivoPath, @MiniaturaPath, @Orden, @Activo,
                   @AreaPublicacionId, @CreadoPorUsuarioId);
              SELECT LAST_INSERT_ID();",
            tutorial);
    }

    public async Task<int> ActualizarAsync(Tutorial tutorial)
    {
        using var con = _db.CrearConexion();
        await con.OpenAsync();
        using var tx = await con.BeginTransactionAsync();

        await con.ExecuteAsync(
            @"UPDATE tutoriales
              SET titulo = @Titulo, descripcion = @Descripcion,
                  archivo_path = @ArchivoPath, miniatura_path = @MiniaturaPath,
                  orden = @Orden, activo = @Activo,
                  area_publicacion_id = @AreaPublicacionId,
                  actualizado_por_usuario_id = @ActualizadoPorUsuarioId,
                  fecha_actualizacion = CURRENT_TIMESTAMP
              WHERE id = @Id",
            tutorial,
            tx);

        var filasVerificadas = await con.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM tutoriales WHERE id = @Id",
            tutorial,
            tx);

        if (filasVerificadas == 1)
            await tx.CommitAsync();
        else
            await tx.RollbackAsync();

        return filasVerificadas;
    }

    public async Task<int> EliminarAsync(int id)
    {
        using var con = _db.CrearConexion();
        return await con.ExecuteAsync("DELETE FROM tutoriales WHERE id = @id", new { id });
    }

    public async Task CambiarEstadoAsync(int id, bool activo, int? actualizadoPorUsuarioId = null)
    {
        using var con = _db.CrearConexion();
        await con.ExecuteAsync(
            @"UPDATE tutoriales
              SET activo = @activo,
                  actualizado_por_usuario_id = @actualizadoPorUsuarioId,
                  fecha_actualizacion = CURRENT_TIMESTAMP
              WHERE id = @id",
            new { id, activo, actualizadoPorUsuarioId });
    }
}
