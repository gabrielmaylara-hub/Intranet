using Intranet.Services.Interfaces;
using System.Text;

namespace Intranet.Services;

/// <summary>
/// Gestiona el almacenamiento de archivos subidos en la carpeta Storage/ (fuera de wwwroot).
/// Genera nombres únicos para evitar colisiones y previene sobrescrituras accidentales.
/// </summary>
public class ArchivoService : IArchivoService
{
    private readonly string _rutaBaseStorage;
    private readonly long _maxTamanoGeneral;
    private const long MaxTamanoSvg = 2L * 1024L * 1024L;
    private const long MaxTamanoGeneralPredeterminado = 524_288_000L;

    // Extensiones permitidas por tipo de contenido
    private static readonly HashSet<string> ExtPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".png", ".svg", ".jpg", ".jpeg", ".webp", ".mp4"
    };

    public ArchivoService(IWebHostEnvironment entorno, IConfiguration config)
    {
        var subcarpeta = config["Storage:RutaBase"] ?? "Storage";
        _rutaBaseStorage = Path.GetFullPath(Path.Combine(entorno.ContentRootPath, subcarpeta));
        _maxTamanoGeneral = config.GetValue<long?>("Uploads:MaxFileSizeBytes")
            ?? MaxTamanoGeneralPredeterminado;

        if (_maxTamanoGeneral <= 0)
            throw new InvalidOperationException("Uploads:MaxFileSizeBytes debe ser mayor que cero.");

        Directory.CreateDirectory(_rutaBaseStorage);
    }

    public async Task<string> GuardarAsync(
        IFormFile archivo, string subcarpeta, string? nombreForzado = null)
    {
        if (archivo.Length == 0)
            throw new InvalidOperationException("El archivo recibido está vacío.");

        if (archivo.Length > _maxTamanoGeneral)
            throw new InvalidOperationException(
                $"El archivo excede el tamaño máximo permitido de {FormatearTamano(_maxTamanoGeneral)}.");

        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();

        if (!ExtPermitidas.Contains(extension))
            throw new InvalidOperationException(
                $"Tipo de archivo no permitido: {extension}");

        if (extension == ".svg")
            await ValidarSvgAsync(archivo);

        var subcarpetaSegura = NormalizarSubcarpeta(subcarpeta);
        var carpeta = Path.GetFullPath(Path.Combine(
            _rutaBaseStorage,
            subcarpetaSegura.Replace('/', Path.DirectorySeparatorChar)));

        if (!EstaDentroDeStorage(carpeta))
            throw new InvalidOperationException("La ruta de almacenamiento no es válida.");

        Directory.CreateDirectory(carpeta);

        var nombreArchivo = nombreForzado is not null
            ? $"{SanitizarNombre(nombreForzado)}{extension}"
            : $"{Guid.NewGuid():N}{extension}";

        var rutaFisica = Path.Combine(carpeta, nombreArchivo);
        await using var stream = File.Create(rutaFisica);
        await archivo.CopyToAsync(stream);

        // Retorna la ruta relativa desde Storage/ con separador Unix para portabilidad
        return Path.Combine(subcarpetaSegura, nombreArchivo).Replace('\\', '/');
    }

    public void Eliminar(string? rutaRelativa)
    {
        if (string.IsNullOrWhiteSpace(rutaRelativa)) return;

        var rutaFisica = ObtenerRutaFisica(rutaRelativa);

        // Prevención de path traversal al eliminar
        if (!EstaDentroDeStorage(rutaFisica))
            return;

        if (File.Exists(rutaFisica))
            File.Delete(rutaFisica);
    }

    public string ObtenerRutaFisica(string rutaRelativa) =>
        Path.GetFullPath(Path.Combine(
            _rutaBaseStorage,
            rutaRelativa.Replace('/', Path.DirectorySeparatorChar)));

    private bool EstaDentroDeStorage(string rutaFisica)
    {
        var baseConSeparador = _rutaBaseStorage.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

        return rutaFisica.StartsWith(baseConSeparador, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizarSubcarpeta(string subcarpeta)
    {
        var normalizada = subcarpeta.Replace('\\', '/').Trim('/');
        if (normalizada.Contains("..", StringComparison.Ordinal))
            throw new InvalidOperationException("La subcarpeta de almacenamiento no es válida.");

        return normalizada;
    }

    private static async Task ValidarSvgAsync(IFormFile archivo)
    {
        if (archivo.Length > MaxTamanoSvg)
            throw new InvalidOperationException("El archivo SVG excede el tamaño máximo permitido.");

        using var lector = new StreamReader(
            archivo.OpenReadStream(),
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true);

        var contenido = (await lector.ReadToEndAsync()).ToLowerInvariant();
        var patronesNoPermitidos = new[]
        {
            "<script",
            "javascript:",
            "onload=",
            "onerror=",
            "<foreignobject"
        };

        if (patronesNoPermitidos.Any(patron => contenido.Contains(patron, StringComparison.Ordinal)))
            throw new InvalidOperationException("El SVG contiene elementos no permitidos.");
    }

    private static string SanitizarNombre(string nombre)
    {
        var invalidos = Path.GetInvalidFileNameChars();
        var limpio = string.Concat(nombre.Select(c => invalidos.Contains(c) ? '_' : c)).Trim();
        return string.IsNullOrWhiteSpace(limpio) ? Guid.NewGuid().ToString("N") : limpio;
    }

    private static string FormatearTamano(long bytes)
    {
        var megabytes = bytes / 1024D / 1024D;
        return megabytes % 1 == 0
            ? $"{megabytes:0} MB"
            : $"{megabytes:0.##} MB";
    }
}
