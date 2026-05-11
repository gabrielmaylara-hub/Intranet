using System.Security.Claims;
using Intranet.Repositories.Interfaces;
using Intranet.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Intranet.Pages.Admin;

public class LoginModel : PageModel
{
    private readonly IUsuarioRepository _usuariosRepo;
    private readonly IAuthService       _auth;
    private readonly ILoginAttemptService _intentosLogin;

    public LoginModel(
        IUsuarioRepository usuariosRepo,
        IAuthService auth,
        ILoginAttemptService intentosLogin)
    {
        _usuariosRepo  = usuariosRepo;
        _auth          = auth;
        _intentosLogin = intentosLogin;
    }

    [BindProperty]
    public string Usuario   { get; set; } = string.Empty;

    [BindProperty]
    public string Password  { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? ErrorMensaje { get; private set; }

    public IActionResult OnGet()
    {
        // Si ya tiene sesión activa, redirige al dashboard
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Admin/Index");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Usuario) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMensaje = "Ingresa tu usuario y contraseña.";
            return Page();
        }

        if (_intentosLogin.EstaBloqueado(HttpContext, Usuario))
        {
            ErrorMensaje = "Demasiados intentos. Intente nuevamente más tarde.";
            return Page();
        }

        var usuario = await _usuariosRepo.ObtenerPorUsuarioAsync(Usuario.Trim());

        if (usuario is null || !_auth.VerificarPassword(Password, usuario.PasswordHash))
        {
            _intentosLogin.RegistrarFallo(HttpContext, Usuario);
            ErrorMensaje = _intentosLogin.EstaBloqueado(HttpContext, Usuario)
                ? "Demasiados intentos. Intente nuevamente más tarde."
                : "Usuario o contraseña incorrectos.";
            return Page();
        }

        _intentosLogin.RegistrarExito(HttpContext, Usuario);

        // Crea los claims de la sesión (sin roles — un solo perfil admin)
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name,        usuario.Usuario),
            new(ClaimTypes.GivenName,   usuario.NombreCompleto),
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString())
        };

        var identidad  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal  = new ClaimsPrincipal(identidad);
        var propiedades = new AuthenticationProperties
        {
            IsPersistent = false,
            ExpiresUtc   = DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            propiedades);

        if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            return LocalRedirect(ReturnUrl);

        return RedirectToPage("/Admin/Index");
    }

    public async Task<IActionResult> OnPostSalirAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Admin/Login");
    }
}
