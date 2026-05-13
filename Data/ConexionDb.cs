using MySqlConnector;

namespace Intranet.Data;

/// <summary>
/// Fábrica de conexiones MySQL. Se registra como Singleton para
/// reutilizar la cadena de conexión sin mantener conexiones abiertas.
/// Dapper abre y cierra la conexión automáticamente por operación.
/// </summary>
public class ConexionDb
{
    private readonly string _cadenaConexion;

    public ConexionDb(IConfiguration configuracion)
    {
        // La cadena real puede venir de la variable ConnectionStrings__MySQL.
        // No guardar usuarios ni passwords reales en appsettings versionados.
        var cadenaConexion = configuracion.GetConnectionString("MySQL")
            ?? throw new InvalidOperationException(
                "La cadena de conexión 'MySQL' no está configurada en appsettings.json.");

        // AllowUserVariables permite scripts MySQL con variables @locales. Esta
        // clase no mantiene conexiones abiertas; solo prepara la cadena que cada
        // repositorio usara para crear su propia conexion por operacion.
        var builder = new MySqlConnectionStringBuilder(cadenaConexion)
        {
            AllowUserVariables = true
        };

        _cadenaConexion = builder.ConnectionString;
    }

    /// <summary>
    /// Crea y devuelve una nueva conexión MySQL cerrada.
    /// Dapper la abre antes de cada operación y la cierra al finalizar.
    /// </summary>
    public MySqlConnection CrearConexion() => new(_cadenaConexion);
}
