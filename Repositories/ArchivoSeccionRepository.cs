using Dapper;
using Intranet.Data;
using Intranet.Models;
using Intranet.Repositories.Interfaces;

namespace Intranet.Repositories;

// Este repositorio agrupa el SQL del modulo de archivos por seccion. La UI solo
// debe pedir operaciones a este repositorio y dejar la creacion de conexiones a
// ConexionDb. Los valores de entrada se envian como parametros de Dapper.
// Las interpolaciones del SQL deben limitarse a columnas o filtros internos,
// nunca a texto capturado directamente del usuario.
public class ArchivoSeccionRepository : IArchivoSeccionRepository
{
    private readonly ConexionDb _db;
    // archivo_path guarda una ruta relativa de Storage; no es una ruta fisica.
    // Esa distincion es importante para descargas y para no acoplar la BD al host.
    private const string ColumnasArchivoSeccion =
        "id, seccion, nombre, descripcion, archivo_path, orden, activo";

    public ArchivoSeccionRepository(ConexionDb db) => _db = db;

    public async Task<IEnumerable<ArchivoSeccion>> ObtenerPorSeccionAsync(
        string seccion, bool soloActivos = false)
    {
        // Este metodo alimenta vistas publicas por seccion y pantallas del Admin.
        // soloActivos protege la salida publica sin alterar el historial interno.
        using var con = _db.CrearConexion();
        var filtroActivo = soloActivos ? "AND activo = 1" : "";
        return await con.QueryAsync<ArchivoSeccion>(
            $"SELECT {ColumnasArchivoSeccion} FROM archivos_seccion WHERE seccion = @seccion {filtroActivo} ORDER BY orden ASC",
            new { seccion });
    }

    public async Task<IEnumerable<ArchivoSeccion>> ObtenerTodosAsync()
    {
        using var con = _db.CrearConexion();
        return await con.QueryAsync<ArchivoSeccion>(
            $"SELECT {ColumnasArchivoSeccion} FROM archivos_seccion ORDER BY seccion ASC, orden ASC");
    }

    public async Task<ArchivoSeccion?> ObtenerPorIdAsync(int id)
    {
        // Se usa en edicion y en endpoints de descarga para resolver la metadata
        // que luego apuntara al archivo real en Storage.
        using var con = _db.CrearConexion();
        return await con.QueryFirstOrDefaultAsync<ArchivoSeccion>(
            $"SELECT {ColumnasArchivoSeccion} FROM archivos_seccion WHERE id = @id", new { id });
    }

    public async Task<int> InsertarAsync(ArchivoSeccion archivo)
    {
        using var con = _db.CrearConexion();
        // LAST_INSERT_ID() es sintaxis MySQL. Si cambia el proveedor, esta parte
        // requiere revision explicita aunque el contrato del metodo no cambie.
        return await con.ExecuteScalarAsync<int>(
            @"INSERT INTO archivos_seccion (seccion, nombre, descripcion, archivo_path, orden, activo)
              VALUES (@Seccion, @Nombre, @Descripcion, @ArchivoPath, @Orden, @Activo);
              SELECT LAST_INSERT_ID();",
            archivo);
    }

    public async Task<int> ActualizarAsync(ArchivoSeccion archivo)
    {
        using var con = _db.CrearConexion();
        await con.OpenAsync();
        using var tx = await con.BeginTransactionAsync();

        // Esta actualizacion usa transaccion porque el panel Admin espera una
        // confirmacion coherente de persistencia antes de conservar o limpiar
        // archivos en Storage fuera de la base de datos.
        await con.ExecuteAsync(
            @"UPDATE archivos_seccion
              SET seccion = @Seccion, nombre = @Nombre, descripcion = @Descripcion,
                  archivo_path = @ArchivoPath, orden = @Orden, activo = @Activo
              WHERE id = @Id",
            archivo,
            tx);

        var filasVerificadas = await con.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM archivos_seccion WHERE id = @Id",
            archivo,
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
        return await con.ExecuteAsync("DELETE FROM archivos_seccion WHERE id = @id", new { id });
    }

    public async Task CambiarEstadoAsync(int id, bool activo)
    {
        // Activo/inactivo controla visibilidad publica; la ruta relativa del
        // archivo sigue en BD aunque el registro deje de mostrarse al publico.
        using var con = _db.CrearConexion();
        await con.ExecuteAsync(
            "UPDATE archivos_seccion SET activo = @activo WHERE id = @id",
            new { id, activo });
    }
}
