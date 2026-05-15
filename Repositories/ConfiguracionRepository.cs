using Dapper;
using Intranet.Data;
using Intranet.Models;
using Intranet.Repositories.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Intranet.Repositories;

// Este repositorio concentra configuracion institucional y enlaces del sitio.
// El panel Admin lo usa para mantener textos, enlaces y ajustes visibles en la
// intranet. Los valores entrantes deben viajar como parametros de Dapper y las
// interpolaciones deben limitarse a columnas o fragmentos internos controlados.
public class ConfiguracionRepository : IConfiguracionRepository
{
    private readonly ConexionDb    _db;
    private readonly IMemoryCache  _cache;
    // Estas claves de cache impactan navegacion y textos visibles. Si cambia la
    // semantica de invalidez, revisar panel Admin, portada y enlaces publicos.
    private const string CacheKey = "intranet_site_config";
    private const string CacheKeyEnlaces = "intranet_site_links";
    private const string ColumnasConfiguracionSitio = "clave, valor, tipo, descripcion";
    private const string ColumnasSitioEnlaces = "id, grupo, texto, url, orden, activo";

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
        // La lectura pasa por cache para reducir consultas repetidas de valores
        // institucionales que se usan en multiples paginas por request.
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

    public async Task<IEnumerable<SitioEnlace>> ObtenerEnlacesAsync(
        string grupo,
        bool soloActivos = false)
    {
        // Estos enlaces afectan navegacion publica e institucional. El grupo y
        // su estado determinan que opciones ve el usuario en cada seccion.
        var cacheKey = $"{CacheKeyEnlaces}:{grupo}:{soloActivos}";
        if (_cache.TryGetValue(cacheKey, out IEnumerable<SitioEnlace>? cached) && cached is not null)
            return cached;

        using var con = _db.CrearConexion();
        var filtroActivo = soloActivos ? "AND activo = 1" : "";
        var enlaces = (await con.QueryAsync<SitioEnlace>(
            $@"SELECT {ColumnasSitioEnlaces}
               FROM sitio_enlaces
               WHERE grupo = @grupo {filtroActivo}
               ORDER BY orden ASC, texto ASC",
            new { grupo })).ToList();

        _cache.Set(cacheKey, enlaces, TimeSpan.FromMinutes(5));
        return enlaces;
    }

    public async Task GuardarAsync(string clave, string valor)
    {
        using var con = _db.CrearConexion();
        // ON DUPLICATE KEY UPDATE es sintaxis MySQL y centraliza la persistencia
        // de claves institucionales sin exponer logica de upsert al PageModel.
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
        // La transaccion evita dejar configuracion parcial cuando el Admin guarda
        // varias claves institucionales en una sola operacion.
        foreach (var (clave, valor) in valores)
            await con.ExecuteAsync(
                @"INSERT INTO configuracion_sitio (clave, valor) VALUES (@clave, @valor)
                  ON DUPLICATE KEY UPDATE valor = @valor",
                new { clave, valor }, tx);
        await tx.CommitAsync();
        _cache.Remove(CacheKey);
    }

    public async Task GuardarEnlacesAsync(string grupo, IEnumerable<SitioEnlace> enlaces)
    {
        using var con = _db.CrearConexion();
        await con.OpenAsync();
        using var tx = await con.BeginTransactionAsync();

        // Insert y update comparten una misma transaccion porque el orden y el
        // estado de enlaces impactan directamente navegacion y consistencia visual.
        foreach (var enlace in enlaces)
        {
            if (enlace.Id > 0)
            {
                await con.ExecuteAsync(
                    @"UPDATE sitio_enlaces
                      SET texto = @Texto,
                          url = @Url,
                          orden = @Orden,
                          activo = @Activo
                      WHERE id = @Id AND grupo = @Grupo",
                    new
                    {
                        enlace.Id,
                        Grupo = grupo,
                        enlace.Texto,
                        enlace.Url,
                        enlace.Orden,
                        enlace.Activo
                    },
                    tx);
            }
            else
            {
                await con.ExecuteAsync(
                    @"INSERT INTO sitio_enlaces (grupo, texto, url, orden, activo)
                      VALUES (@Grupo, @Texto, @Url, @Orden, @Activo)",
                    new
                    {
                        Grupo = grupo,
                        enlace.Texto,
                        enlace.Url,
                        enlace.Orden,
                        enlace.Activo
                    },
                    tx);
            }
        }

        await tx.CommitAsync();
        _cache.Remove($"{CacheKeyEnlaces}:{grupo}:True");
        _cache.Remove($"{CacheKeyEnlaces}:{grupo}:False");
    }
}
