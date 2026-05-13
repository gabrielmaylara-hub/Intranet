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

    // Defensa en capas para uploads: extension visible, MIME declarado y firma
    // real del archivo. No relajar esta lista sin revisar tambien Program.cs,
    // porque /storage solo sirve extensiones autorizadas.
    private static readonly HashSet<string> ExtPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".png", ".svg", ".jpg", ".jpeg", ".webp", ".mp4"
    };

    private static readonly Dictionary<string, string[]> MimePermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = ["application/pdf"],
        [".png"] = ["image/png"],
        [".jpg"] = ["image/jpeg", "image/pjpeg"],
        [".jpeg"] = ["image/jpeg", "image/pjpeg"],
        [".webp"] = ["image/webp"],
        [".mp4"] = ["video/mp4", "application/mp4"],
        [".svg"] = ["image/svg+xml", "text/xml", "application/xml"]
    };

    private static readonly byte[] FirmaPng = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly HashSet<string> MarcasMp4 = new(StringComparer.OrdinalIgnoreCase)
    {
        "isom", "iso2", "mp41", "mp42", "avc1", "M4V ", "MSNV", "dash"
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

        await ValidarTipoArchivoAsync(archivo, extension);

        var subcarpetaSegura = NormalizarSubcarpeta(subcarpeta);
        var carpeta = Path.GetFullPath(Path.Combine(
            _rutaBaseStorage,
            subcarpetaSegura.Replace('/', Path.DirectorySeparatorChar)));

        // La ruta final siempre debe quedar dentro de Storage/. Esta validacion
        // protege contra path traversal aunque el nombre o subcarpeta vengan
        // de un formulario manipulado.
        if (!EstaDentroDeStorage(carpeta))
            throw new InvalidOperationException("La ruta de almacenamiento no es válida.");

        Directory.CreateDirectory(carpeta);

        var nombreArchivo = nombreForzado is not null
            ? $"{SanitizarNombre(nombreForzado)}{extension}"
            : $"{Guid.NewGuid():N}{extension}";

        var rutaFisica = Path.Combine(carpeta, nombreArchivo);
        await using var stream = File.Create(rutaFisica);
        await archivo.CopyToAsync(stream);

        // Retorna la ruta relativa desde Storage/ con separador Unix. Esa ruta
        // es la que se guarda en BD y luego se resuelve via /storage/{ruta}.
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
        // Evita que una subcarpeta manipulada salga de Storage usando ..
        var normalizada = subcarpeta.Replace('\\', '/').Trim('/');
        if (normalizada.Contains("..", StringComparison.Ordinal))
            throw new InvalidOperationException("La subcarpeta de almacenamiento no es válida.");

        return normalizada;
    }

    private static async Task ValidarSvgAsync(IFormFile archivo)
    {
        // SVG es texto y puede traer script embebido. Se permite por el logo,
        // pero se bloquean patrones de ejecucion comunes antes de guardarlo.
        if (archivo.Length > MaxTamanoSvg)
            throw new InvalidOperationException("El archivo SVG excede el tamaño máximo permitido.");

        using var lector = new StreamReader(
            archivo.OpenReadStream(),
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true);

        var contenido = (await lector.ReadToEndAsync()).ToLowerInvariant();
        if (!contenido.Contains("<svg", StringComparison.Ordinal))
            throw new InvalidOperationException("El contenido del SVG no corresponde al tipo permitido.");

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

    private static async Task ValidarTipoArchivoAsync(IFormFile archivo, string extension)
    {
        // El navegador puede mentir sobre Content-Type; por eso tambien se lee
        // la firma de los primeros bytes antes de aceptar el archivo.
        ValidarMimeDeclarado(archivo.ContentType, extension);

        if (extension == ".svg")
        {
            await ValidarSvgAsync(archivo);
            return;
        }

        var bytesALeer = (int)Math.Min(64L, archivo.Length);
        var encabezado = new byte[bytesALeer];

        await using var stream = archivo.OpenReadStream();
        var leidos = await stream.ReadAsync(encabezado.AsMemory(0, bytesALeer));

        if (!FirmaValida(extension, encabezado.AsSpan(0, leidos)))
            throw new InvalidOperationException("El contenido del archivo no corresponde al tipo permitido.");
    }

    private static void ValidarMimeDeclarado(string? contentType, string extension)
    {
        var tipo = contentType?.Split(';', 2)[0].Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(tipo) || tipo == "application/octet-stream")
            return;

        if (!MimePermitidos.TryGetValue(extension, out var permitidos) ||
            !permitidos.Contains(tipo, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("El tipo declarado del archivo no corresponde a la extension permitida.");
        }
    }

    private static bool FirmaValida(string extension, ReadOnlySpan<byte> encabezado) =>
        extension switch
        {
            ".pdf" => IniciaConAscii(encabezado, "%PDF-"),
            ".png" => encabezado.StartsWith(FirmaPng),
            ".jpg" or ".jpeg" => TieneFirmaJpeg(encabezado),
            ".webp" => TieneFirmaWebp(encabezado),
            ".mp4" => TieneFirmaMp4(encabezado),
            _ => false
        };

    private static bool TieneFirmaJpeg(ReadOnlySpan<byte> encabezado) =>
        encabezado.Length >= 3 &&
        encabezado[0] == 0xFF &&
        encabezado[1] == 0xD8 &&
        encabezado[2] == 0xFF;

    private static bool TieneFirmaWebp(ReadOnlySpan<byte> encabezado) =>
        encabezado.Length >= 12 &&
        IniciaConAscii(encabezado[..4], "RIFF") &&
        IniciaConAscii(encabezado.Slice(8, 4), "WEBP");

    private static bool TieneFirmaMp4(ReadOnlySpan<byte> encabezado)
    {
        if (encabezado.Length < 12 || !IniciaConAscii(encabezado.Slice(4, 4), "ftyp"))
            return false;

        var marcaPrincipal = Encoding.ASCII.GetString(encabezado.Slice(8, 4));
        if (MarcasMp4.Contains(marcaPrincipal))
            return true;

        for (var indice = 16; indice + 4 <= encabezado.Length; indice += 4)
        {
            var marcaCompatible = Encoding.ASCII.GetString(encabezado.Slice(indice, 4));
            if (MarcasMp4.Contains(marcaCompatible))
                return true;
        }

        return false;
    }

    private static bool IniciaConAscii(ReadOnlySpan<byte> bytes, string valor)
    {
        if (bytes.Length < valor.Length)
            return false;

        for (var indice = 0; indice < valor.Length; indice++)
        {
            if (bytes[indice] != valor[indice])
                return false;
        }

        return true;
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
