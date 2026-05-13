using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Intranet.Pages.Admin;

public abstract class AdminPageModel : PageModel
{
    protected virtual bool RequiereAdminGeneral => false;

    public override async Task OnPageHandlerExecutionAsync(
        PageHandlerExecutingContext context,
        PageHandlerExecutionDelegate next)
    {
        if (RequiereAdminGeneral && !EsAdminGeneral())
        {
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
            return;
        }

        await next();
    }

    protected bool EsAdminGeneral() =>
        string.Equals(
            User.FindFirst("rol")?.Value,
            "admin_general",
            StringComparison.OrdinalIgnoreCase);

    protected bool EsUsuarioArea() =>
        string.Equals(
            User.FindFirst("rol")?.Value,
            "usuario_area",
            StringComparison.OrdinalIgnoreCase);

    protected int? ObtenerAreaPublicacionId()
    {
        var idTexto = User.FindFirst("area_publicacion_id")?.Value;
        return int.TryParse(idTexto, out var id) ? id : null;
    }

    protected int? ObtenerUsuarioId()
    {
        var idTexto = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idTexto, out var id) ? id : null;
    }
}
