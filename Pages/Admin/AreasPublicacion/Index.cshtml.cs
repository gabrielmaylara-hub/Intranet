using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Intranet.Models;
using Intranet.Pages.Admin;
using Intranet.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;

namespace Intranet.Pages.Admin.AreasPublicacion;

public class IndexModel : AdminPageModel
{
    private const int MaxNombre = 180;
    private const int MaxDescripcion = 300;

    private readonly IAreaPublicacionRepository _areasRepo;

    public IndexModel(IAreaPublicacionRepository areasRepo) =>
        _areasRepo = areasRepo;

    public IEnumerable<AreaPublicacion> Areas { get; private set; } = [];

    [BindProperty] public int Id { get; set; }
    [BindProperty] public string Nombre { get; set; } = string.Empty;
    [BindProperty] public string? Descripcion { get; set; }
    [BindProperty] public int Orden { get; set; }
    [BindProperty] public bool Activa { get; set; } = true;

    [TempData] public string? Mensaje { get; set; }
    [TempData] public bool EsError { get; set; }

    public async Task<IActionResult> OnGetAsync(int? editarId)
    {
        if (!EsAdminGeneral())
            return StatusCode(StatusCodes.Status403Forbidden);

        await CargarAreasAsync();

        if (editarId is null)
            return Page();

        var area = await _areasRepo.ObtenerPorIdAsync(editarId.Value);
        if (area is null)
        {
            EsError = true;
            Mensaje = "No se encontró el área seleccionada.";
            return Page();
        }

        Id = area.Id;
        Nombre = area.Nombre;
        Descripcion = area.Descripcion;
        Orden = area.Orden;
        Activa = area.Activa;
        return Page();
    }

    public async Task<IActionResult> OnPostGuardarAsync()
    {
        if (!EsAdminGeneral())
            return StatusCode(StatusCodes.Status403Forbidden);

        var error = await ValidarFormularioAsync();
        if (error is not null)
        {
            EsError = true;
            Mensaje = error;
            await CargarAreasAsync();
            return Page();
        }

        var nombreNormalizado = NormalizarTexto(Nombre);
        var descripcionNormalizada = NormalizarTexto(Descripcion);
        var ordenFinal = Orden > 0
            ? Orden
            : await ObtenerSiguienteOrdenAsync();

        try
        {
            if (Id > 0)
            {
                var existente = await _areasRepo.ObtenerPorIdAsync(Id);
                if (existente is null)
                {
                    EsError = true;
                    Mensaje = "No se encontró el área seleccionada.";
                    await CargarAreasAsync();
                    return Page();
                }

                existente.Nombre = nombreNormalizado;
                existente.Descripcion = descripcionNormalizada;
                existente.Orden = ordenFinal;
                existente.Activa = Activa;

                await _areasRepo.ActualizarAsync(existente);
                Mensaje = "Área de publicación actualizada.";
            }
            else
            {
                await _areasRepo.CrearAsync(new AreaPublicacion
                {
                    Nombre = nombreNormalizado,
                    Slug = GenerarSlug(nombreNormalizado),
                    Descripcion = descripcionNormalizada,
                    Orden = ordenFinal,
                    Activa = Activa
                });

                Mensaje = "Área de publicación creada.";
            }
        }
        catch (MySqlException ex) when (EsDuplicado(ex))
        {
            EsError = true;
            Mensaje = "Ya existe un área de publicación con ese nombre.";
            await CargarAreasAsync();
            return Page();
        }

        EsError = false;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostActivarAsync(int id)
    {
        if (!EsAdminGeneral())
            return StatusCode(StatusCodes.Status403Forbidden);

        await _areasRepo.ActivarAsync(id);
        Mensaje = "Área de publicación activada.";
        EsError = false;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDesactivarAsync(int id)
    {
        if (!EsAdminGeneral())
            return StatusCode(StatusCodes.Status403Forbidden);

        await _areasRepo.DesactivarAsync(id);
        Mensaje = "Área de publicación desactivada.";
        EsError = false;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEliminarAsync(int id)
    {
        if (!EsAdminGeneral())
            return StatusCode(StatusCodes.Status403Forbidden);

        var puedeEliminar = await _areasRepo.PuedeEliminarAsync(id);
        if (!puedeEliminar)
        {
            EsError = true;
            Mensaje = "No se puede eliminar porque hay usuarios asociados. Desactívala si ya no debe usarse.";
            return RedirectToPage();
        }

        var eliminada = await _areasRepo.EliminarAsync(id);
        EsError = !eliminada;
        Mensaje = eliminada
            ? "Área de publicación eliminada."
            : "No se encontró el área seleccionada.";

        return RedirectToPage();
    }

    private async Task<string?> ValidarFormularioAsync()
    {
        Nombre = NormalizarTexto(Nombre);
        Descripcion = NormalizarTexto(Descripcion);

        if (string.IsNullOrWhiteSpace(Nombre))
            return "El nombre del área es obligatorio.";
        if (Nombre.Length > MaxNombre)
            return $"El nombre del área no debe superar {MaxNombre} caracteres.";
        if (!string.IsNullOrWhiteSpace(Descripcion) && Descripcion.Length > MaxDescripcion)
            return $"La descripción no debe superar {MaxDescripcion} caracteres.";
        if (Orden < 0)
            return "El orden no puede ser negativo.";
        if (ContieneControl(Nombre) || ContieneControl(Descripcion))
            return "El formulario contiene caracteres no permitidos.";
        if (await _areasRepo.ExisteNombreAsync(Nombre, Id > 0 ? Id : null))
            return "Ya existe un área de publicación con ese nombre.";

        return null;
    }

    private async Task CargarAreasAsync() =>
        Areas = await _areasRepo.ObtenerTodasAsync();

    private async Task<int> ObtenerSiguienteOrdenAsync()
    {
        var areas = (await _areasRepo.ObtenerTodasAsync()).ToList();
        return areas.Count == 0 ? 1 : areas.Max(a => a.Orden) + 1;
    }

    private static string NormalizarTexto(string? valor) => valor?.Trim() ?? string.Empty;

    private static bool ContieneControl(string? valor) =>
        !string.IsNullOrEmpty(valor) &&
        valor.Any(c => char.IsControl(c) && c is not '\t');

    private static bool EsDuplicado(MySqlException ex) =>
        ex.Number == 1062 &&
        (ex.Message.Contains("uk_areas_publicacion_nombre", StringComparison.OrdinalIgnoreCase) ||
         ex.Message.Contains("uk_areas_publicacion_slug", StringComparison.OrdinalIgnoreCase));

    private static string GenerarSlug(string valor)
    {
        var normalizado = valor.Normalize(NormalizationForm.FormD);
        var sinAcentos = new StringBuilder();

        foreach (var caracter in normalizado)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark)
                sinAcentos.Append(caracter);
        }

        var slug = sinAcentos
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .ToLowerInvariant();

        slug = Regex.Replace(slug, @"[^a-z0-9]+", "-");
        slug = Regex.Replace(slug, @"-+", "-").Trim('-');

        return string.IsNullOrWhiteSpace(slug)
            ? $"area-{Guid.NewGuid():N}"
            : slug;
    }
}
