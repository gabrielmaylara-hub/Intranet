using System.Security.Claims;
using Intranet.Repositories.Interfaces;
using Intranet.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Intranet.Pages.Admin;

public class CambiarPasswordModel : PageModel
{
    private const int LongitudMinimaPassword = 10;

    private readonly IUsuarioRepository _usuariosRepo;
    private readonly IAuthService _auth;

    public CambiarPasswordModel(IUsuarioRepository usuariosRepo, IAuthService auth)
    {
        _usuariosRepo = usuariosRepo;
        _auth = auth;
    }

    [BindProperty] public string PasswordActual { get; set; } = string.Empty;
    [BindProperty] public string NuevoPassword { get; set; } = string.Empty;
    [BindProperty] public string ConfirmarPassword { get; set; } = string.Empty;

    public string? Mensaje { get; private set; }
    public bool EsError { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var usuario = await ObtenerUsuarioActualAsync();
        if (usuario is null)
            return RedirectToPage("/Admin/Login");

        var error = ValidarFormulario();
        if (error is not null)
        {
            EsError = true;
            Mensaje = error;
            return Page();
        }

        if (!_auth.VerificarPassword(PasswordActual, usuario.PasswordHash))
        {
            EsError = true;
            Mensaje = "La contraseña actual no es correcta.";
            return Page();
        }

        var nuevoHash = _auth.HashearPassword(NuevoPassword);
        var actualizado = await _usuariosRepo.ActualizarPasswordHashAsync(usuario.Id, nuevoHash);

        if (!actualizado)
        {
            EsError = true;
            Mensaje = "No se pudo actualizar la contraseña. Intenta nuevamente.";
            return Page();
        }

        PasswordActual = string.Empty;
        NuevoPassword = string.Empty;
        ConfirmarPassword = string.Empty;
        Mensaje = "Contraseña actualizada correctamente. Por seguridad, cierra sesión e ingresa nuevamente.";
        return Page();
    }

    private async Task<Models.UsuarioAdmin?> ObtenerUsuarioActualAsync()
    {
        var idTexto = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idTexto, out var id)
            ? await _usuariosRepo.ObtenerPorIdAsync(id)
            : null;
    }

    private string? ValidarFormulario()
    {
        PasswordActual = PasswordActual.Trim();
        NuevoPassword = NuevoPassword.Trim();
        ConfirmarPassword = ConfirmarPassword.Trim();

        if (string.IsNullOrWhiteSpace(PasswordActual))
            return "Ingresa tu contraseña actual.";
        if (string.IsNullOrWhiteSpace(NuevoPassword))
            return "Ingresa la nueva contraseña.";
        if (NuevoPassword.Length < LongitudMinimaPassword)
            return $"La nueva contraseña debe tener al menos {LongitudMinimaPassword} caracteres.";
        if (!string.Equals(NuevoPassword, ConfirmarPassword, StringComparison.Ordinal))
            return "La confirmación no coincide con la nueva contraseña.";

        return null;
    }
}
