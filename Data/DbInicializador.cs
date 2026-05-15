using Dapper;
using Intranet.Services.Interfaces;
using System.Data;

namespace Intranet.Data;

/// <summary>
/// Siembra el usuario administrador inicial si la tabla está vacía.
/// Se ejecuta una sola vez al arranque de la aplicación.
/// El hash de contraseña se genera en tiempo de ejecución para evitar
/// valores hardcodeados en el script SQL.
/// </summary>
public class DbInicializador
{
    private readonly ConexionDb    _db;
    private readonly IAuthService  _auth;
    private readonly IWebHostEnvironment _entorno;
    private readonly ILogger<DbInicializador> _log;

    public DbInicializador(
        ConexionDb db,
        IAuthService auth,
        IWebHostEnvironment entorno,
        ILogger<DbInicializador> log)
    {
        _db      = db;
        _auth    = auth;
        _entorno = entorno;
        _log     = log;
    }

    public async Task InicializarAsync()
    {
        // Este inicializador corre antes de que la app atienda trafico. Su
        // objetivo es dejar la BD en un estado minimo consistente para que las
        // Razor Pages y los endpoints no operen contra una estructura incompleta.
        using var con = _db.CrearConexion();

        try
        {
            await con.OpenAsync();
            // Compatibilidad con instalaciones antiguas: esta rutina solo toca
            // columnas puntuales si la tabla ya existe. En una BD vacia no crea
            // nada; las migraciones versionadas son la fuente de verdad.
            await AsegurarEstructuraAsync(con);

            // Las migraciones se aplican antes de cualquier seed funcional.
            // schema_migrations manda: si una version ya esta registrada, el
            // script no se reejecuta aunque sea idempotente. Esto protege futuras
            // migraciones que no puedan repetirse sin riesgo.
            await AplicarMigracionesAsync(con);
        }
        catch (Exception ex)
        {
            // Un error aqui es bloqueante para despliegue: si la dependencia de
            // datos no abre o las migraciones fallan, el host aborta el arranque.
            _log.LogError(ex,
                "Error critico al preparar la estructura de base de datos. " +
                "El arranque se detendra para evitar operar con una BD inconsistente.");
            throw;
        }

        try
        {
            var totalUsuarios = await con.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM usuarios_admin WHERE activo = 1");

            if (totalUsuarios == 0)
            {
                // No hay password fijo en codigo. Para una BD recien creada, el
                // primer admin solo se genera si el operador definio esta variable
                // de entorno en la sesion local o en el ambiente de despliegue.
                var passwordInicial = Environment.GetEnvironmentVariable("INTRANET_ADMIN_INITIAL_PASSWORD");
                if (string.IsNullOrWhiteSpace(passwordInicial))
                {
                    _log.LogWarning(
                        "No se creo el usuario admin inicial porque falta la variable de entorno INTRANET_ADMIN_INITIAL_PASSWORD.");
                    return;
                }

                var hash = _auth.HashearPassword(passwordInicial);
                await con.ExecuteAsync(
                    @"INSERT INTO usuarios_admin (usuario, password_hash, nombre_completo, activo)
                      VALUES (@usuario, @hash, @nombre, 1)",
                    new { usuario = "admin", hash, nombre = "Administrador DGIE" });

                _log.LogInformation("Usuario admin inicial sembrado correctamente.");
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex,
                "Error al sembrar o validar el usuario administrador inicial. " +
                "El arranque se detendra porque el panel podria quedar inaccesible.");
            throw;
        }
    }

