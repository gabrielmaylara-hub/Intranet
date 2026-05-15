using Dapper;
using Intranet.Data;
using Intranet.Models;
using Intranet.Repositories.Interfaces;

namespace Intranet.Repositories;

// Este repositorio concentra el SQL de las areas de publicacion. Estas areas
// condicionan permisos de administracion y segmentacion de contenido en avisos,
// tutoriales y usuarios. Los valores de entrada deben ir como parametros de
// Dapper; las interpolaciones deben limitarse a constantes internas.
public class AreaPublicacionRepository : IAreaPublicacionRepository
{
    private readonly ConexionDb _db;
    // nombre, slug y activa afectan catalogos del panel Admin y visibilidad
    // funcional de modulos asociados. Mantener esta proyeccion alineada con el modelo.
    private const string ColumnasAreaPublicacion =
        "id, nombre, slug, descripcion, orden, activa, fecha_creacion, fecha_actualizacion";

    public AreaPublicacionRepository(ConexionDb db) => _db = db;

    public async Task<IEnumerable<AreaPublicacion>> ObtenerTodasAsync()
    {
        // Se usa en catalogos administrativos y formularios que dependen del
        // universo completo de areas, incluso cuando alguna esta desactivada.
        using var con = _db.CrearConexion();
        return await con.QueryAsync<AreaPublicacion>(
            $"SELECT {ColumnasAreaPublicacion} FROM areas_publicacion ORDER BY orden ASC, nombre ASC");
    }

    public async Task<IEnumerable<AreaPublicacion>> ObtenerActivasAsync()
    {
        using var con = _db.CrearConexion();
        return await con.QueryAsync<AreaPublicacion>(
            $@"SELECT {ColumnasAreaPublicacion}
               FROM areas_publicacion
               WHERE activa = 1
               ORDER BY orden ASC, nombre ASC");
    }

    public async Task<AreaPublicacion?> ObtenerPorIdAsync(int id)
    {
        using var con = _db.CrearConexion();
        return await con.QueryFirstOrDefaultAsync<AreaPublicacion>(
            $"SELECT {ColumnasAreaPublicacion} FROM areas_publicacion WHERE id = @id",
            new { id });
    }

    public async Task<bool> ExisteNombreAsync(string nombre, int? excluirId = null)
    {
        using var con = _db.CrearConexion();
        var total = await con.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*)
              FROM areas_publicacion
              WHERE nombre = @nombre
                AND (@excluirId IS NULL OR id <> @excluirId)",
            new { nombre = nombre.Trim(), excluirId });

        return total > 0;
    }

    public async Task<int> CrearAsync(AreaPublicacion area)
    {
        using var con = _db.CrearConexion();
        // LAST_INSERT_ID() es dependiente de MySQL. Documentarlo aqui ayuda a
        // evaluar el alcance si el repositorio cambia de proveedor algun dia.
        return await con.ExecuteScalarAsync<int>(
            @"INSERT INTO areas_publicacion
                  (nombre, slug, descripcion, orden, activa)
              VALUES
                  (@Nombre, @Slug, NULLIF(@Descripcion, ''), @Orden, @Activa);
              SELECT LAST_INSERT_ID();",
            area);
    }

    public async Task<bool> ActualizarAsync(AreaPublicacion area)
    {
        using var con = _db.CrearConexion();
        var filas = await con.ExecuteAsync(
            @"UPDATE areas_publicacion
              SET nombre = @Nombre,
                  descripcion = NULLIF(@Descripcion, ''),
                  orden = @Orden,
                  activa = @Activa,
                  fecha_actualizacion = CURRENT_TIMESTAMP
              WHERE id = @Id",
            area);

        return filas == 1;
    }

    public async Task<bool> DesactivarAsync(int id)
    {
        using var con = _db.CrearConexion();
        var filas = await con.ExecuteAsync(
            @"UPDATE areas_publicacion
              SET activa = 0,
                  fecha_actualizacion = CURRENT_TIMESTAMP
              WHERE id = @id",
            new { id });

        return filas == 1;
    }

    public async Task<bool> ActivarAsync(int id)
    {
        using var con = _db.CrearConexion();
        var filas = await con.ExecuteAsync(
            @"UPDATE areas_publicacion
              SET activa = 1,
                  fecha_actualizacion = CURRENT_TIMESTAMP
              WHERE id = @id",
            new { id });

        return filas == 1;
    }

    public async Task<bool> PuedeEliminarAsync(int id)
    {
        // Antes de eliminar, se valida dependencia con usuarios administrativos.
        // Este chequeo es delicado porque protege consistencia de permisos.
        using var con = _db.CrearConexion();
        var usuarios = await con.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM usuarios_admin WHERE area_publicacion_id = @id",
            new { id });

        // Actualmente este metodo solo bloquea eliminacion por dependencia con
        // usuarios administrativos. Avisos y tutoriales tambien pueden quedar
        // vinculados a un area; si la regla de negocio exige bloquear por ese
        // contenido asociado, este metodo debe ampliarse explicitamente.
        return usuarios == 0;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        if (!await PuedeEliminarAsync(id))
            return false;

        using var con = _db.CrearConexion();
        var filas = await con.ExecuteAsync(
            "DELETE FROM areas_publicacion WHERE id = @id",
            new { id });

        return filas == 1;
    }

    public async Task<int> ActualizarOrdenAsync(IEnumerable<(int Id, int Orden)> items)
    {
        var lista = items.ToList();

        using var con = _db.CrearConexion();
        await con.OpenAsync();
        using var tx = await con.BeginTransactionAsync();

        // El reordenamiento debe ser atomico porque impacta orden visual y
        // experiencia del panel Admin; no conviene persistir estados parciales.
        var ids = lista.Select(i => i.Id).ToArray();
        var filasEncontradas = await con.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM areas_publicacion WHERE id IN @ids",
            new { ids }, tx);

        if (filasEncontradas != lista.Count)
        {
            await tx.RollbackAsync();
            return filasEncontradas;
        }

        foreach (var (id, orden) in lista)
            await con.ExecuteAsync(
                @"UPDATE areas_publicacion
                  SET orden = @orden,
                      fecha_actualizacion = CURRENT_TIMESTAMP
                  WHERE id = @id",
                new { id, orden }, tx);

        var filasVerificadas = 0;
        foreach (var (id, orden) in lista)
            filasVerificadas += await con.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*)
                  FROM areas_publicacion
                  WHERE id = @id AND orden = @orden",
                new { id, orden }, tx);

        if (filasVerificadas != lista.Count)
        {
            await tx.RollbackAsync();
            return filasVerificadas;
        }

        await tx.CommitAsync();
        return filasVerificadas;
    }
}
