using System.Globalization;
using Intranet.Models;
using Intranet.Pages.Admin;
using Intranet.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Intranet.Pages.Admin.Avisos;

public class IndexModel : AdminPageModel
{
    private const int MaxTitulo = 200;
    private const int MaxContenido = 4000;

    private readonly IAvisoRepository _avisosRepo;
    private readonly IAreaPublicacionRepository _areasRepo;

    public IndexModel(
        IAvisoRepository avisosRepo,
        IAreaPublicacionRepository areasRepo)
    {
        _avisosRepo = avisosRepo;
        _areasRepo = areasRepo;
    }

    public IEnumerable<Aviso> Avisos { get; private set; } = [];
    public IEnumerable<AreaPublicacion> Areas { get; private set; } = [];
    public string? AreaActualNombre { get; private set; }
    public bool PuedeGestionarAvisos { get; private set; }
    public bool EsAdminGeneralActual => EsAdminGeneral();

    [BindProperty] public int Id { get; set; }
    [BindProperty] public string Titulo { get; set; } = string.Empty;
    [BindProperty] public string? Contenido { get; set; }
    [BindProperty] public string FechaPublicacion { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");
    [BindProperty] public bool Activo { get; set; } = true;
    [BindProperty] public int? AreaPublicacionId { get; set; }

    [BindProperty(SupportsGet = true)] public int? AreaFiltro { get; set; }

    public string? Mensaje { get; private set; }
    public bool EsError { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        await CargarPaginaAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostGuardarAsync()
    {
        var contexto = await ObtenerContextoAsync();
        if (!contexto.PuedeGestionar)
        {
            EsError = true;
            Mensaje = "Tu usuario no tiene un área de publicación activa asignada.";
            await CargarPaginaAsync();
            return Page();
        }

        Aviso? existente = null;

        if (Id > 0)
        {
            existente = await _avisosRepo.ObtenerPorIdAsync(Id);
            if (existente is null)
            {
                EsError = true;
                Mensaje = "No se encontró el aviso seleccionado.";
                await CargarPaginaAsync();
                return Page();
            }

            if (!PuedeOperarAviso(contexto, existente))
                return StatusCode(StatusCodes.Status403Forbidden);
        }

        var error = await ValidarFormularioAsync(contexto);
        if (error is not null)
        {
            EsError = true;
            Mensaje = error;
            await CargarPaginaAsync();
            return Page();
        }

        if (!TryParseFechaAviso(FechaPublicacion, out var fecha))
        {
            EsError = true;
            Mensaje = "La fecha del aviso es obligatoria y debe tener el formato aaaa-mm-dd.";
            await CargarPaginaAsync();
            return Page();
        }

        var areaFinal = contexto.EsAdminGeneral ? NormalizarAreaAdmin(AreaPublicacionId) : contexto.AreaId;
        var usuarioId = ObtenerUsuarioId();

        if (Id > 0 && existente is not null)
        {
            existente.Titulo = Titulo.Trim();
            existente.Contenido = Contenido?.Trim();
            existente.FechaPublicacion = fecha.Date;
            existente.Activo = Activo;
            existente.AreaPublicacionId = areaFinal;
            existente.ActualizadoPorUsuarioId = usuarioId;

            await _avisosRepo.ActualizarAsync(existente);
        }
        else
        {
            var aviso = new Aviso
            {
                Titulo = Titulo.Trim(),
                Contenido = Contenido?.Trim(),
                FechaPublicacion = fecha.Date,
                Activo = Activo,
                Orden = 0,
                AreaPublicacionId = areaFinal,
                CreadoPorUsuarioId = usuarioId
            };

            await _avisosRepo.InsertarAsync(aviso);
        }

        return RedirectToPage(new { AreaFiltro });
    }

    public async Task<IActionResult> OnPostEliminarAsync(int id)
    {
        var contexto = await ObtenerContextoAsync();
        if (!contexto.PuedeGestionar)
            return StatusCode(StatusCodes.Status403Forbidden);

        var aviso = await _avisosRepo.ObtenerPorIdAsync(id);
        if (aviso is null)
            return RedirectToPage(new { AreaFiltro });

        if (!PuedeOperarAviso(contexto, aviso))
            return StatusCode(StatusCodes.Status403Forbidden);

        await _avisosRepo.EliminarAsync(id);
        return RedirectToPage(new { AreaFiltro });
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var contexto = await ObtenerContextoAsync();
        if (!contexto.PuedeGestionar)
            return StatusCode(StatusCodes.Status403Forbidden);

        var aviso = await _avisosRepo.ObtenerPorIdAsync(id);
        if (aviso is null)
            return RedirectToPage(new { AreaFiltro });

        if (!PuedeOperarAviso(contexto, aviso))
            return StatusCode(StatusCodes.Status403Forbidden);

        await _avisosRepo.CambiarEstadoAsync(id, !aviso.Activo);
        return RedirectToPage(new { AreaFiltro });
    }

    private async Task<string?> ValidarFormularioAsync(ContextoAvisos contexto)
    {
        Titulo = Titulo.Trim();
        Contenido = Contenido?.Trim();

        if (string.IsNullOrWhiteSpace(Titulo))
            return "El título del aviso es obligatorio.";
        if (Titulo.Length > MaxTitulo)
            return $"El título no debe superar {MaxTitulo} caracteres.";
        if (ContieneControl(Titulo))
            return "El título contiene caracteres no permitidos.";
        if (string.IsNullOrWhiteSpace(Contenido))
            return "El contenido del aviso es obligatorio.";
        if (Contenido.Length > MaxContenido)
            return $"El contenido no debe superar {MaxContenido} caracteres.";
        if (ContieneControl(Contenido))
            return "El contenido contiene caracteres no permitidos.";

        if (contexto.EsAdminGeneral)
        {
            var area = NormalizarAreaAdmin(AreaPublicacionId);
            if (area.HasValue)
            {
                var areaExistente = await _areasRepo.ObtenerPorIdAsync(area.Value);
                if (areaExistente is null || !areaExistente.Activa)
                    return "Selecciona un área de publicación activa.";
            }
        }
        else if (contexto.AreaId is null)
        {
            return "Tu usuario no tiene un área de publicación activa asignada.";
        }

        return null;
    }

    private async Task CargarPaginaAsync()
    {
        Areas = await _areasRepo.ObtenerActivasAsync();
        var contexto = await ObtenerContextoAsync();

        PuedeGestionarAvisos = contexto.PuedeGestionar;
        AreaActualNombre = contexto.AreaNombre;

        if (contexto.EsAdminGeneral)
        {
            Avisos = AreaFiltro is > 0
                ? await _avisosRepo.ObtenerPorAreaPublicacionAsync(AreaFiltro.Value)
                : await _avisosRepo.ObtenerTodosAsync();
            return;
        }

        Avisos = contexto.AreaId.HasValue
            ? await _avisosRepo.ObtenerPorAreaPublicacionAsync(contexto.AreaId.Value)
            : [];
    }

    private async Task<ContextoAvisos> ObtenerContextoAsync()
    {
        if (EsAdminGeneral())
            return new ContextoAvisos(true, null, null, true);

        if (!EsUsuarioArea())
            return new ContextoAvisos(false, null, null, false);

        var areaId = ObtenerAreaPublicacionId();
        if (areaId is null or <= 0)
            return new ContextoAvisos(false, null, null, false);

        var area = await _areasRepo.ObtenerPorIdAsync(areaId.Value);
        return area is { Activa: true }
            ? new ContextoAvisos(false, area.Id, area.Nombre, true)
            : new ContextoAvisos(false, null, null, false);
    }

    private static bool PuedeOperarAviso(ContextoAvisos contexto, Aviso aviso) =>
        contexto.EsAdminGeneral ||
        contexto.AreaId.HasValue &&
        aviso.AreaPublicacionId == contexto.AreaId.Value;

    private static int? NormalizarAreaAdmin(int? areaId) =>
        areaId is > 0 ? areaId : null;

    private static bool TryParseFechaAviso(string? valor, out DateTime fecha)
    {
        fecha = default;

        if (string.IsNullOrWhiteSpace(valor))
            return false;

        return DateTime.TryParseExact(
            valor.Trim(),
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out fecha);
    }

    private static bool ContieneControl(string? valor) =>
        !string.IsNullOrEmpty(valor) &&
        valor.Any(c => char.IsControl(c) && c is not '\r' and not '\n' and not '\t');

    private sealed record ContextoAvisos(
        bool EsAdminGeneral,
        int? AreaId,
        string? AreaNombre,
        bool PuedeGestionar);
}