    private async Task AplicarMigracionesAsync(IDbConnection con)
    {
        // El orden alfabetico por prefijo 001, 002, ... define el historial.
        // No insertar migraciones "entre medias" cuando ya existen ambientes con
        // versiones aplicadas; crea una nueva version siguiente.
        // Para despliegue, verificar que Data/Scripts/Migrations viaje junto con
        // la publicacion. Si falta esa carpeta, el arranque debe fallar.
        var carpetaMigraciones = Path.Combine(
            _entorno.ContentRootPath,
            "Data",
            "Scripts",
            "Migrations");

        if (!Directory.Exists(carpetaMigraciones))
            throw new DirectoryNotFoundException(
                $"No se encontro la carpeta de migraciones: {carpetaMigraciones}");

        var scripts = Directory
            .EnumerateFiles(carpetaMigraciones, "*.sql", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var migracionesRegistradas = await ObtenerMigracionesRegistradasAsync(con);

        foreach (var script in scripts)
        {
            var version = ObtenerVersionMigracion(script);
            var nombre = Path.GetFileNameWithoutExtension(script);

            if (migracionesRegistradas.Contains(version))
            {
                _log.LogInformation(
                    "Migracion omitida porque ya esta registrada: {Migracion}",
                    Path.GetFileName(script));
                continue;
            }

            try
            {
                var contenido = await File.ReadAllTextAsync(script);
                // Los scripts versionados usan sentencias simples separadas por ;
                // Evitar procedimientos complejos con delimitadores personalizados
                // sin ajustar antes este separador.
                foreach (var sentencia in SepararSentenciasSql(contenido))
                    await con.ExecuteAsync(sentencia);

                await RegistrarMigracionAsync(con, version, nombre);
                migracionesRegistradas.Add(version);

                _log.LogInformation(
                    "Migracion aplicada: {Migracion}",
                    Path.GetFileName(script));
            }
            catch (Exception ex)
            {
                _log.LogError(ex,
                    "Error al aplicar la migracion {Migracion}.",
                    Path.GetFileName(script));
                throw;
            }
        }
    }

    private static async Task<HashSet<string>> ObtenerMigracionesRegistradasAsync(IDbConnection con)
    {
        // En una BD completamente vacia, schema_migrations todavia no existe.
        // La migracion 001 debe crearla; por eso aqui se devuelve conjunto vacio
        // y el runner arranca desde el primer script versionado.
        var existeTablaMigraciones = await con.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*)
              FROM INFORMATION_SCHEMA.TABLES
              WHERE TABLE_SCHEMA = DATABASE()
                AND TABLE_NAME = 'schema_migrations'");

        if (existeTablaMigraciones == 0)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var versiones = await con.QueryAsync<string>(
            "SELECT version FROM schema_migrations");

        return new HashSet<string>(versiones, StringComparer.OrdinalIgnoreCase);
    }

    private static async Task RegistrarMigracionAsync(
        IDbConnection con,
        string version,
        string nombre)
    {
        await con.ExecuteAsync(
            @"INSERT IGNORE INTO schema_migrations (version, name)
              VALUES (@version, @nombre)",
            new { version, nombre });
    }

    private static string ObtenerVersionMigracion(string script)
    {
        var nombre = Path.GetFileNameWithoutExtension(script);
        var separador = nombre.IndexOf('_', StringComparison.Ordinal);

        if (separador <= 0)
            throw new InvalidOperationException(
                $"La migracion no tiene prefijo de version: {Path.GetFileName(script)}");

        return nombre[..separador];
    }

    private static IEnumerable<string> SepararSentenciasSql(string contenido)
    {
        using var lector = new StringReader(contenido);
        var lineas = new List<string>();

        while (lector.ReadLine() is { } linea)
        {
            if (linea.TrimStart().StartsWith("--", StringComparison.Ordinal))
                continue;

            lineas.Add(linea);
        }

        return string.Join(Environment.NewLine, lineas)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => !string.IsNullOrWhiteSpace(s));
    }

    private static async Task AsegurarEstructuraAsync(IDbConnection con)
    {
        // Parche defensivo para bases legadas previas a las migraciones actuales.
        // No reemplaza a los scripts SQL ni debe crecer como sistema paralelo de
        // migraciones; cualquier cambio nuevo debe ir en Data/Scripts/Migrations.
        var existeAccesosRapidos = await con.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*)
              FROM INFORMATION_SCHEMA.TABLES
              WHERE TABLE_SCHEMA = DATABASE()
                AND TABLE_NAME = 'accesos_rapidos'");

        if (existeAccesosRapidos == 0)
            return;

        var existeBannerPath = await con.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*)
              FROM INFORMATION_SCHEMA.COLUMNS
              WHERE TABLE_SCHEMA = DATABASE()
                AND TABLE_NAME = 'accesos_rapidos'
                AND COLUMN_NAME = 'banner_path'");

        if (existeBannerPath == 0)
        {
            await con.ExecuteAsync(
                @"ALTER TABLE accesos_rapidos
                  ADD COLUMN banner_path VARCHAR(500) NULL
                  COMMENT 'Imagen panoramica desde Storage/'
                  AFTER icono_path");
        }
    }
}
