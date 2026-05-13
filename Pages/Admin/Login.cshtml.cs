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
    private readonly IConfiguracionRepository _configRepo;
    private readonly IAuthService       _auth;
    private readonly ILoginAttemptService _intentosLogin;

    public LoginModel(
        IUsuarioRepository usuariosRepo,
        IConfiguracionRepository configRepo,
        IAuthService auth,
        ILoginAttemptService intentosLogin)
    {
        _usuariosRepo  = usuariosRepo;
        _configRepo    = configRepo;
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
    public string  LogoPath { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        // Si ya tiene sesión activa, redirige al dashboard
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Admin/Index");

        await CargarLogoAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Usuario) || string.IsNullOrWhiteSpace(Password))
        {
            await CargarLogoAsync();
            ErrorMensaje = "Ingresa tu usuario y contraseña.";
            return Page();
        }

        if (_intentosLogin.EstaBloqueado(HttpContext, Usuario))
        {
            await CargarLogoAsync();
            ErrorMensaje = "Demasiados intentos. Intente nuevamente más tarde.";
            return Page();
        }

        var usuario = await _usuariosRepo.ObtenerPorUsuarioAsync(Usuario.Trim());

        if (usuario is null || !_auth.VerificarPassword(Password, usuario.PasswordHash))
        {
            _intentosLogin.RegistrarFallo(HttpContext, Usuario);
            await CargarLogoAsync();
            ErrorMensaje = _intentosLogin.EstaBloqueado(HttpContext, Usuario)
                ? "Demasiados intentos. Intente nuevamente más tarde."
                : "Usuario o contraseña incorrectos.";
            return Page();
        }

        _intentosLogin.RegistrarExito(HttpContext, Usuario);

        // Crea los claims de la sesión. NameIdentifier se mantiene como Id para
        // no romper cambio de contraseña ni flujos existentes; rol/area preparan
        // la fase futura de usuarios por area.
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name,        usuario.Usuario),
            new(ClaimTypes.GivenName,   usuario.NombreCompleto),
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new("rol", usuario.Rol)
        };

        if (usuario.AreaPublicacionId is not null)
            claims.Add(new Claim("area_publicacion_id", usuario.AreaPublicacionId.Value.ToString()));

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

    private async Task CargarLogoAsync()
    {
        LogoPath = await _configRepo.ObtenerValorAsync("logo_path") ?? string.Empty;
    }
}
