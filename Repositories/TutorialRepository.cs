using Dapper;
using Intranet.Data;
using Intranet.Models;
using Intranet.Repositories.Interfaces;

namespace Intranet.Repositories;

public class TutorialRepository : ITutorialRepository
{
    private readonly ConexionDb _db;

    public TutorialRepository(ConexionDb db) => _db = db;

    public async Task<IEnumerable<Tutorial>> ObtenerTodosAsync(bool soloActivos = false)
    {
        using var con = _db.CrearConexion();
        var filtro = soloActivos ? "WHERE activo = 1" : "";
        return await con.QueryAsync<Tutorial>(
            $"SELECT * FROM tutoriales {filtro} ORDER BY orden ASC, fecha_creacion DESC");
    }

    public async Task<Tutorial?> ObtenerPorIdAsync(int id)
    {
        using var con = _db.CrearConexion();
        return await con.QueryFirstOrDefaultAsync<Tutorial>(
            "SELECT * FROM tutoriales WHERE id = @id", new { id });
    }

    public async Task<int> InsertarAsync(Tutorial tutorial)
    {
        using var con = _db.CrearConexion();
        return await con.ExecuteScalarAsync<int>(
            @"INSERT INTO tutoriales (titulo, descripcion, archivo_path, miniatura_path, orden, activo)
              VALUES (@Titulo, @Descripcion, @ArchivoPath, @MiniaturaPath, @Orden, @Activo);
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
                  orden = @Orden, activo = @Activo
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

    public async Task CambiarEstadoAsync(int id, bool activo)
    {
        using var con = _db.CrearConexion();
        await con.ExecuteAsync(
            "UPDATE tutoriales SET activo = @activo WHERE id = @id",
            new { id, activo });
    }
}
