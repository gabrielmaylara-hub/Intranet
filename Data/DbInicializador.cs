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
    private readonly ILogger<DbInicializador> _log;

    public DbInicializador(ConexionDb db, IAuthService auth, ILogger<DbInicializador> log)
    {
        _db   = db;
        _auth = auth;
        _log  = log;
    }

    public async Task InicializarAsync()
    {
        try
        {
            using var con = _db.CrearConexion();

            await AsegurarEstructuraAsync(con);

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
            // Solo registra el error; la aplicación arranca aunque falle el seed
            _log.LogError(ex, "Error al inicializar la base de datos. " +
                "Verifique la cadena de conexión y que las tablas existan (ejecute init.sql).");
        }
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
