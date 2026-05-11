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
        _cadenaConexion = configuracion.GetConnectionString("MySQL")
            ?? throw new InvalidOperationException(
                "La cadena de conexión 'MySQL' no está configurada en appsettings.json.");
    }

    /// <summary>
    /// Crea y devuelve una nueva conexión MySQL cerrada.
    /// Dapper la abre antes de cada operación y la cierra al finalizar.
    /// </summary>
    public MySqlConnection CrearConexion() => new(_cadenaConexion);
}
