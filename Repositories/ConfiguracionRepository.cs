using Dapper;
using Intranet.Data;
using Intranet.Models;
using Intranet.Repositories.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Intranet.Repositories;

public class ConfiguracionRepository : IConfiguracionRepository
{
    private readonly ConexionDb    _db;
    private readonly IMemoryCache  _cache;
    private const string CacheKey = "intranet_site_config";
    private const string ColumnasConfiguracionSitio = "clave, valor, tipo, descripcion";

    public ConfiguracionRepository(ConexionDb db, IMemoryCache cache)
    {
        _db    = db;
        _cache = cache;
    }

    public async Task<string?> ObtenerValorAsync(string clave)
    {
        var todos = await ObtenerTodosAsync();
        return todos.GetValueOrDefault(clave);
    }

    public async Task<Dictionary<string, string>> ObtenerTodosAsync()
    {
        if (_cache.TryGetValue(CacheKey, out Dictionary<string, string>? cached) && cached is not null)
            return cached;

        using var con = _db.CrearConexion();
        var lista = await con.QueryAsync<ConfiguracionSitio>(
            $"SELECT {ColumnasConfiguracionSitio} FROM configuracion_sitio");
        var dict  = lista.ToDictionary(c => c.Clave, c => c.Valor ?? string.Empty);

        _cache.Set(CacheKey, dict, TimeSpan.FromMinutes(5));
        return dict;
    }

    public async Task<IEnumerable<ConfiguracionSitio>> ObtenerConDetalleAsync()
    {
        using var con = _db.CrearConexion();
        return await con.QueryAsync<ConfiguracionSitio>(
            $"SELECT {ColumnasConfiguracionSitio} FROM configuracion_sitio ORDER BY clave ASC");
    }

    public async Task GuardarAsync(string clave, string valor)
    {
        using var con = _db.CrearConexion();
        await con.ExecuteAsync(
            @"INSERT INTO configuracion_sitio (clave, valor) VALUES (@clave, @valor)
              ON DUPLICATE KEY UPDATE valor = @valor",
            new { clave, valor });
        _cache.Remove(CacheKey);
    }

    public async Task GuardarMultiplesAsync(Dictionary<string, string> valores)
    {
        using var con = _db.CrearConexion();
        await con.OpenAsync();
        using var tx = await con.BeginTransactionAsync();
        foreach (var (clave, valor) in valores)
            await con.ExecuteAsync(
                @"INSERT INTO configuracion_sitio (clave, valor) VALUES (@clave, @valor)
                  ON DUPLICATE KEY UPDATE valor = @valor",
                new { clave, valor }, tx);
        await tx.CommitAsync();
        _cache.Remove(CacheKey);
    }
}
