using MySqlConnector;

namespace Intranet.Data;

/// <summary>
/// Fabrica de conexiones MySQL. Se registra como Singleton para
/// reutilizar la cadena de conexion sin mantener conexiones abiertas.
/// Esta clase solo construye conexiones nuevas y cerradas para que los
/// consumidores las administren correctamente por operacion.
/// </summary>
public class ConexionDb
{
    private readonly string _cadenaConexion;

    public ConexionDb(IConfiguration configuracion)
    {
        // Toda la app pasa por esta clase para crear conexiones. Eso simplifica
        // despliegue y soporte: si cambia la procedencia de la cadena, el punto
        // de ajuste queda centralizado aqui y no repartido por repositorios.
        // La cadena real puede venir de la variable ConnectionStrings__MySQL.
        // No guardar usuarios ni contrasenas reales en appsettings versionados.
        var cadenaConexion = configuracion.GetConnectionString("MySQL")
            ?? throw new InvalidOperationException(
                "La cadena de conexion 'MySQL' no esta configurada en appsettings.json.");

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
    /// Crea y devuelve una nueva conexion MySQL cerrada.
    /// ConexionDb no mantiene conexiones abiertas ni comparte instancias activas.
    /// </summary>
    // El consumidor o repositorio debe abrirla y liberarla correctamente,
    // normalmente con using o await using. Si la BD esta caida o la cadena es
    // invalida, el fallo aparecera al abrir la conexion en el consumidor.
    public MySqlConnection CrearConexion() => new(_cadenaConexion);
}
