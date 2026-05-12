using System.Text;
using Intranet.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Intranet.Tests;

public class ArchivoServiceTests
{
    [Fact]
    public async Task GuardarAsync_RechazaArchivoMayorAlLimiteConfigurado()
    {
        await using var laboratorio = CrearLaboratorio(maxFileSizeBytes: 4);
        var archivo = CrearArchivo([0x25, 0x50, 0x44, 0x46, 0x2D], "prueba.pdf", "application/pdf");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => laboratorio.Servicio.GuardarAsync(archivo, "archivos/formatos"));

        Assert.Contains("excede", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(ArchivosValidos))]
    public async Task GuardarAsync_AceptaContenidoConFirmaValida(
        byte[] contenido,
        string nombreArchivo,
        string contentType)
    {
        await using var laboratorio = CrearLaboratorio();
        var archivo = CrearArchivo(contenido, nombreArchivo, contentType);

        var ruta = await laboratorio.Servicio.GuardarAsync(archivo, "pruebas");
        var rutaFisica = laboratorio.Servicio.ObtenerRutaFisica(ruta);

        Assert.True(File.Exists(rutaFisica));
    }

    [Theory]
    [InlineData("falso.pdf", "application/pdf")]
    [InlineData("falso.png", "image/png")]
    [InlineData("falso.jpg", "image/jpeg")]
    [InlineData("falso.jpeg", "image/jpeg")]
    [InlineData("falso.webp", "image/webp")]
    [InlineData("falso.mp4", "video/mp4")]
    public async Task GuardarAsync_RechazaContenidoConFirmaInvalida(
        string nombreArchivo,
        string contentType)
    {
        await using var laboratorio = CrearLaboratorio();
        var archivo = CrearArchivo(Encoding.UTF8.GetBytes("contenido falso"), nombreArchivo, contentType);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => laboratorio.Servicio.GuardarAsync(archivo, "pruebas"));

        Assert.Contains("contenido", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(Directory.EnumerateFiles(laboratorio.StoragePath, "*", SearchOption.AllDirectories));
    }

    [Theory]
    [InlineData("falso.pdf", "image/png")]
    [InlineData("falso.png", "text/plain")]
    public async Task GuardarAsync_RechazaMimeDeclaradoIncompatible(
        string nombreArchivo,
        string contentType)
    {
        await using var laboratorio = CrearLaboratorio();
        var archivo = CrearArchivo(ContenidoPdfValido(), nombreArchivo, contentType);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => laboratorio.Servicio.GuardarAsync(archivo, "pruebas"));

        Assert.Contains("tipo declarado", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(Directory.EnumerateFiles(laboratorio.StoragePath, "*", SearchOption.AllDirectories));
    }

    [Theory]
    [InlineData("archivo.pdf.exe")]
    [InlineData("archivo.png.html")]
    [InlineData("archivo.jpg.aspx")]
    public async Task GuardarAsync_RechazaDobleExtensionPeligrosa(string nombreArchivo)
    {
        await using var laboratorio = CrearLaboratorio();
        var archivo = CrearArchivo(ContenidoPdfValido(), nombreArchivo, "application/pdf");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => laboratorio.Servicio.GuardarAsync(archivo, "pruebas"));

        Assert.Contains("no permitido", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GuardarAsync_AceptaSvgBenigno()
    {
        await using var laboratorio = CrearLaboratorio();
        var contenido = Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1 1\"></svg>");
        var archivo = CrearArchivo(contenido, "icono.svg", "image/svg+xml");

        var ruta = await laboratorio.Servicio.GuardarAsync(archivo, "pruebas");
        var rutaFisica = laboratorio.Servicio.ObtenerRutaFisica(ruta);

        Assert.True(File.Exists(rutaFisica));
    }

    [Fact]
    public async Task GuardarAsync_RechazaSvgConScript()
    {
        await using var laboratorio = CrearLaboratorio();
        var contenido = Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>");
        var archivo = CrearArchivo(contenido, "icono.svg", "image/svg+xml");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => laboratorio.Servicio.GuardarAsync(archivo, "pruebas"));

        Assert.Contains("SVG", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(Directory.EnumerateFiles(laboratorio.StoragePath, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task GuardarAsync_RechazaSubcarpetaConTraversal()
    {
        await using var laboratorio = CrearLaboratorio();
        var archivo = CrearArchivo(ContenidoPdfValido(), "documento.pdf", "application/pdf");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => laboratorio.Servicio.GuardarAsync(archivo, "../escape"));

        Assert.Contains("subcarpeta", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(Directory.EnumerateFiles(laboratorio.StoragePath, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Eliminar_NoBorraArchivosFueraDeStorage()
    {
        await using var laboratorio = CrearLaboratorio();
        var rutaExterna = Path.Combine(laboratorio.RootPath, "externo.txt");
        await File.WriteAllTextAsync(rutaExterna, "no borrar");

        laboratorio.Servicio.Eliminar("../externo.txt");

        Assert.True(File.Exists(rutaExterna));
    }

    public static TheoryData<byte[], string, string> ArchivosValidos() => new()
    {
        { ContenidoPdfValido(), "documento.pdf", "application/pdf" },
        { ContenidoPngValido(), "imagen.png", "image/png" },
        { ContenidoJpegValido(), "foto.jpg", "image/jpeg" },
        { ContenidoJpegValido(), "foto.jpeg", "image/jpeg" },
        { ContenidoWebpValido(), "imagen.webp", "image/webp" },
        { ContenidoMp4Valido(), "video.mp4", "video/mp4" }
    };

    private static LaboratorioArchivoService CrearLaboratorio(long maxFileSizeBytes = 524_288_000)
    {
        var raizTemporal = Path.Combine(Path.GetTempPath(), $"intranet-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(raizTemporal);

        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:RutaBase"] = "Storage",
                ["Uploads:MaxFileSizeBytes"] = maxFileSizeBytes.ToString()
            })
            .Build();

        var entorno = new TestWebHostEnvironment(raizTemporal);
        var servicio = new ArchivoService(entorno, configuracion);
        return new LaboratorioArchivoService(raizTemporal, servicio);
    }

    private static FormFile CrearArchivo(byte[] contenido, string nombreArchivo, string contentType)
    {
        var stream = new MemoryStream(contenido);
        return new FormFile(stream, 0, stream.Length, "archivo", nombreArchivo)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private static byte[] ContenidoPdfValido() =>
        Encoding.ASCII.GetBytes("%PDF-1.4\n% test\n");

    private static byte[] ContenidoPngValido() =>
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D
    ];

    private static byte[] ContenidoJpegValido() =>
    [
        0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46,
        0x49, 0x46, 0x00, 0x01
    ];

    private static byte[] ContenidoWebpValido() =>
    [
        0x52, 0x49, 0x46, 0x46, 0x0A, 0x00, 0x00, 0x00,
        0x57, 0x45, 0x42, 0x50
    ];

    private static byte[] ContenidoMp4Valido() =>
    [
        0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70,
        0x69, 0x73, 0x6F, 0x6D, 0x00, 0x00, 0x02, 0x00,
        0x69, 0x73, 0x6F, 0x6D, 0x69, 0x73, 0x6F, 0x32
    ];

    private sealed class LaboratorioArchivoService : IAsyncDisposable
    {
        private readonly string _rootPath;

        public LaboratorioArchivoService(string rootPath, ArchivoService servicio)
        {
            _rootPath = rootPath;
            Servicio = servicio;
            StoragePath = Path.Combine(rootPath, "Storage");
        }

        public ArchivoService Servicio { get; }
        public string RootPath => _rootPath;
        public string StoragePath { get; }

        public ValueTask DisposeAsync()
        {
            if (Directory.Exists(_rootPath))
                Directory.Delete(_rootPath, recursive: true);

            return ValueTask.CompletedTask;
        }
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public TestWebHostEnvironment(string contentRootPath)
        {
            ContentRootPath = contentRootPath;
            WebRootPath = Path.Combine(contentRootPath, "wwwroot");
            Directory.CreateDirectory(WebRootPath);
            ContentRootFileProvider = new PhysicalFileProvider(ContentRootPath);
            WebRootFileProvider = new PhysicalFileProvider(WebRootPath);
        }

        public string ApplicationName { get; set; } = "Intranet.Tests";
        public IFileProvider ContentRootFileProvider { get; set; }
        public string ContentRootPath { get; set; }
        public string EnvironmentName { get; set; } = "Development";
        public string WebRootPath { get; set; }
        public IFileProvider WebRootFileProvider { get; set; }
    }
}
