using Intranet.Models;
using Intranet.Pages.Admin;
using Intranet.Repositories.Interfaces;
using Intranet.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace Intranet.Pages.Admin.Usuarios;

public class IndexModel : AdminPageModel
{
    private const int MinPassword = 10;
    private const int MaxUsuario = 100;
    private const int MaxNombre = 200;
    private const string RolAdminGeneral = "admin_general";
    private const string RolUsuarioArea = "usuario_area";

    private readonly IUsuarioRepository _usuariosRepo;
    private readonly IAreaPublicacionRepository _areasRepo;
    private readonly IAuthService _auth;

    public IndexModel(
        IUsuarioRepository usuariosRepo,
        IAreaPublicacionRepository areasRepo,
        IAuthService auth)
    {
        _usuariosRepo = usuariosRepo;
        _areasRepo = areasRepo;
        _auth = auth;
    }

    public IEnumerable<UsuarioAdmin> Usuarios { get; private set; } = [];
    public IEnumerable<AreaPublicacion> Areas { get; private set; } = [];

    [BindProperty] public int Id { get; set; }
    [BindProperty] public string Usuario { get; set; } = string.Empty;
    [BindProperty] public string NombreCompleto { get; set; } = string.Empty;
    [BindProperty] public string Rol { get; set; } = RolUsuarioArea;
    [BindProperty] public int? AreaPublicacionId { get; set; }
    [BindProperty] public bool Activo { get; set; } = true;
    [BindProperty] public string PasswordTemporal { get; set; } = string.Empty;
    [BindProperty] public string ConfirmarPasswordTemporal { get; set; } = string.Empty;

    [TempData] public string? Mensaje { get; set; }
    [TempData] public bool EsError { get; set; }

    public async Task<IActionResult> OnGetAsync(int? editarId)
    {
        if (!EsAdminGeneral())
            return StatusCode(StatusCodes.Status403Forbidden);

        await CargarListasAsync();

        if (editarId is null)
            return Page();

        var usuario = await _usuariosRepo.ObtenerPorIdAsync(editarId.Value);
        if (usuario is null)
        {
            EsError = true;
            Mensaje = "No se encontró el usuario seleccionado.";
            return Page();
        }

        Id = usuario.Id;
        Usuario = usuario.Usuario;
        NombreCompleto = usuario.NombreCompleto;
        Rol = usuario.Rol;
        AreaPublicacionId = usuario.AreaPublicacionId;
        Activo = usuario.Activo;

        return Page();
    }

