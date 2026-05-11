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
        using var con = _db.CrearConexion();

        try
        {
            await con.OpenAsync();
            await AsegurarEstructuraAsync(con);
            await AplicarMigracionesAsync(con);
        }
        catch (Exception ex)
        {
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
                var hash = _auth.HashearPassword("Fget2025*");
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

        foreach (var script in scripts)
        {
            try
            {
                var contenido = await File.ReadAllTextAsync(script);
                foreach (var sentencia in SepararSentenciasSql(contenido))
                    await con.ExecuteAsync(sentencia);

                _log.LogInformation(
                    "Migracion aplicada/verificada: {Migracion}",
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
