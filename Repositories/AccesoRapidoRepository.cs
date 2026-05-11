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

    public async Task<int> ActualizarAsync(AccesoRapido acceso)
    {
        using var con = _db.CrearConexion();
        await con.OpenAsync();
        using var tx = await con.BeginTransactionAsync();

        await con.ExecuteAsync(
            @"UPDATE accesos_rapidos
              SET nombre = @Nombre, url = @Url, icono_path = @IconoPath, banner_path = @BannerPath,
                  orden = @Orden, abre_nueva_ventana = @AbreNuevaVentana, activo = @Activo
              WHERE id = @Id",
            acceso,
            tx);

        var filasVerificadas = await con.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM accesos_rapidos WHERE id = @Id",
            acceso,
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
        return await con.ExecuteAsync("DELETE FROM accesos_rapidos WHERE id = @id", new { id });
    }

    public async Task CambiarEstadoAsync(int id, bool activo)
    {
        using var con = _db.CrearConexion();
        await con.ExecuteAsync(
            "UPDATE accesos_rapidos SET activo = @activo WHERE id = @id",
            new { id, activo });
    }

    public async Task<int> ActualizarOrdenAsync(IEnumerable<(int Id, int Orden)> items)
    {
        var lista = items.ToList();

        using var con = _db.CrearConexion();
        await con.OpenAsync();
        using var tx = await con.BeginTransactionAsync();

        var ids = lista.Select(i => i.Id).ToArray();
        var filasEncontradas = await con.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM accesos_rapidos WHERE id IN @ids",
            new { ids }, tx);

        if (filasEncontradas != lista.Count)
        {
            await tx.RollbackAsync();
            return filasEncontradas;
        }

        foreach (var (id, orden) in lista)
            await con.ExecuteAsync(
                "UPDATE accesos_rapidos SET orden = @orden WHERE id = @id",
                new { id, orden }, tx);

        var filasVerificadas = 0;
        foreach (var (id, orden) in lista)
            filasVerificadas += await con.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*)
                  FROM accesos_rapidos
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
