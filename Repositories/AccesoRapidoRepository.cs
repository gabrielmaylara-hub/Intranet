using Dapper;
using Intranet.Data;
using Intranet.Models;
using Intranet.Repositories.Interfaces;

namespace Intranet.Repositories;

public class AccesoRapidoRepository : IAccesoRapidoRepository
{
    private readonly ConexionDb _db;

    public AccesoRapidoRepository(ConexionDb db) => _db = db;

    public async Task<IEnumerable<AccesoRapido>> ObtenerTodosAsync(bool soloActivos = false)
    {
        using var con = _db.CrearConexion();
        var filtro = soloActivos ? "WHERE activo = 1" : "";
        return await con.QueryAsync<AccesoRapido>(
            $"SELECT * FROM accesos_rapidos {filtro} ORDER BY orden ASC");
    }

    public async Task<AccesoRapido?> ObtenerPorIdAsync(int id)
    {
        using var con = _db.CrearConexion();
        return await con.QueryFirstOrDefaultAsync<AccesoRapido>(
            "SELECT * FROM accesos_rapidos WHERE id = @id", new { id });
    }

    public async Task<int> InsertarAsync(AccesoRapido acceso)
    {
        using var con = _db.CrearConexion();
        return await con.ExecuteScalarAsync<int>(
            @"INSERT INTO accesos_rapidos (nombre, url, icono_path, banner_path, orden, abre_nueva_ventana, activo)
              VALUES (@Nombre, @Url, @IconoPath, @BannerPath, @Orden, @AbreNuevaVentana, @Activo);
              SELECT LAST_INSERT_ID();",
            acceso);
    }

    public async Task ActualizarAsync(AccesoRapido acceso)
    {
        using var con = _db.CrearConexion();
        await con.ExecuteAsync(
            @"UPDATE accesos_rapidos
              SET nombre = @Nombre, url = @Url, icono_path = @IconoPath, banner_path = @BannerPath,
                  orden = @Orden, abre_nueva_ventana = @AbreNuevaVentana, activo = @Activo
              WHERE id = @Id",
            acceso);
    }

    public async Task EliminarAsync(int id)
    {
        using var con = _db.CrearConexion();
        await con.ExecuteAsync("DELETE FROM accesos_rapidos WHERE id = @id", new { id });
    }

    public async Task CambiarEstadoAsync(int id, bool activo)
    {
        using var con = _db.CrearConexion();
        await con.ExecuteAsync(
            "UPDATE accesos_rapidos SET activo = @activo WHERE id = @id",
            new { id, activo });
    }

    public async Task ActualizarOrdenAsync(IEnumerable<(int Id, int Orden)> items)
    {
        using var con = _db.CrearConexion();
        await con.OpenAsync();
        using var tx = await con.BeginTransactionAsync();
        foreach (var (id, orden) in items)
            await con.ExecuteAsync(
                "UPDATE accesos_rapidos SET orden = @orden WHERE id = @id",
                new { id, orden }, tx);
        await tx.CommitAsync();
    }
}
