using Dapper;
using Intranet.Data;
using Intranet.Models;
using Intranet.Repositories.Interfaces;

namespace Intranet.Repositories;

public class ArchivoSeccionRepository : IArchivoSeccionRepository
{
    private readonly ConexionDb _db;

    public ArchivoSeccionRepository(ConexionDb db) => _db = db;

    public async Task<IEnumerable<ArchivoSeccion>> ObtenerPorSeccionAsync(
        string seccion, bool soloActivos = false)
    {
        using var con = _db.CrearConexion();
        var filtroActivo = soloActivos ? "AND activo = 1" : "";
        return await con.QueryAsync<ArchivoSeccion>(
            $"SELECT * FROM archivos_seccion WHERE seccion = @seccion {filtroActivo} ORDER BY orden ASC",
            new { seccion });
    }

    public async Task<IEnumerable<ArchivoSeccion>> ObtenerTodosAsync()
    {
        using var con = _db.CrearConexion();
        return await con.QueryAsync<ArchivoSeccion>(
            "SELECT * FROM archivos_seccion ORDER BY seccion ASC, orden ASC");
    }

    public async Task<ArchivoSeccion?> ObtenerPorIdAsync(int id)
    {
        using var con = _db.CrearConexion();
        return await con.QueryFirstOrDefaultAsync<ArchivoSeccion>(
            "SELECT * FROM archivos_seccion WHERE id = @id", new { id });
    }

    public async Task<int> InsertarAsync(ArchivoSeccion archivo)
    {
        using var con = _db.CrearConexion();
        return await con.ExecuteScalarAsync<int>(
            @"INSERT INTO archivos_seccion (seccion, nombre, descripcion, archivo_path, orden, activo)
              VALUES (@Seccion, @Nombre, @Descripcion, @ArchivoPath, @Orden, @Activo);
              SELECT LAST_INSERT_ID();",
            archivo);
    }

    public async Task ActualizarAsync(ArchivoSeccion archivo)
    {
        using var con = _db.CrearConexion();
        await con.ExecuteAsync(
            @"UPDATE archivos_seccion
              SET seccion = @Seccion, nombre = @Nombre, descripcion = @Descripcion,
                  archivo_path = @ArchivoPath, orden = @Orden, activo = @Activo
              WHERE id = @Id",
            archivo);
    }

    public async Task EliminarAsync(int id)
    {
        using var con = _db.CrearConexion();
        await con.ExecuteAsync("DELETE FROM archivos_seccion WHERE id = @id", new { id });
    }

    public async Task CambiarEstadoAsync(int id, bool activo)
    {
        using var con = _db.CrearConexion();
        await con.ExecuteAsync(
            "UPDATE archivos_seccion SET activo = @activo WHERE id = @id",
            new { id, activo });
    }
}
