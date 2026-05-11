namespace Intranet.Services.Interfaces;

public interface IArchivoService
{
    /// <summary>
    /// Guarda un archivo en Storage/{subcarpeta}/ y retorna la ruta relativa desde Storage/.
    /// </summary>
    Task<string> GuardarAsync(IFormFile archivo, string subcarpeta, string? nombreForzado = null);

    /// <summary>
    /// Elimina un archivo del Storage dado su ruta relativa desde Storage/.
    /// No lanza excepción si el archivo no existe.
    /// </summary>
    void Eliminar(string? rutaRelativa);

    /// <summary>Retorna la ruta física absoluta de un archivo en Storage.</summary>
    string ObtenerRutaFisica(string rutaRelativa);
}
