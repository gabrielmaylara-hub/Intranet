using Dapper;
using Intranet.Data;
using Intranet.Models;
using Intranet.Repositories.Interfaces;

namespace Intranet.Repositories;

public class AvisoRepository : IAvisoRepository
{
    private readonly ConexionDb _db;
    private const string ColumnasAviso =
        @"a.id, a.titulo, a.contenido, a.fecha_publicacion, a.activo, a.orden,
          a.area_publicacion_id, ap.nombre AS area_publicacion_nombre,
          a.creado_por_usuario_id, a.actualizado_por_usuario_id, a.fecha_actualizacion,
          a.pdf_path, a.pdf_nombre_original, a.pdf_content_type, a.pdf_tamano_bytes";

    public AvisoRepository(ConexionDb db) => _db = db;

    public async Task<IEnumerable<Aviso>> ObtenerTodosAsync(bool soloActivos = false)
    {
        using var con = _db.CrearConexion();
        var filtro = soloActivos ? "WHERE a.activo = 1" : "";
        return await con.QueryAsync<Aviso>(
            $@"SELECT {ColumnasAviso}
               FROM avisos a
               LEFT JOIN areas_publicacion ap ON ap.id = a.area_publicacion_id
               {filtro}
               ORDER BY a.fecha_publicacion DESC, a.id DESC");
    }

    public async Task<IEnumerable<Aviso>> ObtenerPorAreaPublicacionAsync(
        int areaPublicacionId,
        bool soloActivos = false)
    {
        using var con = _db.CrearConexion();
        var filtroActivo = soloActivos ? "AND a.activo = 1" : "";
        return await con.QueryAsync<Aviso>(
            $@"SELECT {ColumnasAviso}
               FROM avisos a
               LEFT JOIN areas_publicacion ap ON ap.id = a.area_publicacion_id
               WHERE a.area_publicacion_id = @areaPublicacionId
                 {filtroActivo}
               ORDER BY a.fecha_publicacion DESC, a.id DESC",
            new { areaPublicacionId });
    }

    public async Task<Aviso?> ObtenerPorIdAsync(int id)
    {
        using var con = _db.CrearConexion();
        return await con.QueryFirstOrDefaultAsync<Aviso>(
            $@"SELECT {ColumnasAviso}
               FROM avisos a
               LEFT JOIN areas_publicacion ap ON ap.id = a.area_publicacion_id
               WHERE a.id = @id",
            new { id });
    }

    public async Task<int> InsertarAsync(Aviso aviso)
    {
        using var con = _db.CrearConexion();
        return await con.ExecuteScalarAsync<int>(
            @"INSERT INTO avisos
                  (titulo, contenido, fecha_publicacion, activo, orden,
                   area_publicacion_id, creado_por_usuario_id,
                   pdf_path, pdf_nombre_original, pdf_content_type, pdf_tamano_bytes)
              VALUES
                  (@Titulo, @Contenido, @FechaPublicacion, @Activo, @Orden,
                   @AreaPublicacionId, @CreadoPorUsuarioId,
                   @PdfPath, @PdfNombreOriginal, @PdfContentType, @PdfTamanoBytes);
              SELECT LAST_INSERT_ID();",
            aviso);
    }

    public async Task ActualizarAsync(Aviso aviso)
    {
        using var con = _db.CrearConexion();
        await con.ExecuteAsync(
            @"UPDATE avisos
              SET titulo = @Titulo, contenido = @Contenido,
                  fecha_publicacion = @FechaPublicacion,
                  activo = @Activo,
                  orden = @Orden,
                  area_publicacion_id = @AreaPublicacionId,
                  actualizado_por_usuario_id = @ActualizadoPorUsuarioId,
                  fecha_actualizacion = CURRENT_TIMESTAMP,
                  pdf_path = @PdfPath,
                  pdf_nombre_original = @PdfNombreOriginal,
                  pdf_content_type = @PdfContentType,
                  pdf_tamano_bytes = @PdfTamanoBytes
              WHERE id = @Id",
            aviso);
    }

    public async Task EliminarAsync(int id)
    {
        using var con = _db.CrearConexion();
        await con.ExecuteAsync("DELETE FROM avisos WHERE id = @id", new { id });
    }

    public async Task CambiarEstadoAsync(int id, bool activo)
    {
        using var con = _db.CrearConexion();
        await con.ExecuteAsync(
            @"UPDATE avisos
              SET activo = @activo,
                  fecha_actualizacion = CURRENT_TIMESTAMP
              WHERE id = @id",
            new { id, activo });
    }
}
