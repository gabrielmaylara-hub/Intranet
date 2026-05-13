using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Intranet.Pages.Admin;

public abstract class AdminPageModel : PageModel
{
    protected bool EsAdminGeneral() =>
        string.Equals(
            User.FindFirst("rol")?.Value,
            "admin_general",
            StringComparison.OrdinalIgnoreCase);

    protected int? ObtenerUsuarioId()
    {
        var idTexto = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idTexto, out var id) ? id : null;
    }
}
