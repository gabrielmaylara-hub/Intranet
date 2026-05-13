using Dapper;
using Intranet.Data;

namespace Intranet.Pages.Admin;

public class IndexModel : AdminPageModel
{
    private readonly ConexionDb _db;

    public IndexModel(ConexionDb db) => _db = db;

    public int TotalAccesos { get; private set; }
    public int TotalAvisos { get; private set; }
    public int TotalTutoriales { get; private set; }
    public int TotalArchivos { get; private set; }
    public bool EsAdminGeneralActual => EsAdminGeneral();

    public async Task OnGetAsync()
    {
        using var con = _db.CrearConexion();

        // admin_general ve las metricas globales. usuario_area solo ve datos de
        // los modulos que ya tienen permisos por area: Avisos y Tutoriales.
        if (EsAdminGeneral())
        {
            TotalAccesos = await con.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM accesos_rapidos WHERE activo = 1");
            TotalAvisos = await con.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM avisos WHERE activo = 1");
            TotalTutoriales = await con.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM tutoriales WHERE activo = 1");
            TotalArchivos = await con.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM archivos_seccion WHERE activo = 1");
            return;
        }

        var areaId = ObtenerAreaPublicacionId();
        if (areaId is null or <= 0)
            return;

        TotalAvisos = await con.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM avisos WHERE activo = 1 AND area_publicacion_id = @areaId",
            new { areaId });
        TotalTutoriales = await con.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM tutoriales WHERE activo = 1 AND area_publicacion_id = @areaId",
            new { areaId });
    }
}
