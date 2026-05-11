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
        var raizTemporal = Path.Combine(Path.GetTempPath(), $"intranet-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(raizTemporal);

        try
        {
            var configuracion = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Storage:RutaBase"] = "Storage",
                    ["Uploads:MaxFileSizeBytes"] = "4"
                })
                .Build();

            var entorno = new TestWebHostEnvironment(raizTemporal);
            var servicio = new ArchivoService(entorno, configuracion);
            await using var contenido = new MemoryStream(new byte[5]);
            var archivo = new FormFile(contenido, 0, contenido.Length, "archivo", "prueba.pdf");

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => servicio.GuardarAsync(archivo, "archivos/formatos"));

            Assert.Contains("excede", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(raizTemporal))
                Directory.Delete(raizTemporal, recursive: true);
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
