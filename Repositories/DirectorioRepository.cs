using Dapper;
using Intranet.Data;
using Intranet.Models;
using Intranet.Repositories.Interfaces;

namespace Intranet.Repositories;

public class DirectorioRepository : IDirectorioRepository
{
    private readonly ConexionDb _db;
    private const string ColumnasDirectorio =
        "id, area, nombre, extension, orden, activo";
    private const string ColumnasAreas =
        "id, nombre, titular, ubicacion, correo, orden, activo";

    public DirectorioRepository(ConexionDb db) => _db = db;

    public async Task<IEnumerable<DirectorioEntrada>> ObtenerTodosAsync(bool soloActivos = false)
    {
        using var con = _db.CrearConexion();
        var filtro = soloActivos ? "WHERE d.activo = 1" : "";
        return await con.QueryAsync<DirectorioEntrada>(
            $@"SELECT d.id, d.area, d.nombre, d.extension, d.orden, d.activo
               FROM directorio d
               LEFT JOIN directorio_areas a ON a.nombre = d.area
               {filtro}
               ORDER BY COALESCE(a.orden, 999999) ASC, d.area ASC, d.orden ASC, d.nombre ASC");
    }

    public async Task<DirectorioEntrada?> ObtenerPorIdAsync(int id)
    {
        using var con = _db.CrearConexion();
        return await con.QueryFirstOrDefaultAsync<DirectorioEntrada>(
            $"SELECT {ColumnasDirectorio} FROM directorio WHERE id = @id", new { id });
    }

    public async Task<int> InsertarAsync(DirectorioEntrada entrada)
    {
        using var con = _db.CrearConexion();
        await AsegurarAreaInternaAsync(con, entrada.Area);

        return await con.ExecuteScalarAsync<int>(
            @"INSERT INTO directorio (area, nombre, extension, orden, activo)
              VALUES (@Area, @Nombre, @Extension, @Orden, @Activo);
              SELECT LAST_INSERT_ID();",
            entrada);
    }

    public async Task ActualizarAsync(DirectorioEntrada entrada)
    {
        using var con = _db.CrearConexion();
        await AsegurarAreaInternaAsync(con, entrada.Area);

        await con.ExecuteAsync(
            @"UPDATE directorio
              SET area = @Area, nombre = @Nombre, extension = @Extension,
                  orden = @Orden, activo = @Activo
              WHERE id = @Id",
            entrada);
    }

    public async Task EliminarAsync(int id)
    {
        using var con = _db.CrearConexion();
        await con.ExecuteAsync("DELETE FROM directorio WHERE id = @id", new { id });
    }

    public async Task CambiarEstadoAsync(int id, bool activo)
    {
        using var con = _db.CrearConexion();
        await con.ExecuteAsync(
            "UPDATE directorio SET activo = @activo WHERE id = @id",
            new { id, activo });
    }

    public async Task<IEnumerable<DirectorioArea>> ObtenerAreasAsync(bool soloActivas = false)
    {
        using var con = _db.CrearConexion();
        var filtro = soloActivas ? "WHERE activo = 1" : "";
        return await con.QueryAsync<DirectorioArea>(
            $"SELECT {ColumnasAreas} FROM directorio_areas {filtro} ORDER BY orden ASC, nombre ASC");
    }

    public async Task<DirectorioArea?> ObtenerAreaPorIdAsync(int id)
    {
        using var con = _db.CrearConexion();
        return await con.QueryFirstOrDefaultAsync<DirectorioArea>(
            $"SELECT {ColumnasAreas} FROM directorio_areas WHERE id = @id",
            new { id });
    }

    public async Task<DirectorioArea?> ObtenerAreaPorNombreAsync(string nombre)
    {
        using var con = _db.CrearConexion();
        return await con.QueryFirstOrDefaultAsync<DirectorioArea>(
            $"SELECT {ColumnasAreas} FROM directorio_areas WHERE nombre = @nombre",
            new { nombre });
    }

    public async Task<int> InsertarAreaAsync(DirectorioArea area)
    {
        using var con = _db.CrearConexion();
        return await con.ExecuteScalarAsync<int>(
            @"INSERT INTO directorio_areas
                  (nombre, titular, ubicacion, correo, orden, activo)
              VALUES
                  (@Nombre, NULLIF(@Titular, ''), NULLIF(@Ubicacion, ''),
                   NULLIF(@Correo, ''), @Orden, @Activo);
              SELECT LAST_INSERT_ID();",
            area);
    }

    public async Task ActualizarAreaAsync(DirectorioArea area)
    {
        using var con = _db.CrearConexion();
        await con.ExecuteAsync(
            @"UPDATE directorio_areas
              SET titular = NULLIF(@Titular, ''),
                  ubicacion = NULLIF(@Ubicacion, ''),
                  correo = NULLIF(@Correo, ''),
                  orden = @Orden,
                  activo = @Activo
              WHERE id = @Id",
            area);
    }

    public async Task AsegurarAreaAsync(string nombre)
    {
        using var con = _db.CrearConexion();
        await AsegurarAreaInternaAsync(con, nombre);
    }

    public async Task ActualizarAreaDesdeImportacionAsync(
        string nombre,
        string? titular,
        string? ubicacion,
        string? correo)
    {
        using var con = _db.CrearConexion();
        await con.ExecuteAsync(
            @"UPDATE directorio_areas
              SET titular = CASE WHEN NULLIF(@titular, '') IS NULL THEN titular ELSE @titular END,
                  ubicacion = CASE WHEN NULLIF(@ubicacion, '') IS NULL THEN ubicacion ELSE @ubicacion END,
                  correo = CASE WHEN NULLIF(@correo, '') IS NULL THEN correo ELSE @correo END
              WHERE nombre = @nombre",
            new
            {
                nombre,
                titular = titular?.Trim(),
                ubicacion = ubicacion?.Trim(),
                correo = correo?.Trim()
            });
    }

    private static async Task AsegurarAreaInternaAsync(
        System.Data.IDbConnection con,
        string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return;

        await con.ExecuteAsync(
            @"INSERT INTO directorio_areas (nombre, orden, activo)
              SELECT @nombre, COALESCE((SELECT MAX(a.orden) + 1 FROM directorio_areas a), 1), 1
              WHERE NOT EXISTS (
                  SELECT 1 FROM directorio_areas WHERE nombre = @nombre
              )",
            new { nombre = nombre.Trim() });
    }
}
