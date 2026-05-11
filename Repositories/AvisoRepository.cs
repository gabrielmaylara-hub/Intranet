using Dapper;
using Intranet.Data;
using Intranet.Models;
using Intranet.Repositories.Interfaces;

namespace Intranet.Repositories;

public class AvisoRepository : IAvisoRepository
{
    private readonly ConexionDb _db;

    public AvisoRepository(ConexionDb db) => _db = db;

    public async Task<IEnumerable<Aviso>> ObtenerTodosAsync(bool soloActivos = false)
    {
        using var con = _db.CrearConexion();
        var filtro = soloActivos ? "WHERE activo = 1" : "";
        return await con.QueryAsync<Aviso>(
            $"SELECT * FROM avisos {filtro} ORDER BY orden ASC, fecha_publicacion DESC");
    }

    public async Task<Aviso?> ObtenerPorIdAsync(int id)
    {
        using var con = _db.CrearConexion();
        return await con.QueryFirstOrDefaultAsync<Aviso>(
            "SELECT * FROM avisos WHERE id = @id", new { id });
    }

    public async Task<int> InsertarAsync(Aviso aviso)
    {
        using var con = _db.CrearConexion();
        return await con.ExecuteScalarAsync<int>(
            @"INSERT INTO avisos (titulo, contenido, fecha_publicacion, activo, orden)
              VALUES (@Titulo, @Contenido, @FechaPublicacion, @Activo, @Orden);
              SELECT LAST_INSERT_ID();",
            aviso);
    }

    public async Task ActualizarAsync(Aviso aviso)
    {
        using var con = _db.CrearConexion();
        await con.ExecuteAsync(
            @"UPDATE avisos
              SET titulo = @Titulo, contenido = @Contenido,
                  fecha_publicacion = @FechaPublicacion, activo = @Activo, orden = @Orden
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
            "UPDATE avisos SET activo = @activo WHERE id = @id",
            new { id, activo });
    }
}
