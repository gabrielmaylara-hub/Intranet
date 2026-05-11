using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Intranet.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    public int    CodigoEstado { get; private set; }
    public string Mensaje      { get; private set; } = string.Empty;

    public void OnGet(int? statusCode = null)
    {
        CodigoEstado = statusCode ?? 500;
        Mensaje = CodigoEstado switch
        {
            404 => "La página que buscas no existe.",
            403 => "No tienes permiso para acceder a este recurso.",
            500 => "Ocurrió un error interno. Intenta de nuevo más tarde.",
            _   => "Ocurrió un error inesperado."
        };
    }
}
