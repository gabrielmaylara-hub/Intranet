using Dapper;
using Intranet.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Intranet.Pages.Admin;

public class IndexModel : PageModel
{
    private readonly ConexionDb _db;

    public IndexModel(ConexionDb db) => _db = db;

    public int TotalAccesos    { get; private set; }
    public int TotalAvisos     { get; private set; }
    public int TotalTutoriales { get; private set; }
    public int TotalArchivos   { get; private set; }

    public async Task OnGetAsync()
    {
        using var con = _db.CrearConexion();

        // Consultas de conteo para el dashboard — directas sin pasar por repositorios
        TotalAccesos    = await con.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM accesos_rapidos  WHERE activo = 1");
        TotalAvisos     = await con.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM avisos           WHERE activo = 1");
        TotalTutoriales = await con.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM tutoriales       WHERE activo = 1");
        TotalArchivos   = await con.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM archivos_seccion WHERE activo = 1");
    }
}