    public async Task<IActionResult> OnPostGuardarAsync()
    {
        if (!EsAdminGeneral())
            return StatusCode(StatusCodes.Status403Forbidden);

        var error = await ValidarFormularioAsync(esCreacion: Id == 0);
        if (error is not null)
        {
            EsError = true;
            Mensaje = error;
            await CargarListasAsync();
            return Page();
        }

        AreaPublicacionId = Rol == RolUsuarioArea ? AreaPublicacionId : null;

        try
        {
            if (Id == 0)
            {
                var usuario = new UsuarioAdmin
                {
                    Usuario = Usuario,
                    NombreCompleto = NombreCompleto,
                    PasswordHash = _auth.HashearPassword(PasswordTemporal),
                    Rol = Rol,
                    AreaPublicacionId = AreaPublicacionId,
                    Activo = Activo
                };

                await _usuariosRepo.CrearAsync(usuario);
                Mensaje = "Usuario creado correctamente.";
            }
            else
            {
                var usuario = await _usuariosRepo.ObtenerPorIdAsync(Id);
                if (usuario is null)
                {
                    EsError = true;
                    Mensaje = "No se encontró el usuario seleccionado.";
                    await CargarListasAsync();
                    return Page();
                }

                usuario.NombreCompleto = NombreCompleto;
                usuario.Rol = Rol;
                usuario.AreaPublicacionId = AreaPublicacionId;
                usuario.Activo = Activo;

                await _usuariosRepo.ActualizarAsync(usuario);
                Mensaje = "Usuario actualizado correctamente.";
            }
        }
        catch (MySqlException ex) when (EsDuplicadoUsuario(ex))
        {
            EsError = true;
            Mensaje = "Ya existe un usuario con ese nombre de acceso.";
            await CargarListasAsync();
            return Page();
        }

        EsError = false;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostActivarAsync(int id)
    {
        if (!EsAdminGeneral())
            return StatusCode(StatusCodes.Status403Forbidden);

        var usuario = await _usuariosRepo.ObtenerPorIdAsync(id);
        if (usuario is null)
        {
            EsError = true;
            Mensaje = "No se encontró el usuario seleccionado.";
            return RedirectToPage();
        }

        if (await EsUsuarioAreaSinArea(usuario))
        {
            EsError = true;
            Mensaje = "No se puede activar un usuario de área sin área activa asignada.";
            return RedirectToPage();
        }

        await _usuariosRepo.ActivarAsync(id);
        EsError = false;
        Mensaje = "Usuario activado.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDesactivarAsync(int id)
    {
        if (!EsAdminGeneral())
            return StatusCode(StatusCodes.Status403Forbidden);

        var error = await ValidarNoDejarSinAdminGeneralAsync(id, desactivar: true);
        if (error is not null)
        {
            EsError = true;
            Mensaje = error;
            return RedirectToPage();
        }

        await _usuariosRepo.DesactivarAsync(id);
        EsError = false;
        Mensaje = "Usuario desactivado.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostResetearPasswordAsync(
        int id,
        string nuevoPassword,
        string confirmarPassword)
    {
        if (!EsAdminGeneral())
            return StatusCode(StatusCodes.Status403Forbidden);

        var usuario = await _usuariosRepo.ObtenerPorIdAsync(id);
        if (usuario is null)
        {
            EsError = true;
            Mensaje = "No se encontró el usuario seleccionado.";
            return RedirectToPage();
        }

        var error = ValidarPasswordTemporal(nuevoPassword, confirmarPassword);
        if (error is not null)
        {
            EsError = true;
            Mensaje = error;
            return RedirectToPage();
        }

        var hash = _auth.HashearPassword(nuevoPassword.Trim());
        await _usuariosRepo.ResetearPasswordAsync(id, hash);

        EsError = false;
        Mensaje = "Contraseña temporal actualizada.";
        return RedirectToPage();
    }

    private async Task<string?> ValidarFormularioAsync(bool esCreacion)
    {
        Usuario = NormalizarTexto(Usuario);
        NombreCompleto = NormalizarTexto(NombreCompleto);
        Rol = NormalizarTexto(Rol);
        PasswordTemporal = PasswordTemporal.Trim();
        ConfirmarPasswordTemporal = ConfirmarPasswordTemporal.Trim();

        if (string.IsNullOrWhiteSpace(Usuario))
            return "El usuario es obligatorio.";
        if (Usuario.Length > MaxUsuario)
            return $"El usuario no debe superar {MaxUsuario} caracteres.";
        if (Usuario.Any(char.IsWhiteSpace) || ContieneControl(Usuario))
            return "El usuario no debe contener espacios ni caracteres de control.";
        if (string.IsNullOrWhiteSpace(NombreCompleto))
            return "El nombre es obligatorio.";
        if (NombreCompleto.Length > MaxNombre)
            return $"El nombre no debe superar {MaxNombre} caracteres.";
        if (ContieneControl(NombreCompleto))
            return "El nombre contiene caracteres no permitidos.";
        if (Rol is not RolAdminGeneral and not RolUsuarioArea)
            return "Selecciona un rol válido.";
        if (await _usuariosRepo.ExisteUsuarioAsync(Usuario, Id > 0 ? Id : null))
            return "Ya existe un usuario con ese nombre de acceso.";

        if (Rol == RolUsuarioArea)
        {
            if (AreaPublicacionId is null or <= 0)
                return "El usuario de área necesita un área de publicación.";
            if (!await _usuariosRepo.ExisteAreaPublicacionAsync(AreaPublicacionId.Value))
                return "Selecciona un área de publicación activa.";
        }

        if (esCreacion)
        {
            var errorPassword = ValidarPasswordTemporal(PasswordTemporal, ConfirmarPasswordTemporal);
            if (errorPassword is not null)
                return errorPassword;
        }
        else
        {
            var errorAdmin = await ValidarNoDejarSinAdminGeneralAsync(
                Id,
                desactivar: !Activo,
                nuevoRol: Rol);
            if (errorAdmin is not null)
                return errorAdmin;
        }

        return null;
    }

    private async Task<string?> ValidarNoDejarSinAdminGeneralAsync(
        int id,
        bool desactivar,
        string? nuevoRol = null)
    {
        var usuario = await _usuariosRepo.ObtenerPorIdAsync(id);
        if (usuario is null || !usuario.Activo || usuario.Rol != RolAdminGeneral)
            return null;

        var dejariaDeSerAdminActivo =
            desactivar ||
            !string.IsNullOrWhiteSpace(nuevoRol) &&
            !string.Equals(nuevoRol, RolAdminGeneral, StringComparison.OrdinalIgnoreCase);

        if (!dejariaDeSerAdminActivo)
            return null;

        var adminsRestantes = await _usuariosRepo.ContarAdminsGeneralesActivosAsync(id);
        return adminsRestantes == 0
            ? "No se puede dejar el sistema sin un admin_general activo."
            : null;
    }

    private async Task<bool> EsUsuarioAreaSinArea(UsuarioAdmin usuario) =>
        usuario.Rol == RolUsuarioArea &&
        (usuario.AreaPublicacionId is null ||
         !await _usuariosRepo.ExisteAreaPublicacionAsync(usuario.AreaPublicacionId.Value));

    private async Task CargarListasAsync()
    {
        Usuarios = await _usuariosRepo.ListarAsync();
        Areas = await _areasRepo.ObtenerTodasAsync();
    }

    private static string? ValidarPasswordTemporal(string password, string confirmacion)
    {
        password = password.Trim();
        confirmacion = confirmacion.Trim();

        if (string.IsNullOrWhiteSpace(password))
            return "Ingresa una contraseña temporal.";
        if (password.Length < MinPassword)
            return $"La contraseña temporal debe tener al menos {MinPassword} caracteres.";
        if (!string.Equals(password, confirmacion, StringComparison.Ordinal))
            return "La confirmación de contraseña no coincide.";

        return null;
    }

    private static string NormalizarTexto(string? valor) => valor?.Trim() ?? string.Empty;

    private static bool ContieneControl(string? valor) =>
        !string.IsNullOrEmpty(valor) &&
        valor.Any(c => char.IsControl(c) && c is not '\t');

    private static bool EsDuplicadoUsuario(MySqlException ex) =>
        ex.Number == 1062 &&
        ex.Message.Contains("uq_usuario", StringComparison.OrdinalIgnoreCase);
}
