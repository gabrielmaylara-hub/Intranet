using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Testcontainers.MySql;

namespace Intranet.Tests.Integration;

public sealed class IntranetWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string TestDatabaseName = "intranet_fget_test";

    private readonly MySqlContainer _mysql;
    private readonly string _storagePath;
    private readonly string _storageRootPath;
    private readonly SemaphoreSlim _inicioLock = new(1, 1);
    private bool _iniciado;

    public IntranetWebApplicationFactory()
    {
        _storageRootPath = Path.Combine(Path.GetTempPath(), "intranet-fget-integration");
        _storagePath = Path.Combine(
            _storageRootPath,
            Guid.NewGuid().ToString("N"));

        _mysql = new MySqlBuilder("mysql:8.4")
            .WithDatabase(TestDatabaseName)
            .WithUsername("intranet_test")
            .WithPassword(Guid.NewGuid().ToString("N"))
            .Build();
    }

    public string StoragePath => _storagePath;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        AsegurarLaboratorioAsync().GetAwaiter().GetResult();

        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuracion) =>
        {
            configuracion.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MySQL"] = _mysql.GetConnectionString(),
                ["Storage:RutaBase"] = _storagePath,
                ["Logging:LogLevel:Default"] = "Warning",
                ["Logging:LogLevel:Microsoft.AspNetCore"] = "Warning"
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _mysql.DisposeAsync().GetAwaiter().GetResult();
        _inicioLock.Dispose();

        if (Directory.Exists(_storagePath))
            Directory.Delete(_storagePath, recursive: true);

        if (Directory.Exists(_storageRootPath) &&
            !Directory.EnumerateFileSystemEntries(_storageRootPath).Any())
        {
            Directory.Delete(_storageRootPath);
        }
    }

    private async Task AsegurarLaboratorioAsync()
    {
        if (_iniciado)
            return;

        await _inicioLock.WaitAsync();
        try
        {
            if (_iniciado)
                return;

            Directory.CreateDirectory(_storagePath);

            await _mysql.StartAsync();
            await AplicarScriptInicialAsync();

            _iniciado = true;
        }
        finally
        {
            _inicioLock.Release();
        }
    }

    private async Task AplicarScriptInicialAsync()
    {
        var rutaScript = Path.Combine(
            EncontrarRaizRepositorio(),
            "Data",
            "Scripts",
            "init.sql");

        var contenido = await File.ReadAllTextAsync(rutaScript);
        contenido = PrepararScriptInicialParaLaboratorio(contenido);

        await using var conexion = new MySqlConnection(_mysql.GetConnectionString());
        await conexion.OpenAsync();

        foreach (var sentencia in SepararSentenciasSql(contenido))
        {
            await using var comando = conexion.CreateCommand();
            comando.CommandText = sentencia;
            await comando.ExecuteNonQueryAsync();
        }
    }

    private static string PrepararScriptInicialParaLaboratorio(string contenido)
    {
        var nombreBaseProduccion = string.Concat("intranet", "_", "fget");

        return contenido
            .Replace(
                $"CREATE DATABASE IF NOT EXISTS {nombreBaseProduccion}",
                $"CREATE DATABASE IF NOT EXISTS {TestDatabaseName}",
                StringComparison.Ordinal)
            .Replace(
                $"USE {nombreBaseProduccion};",
                $"USE {TestDatabaseName};",
                StringComparison.Ordinal);
    }

    private static IEnumerable<string> SepararSentenciasSql(string contenido)
    {
        using var lector = new StringReader(contenido);
        var lineas = new List<string>();

        while (lector.ReadLine() is { } linea)
        {
            if (linea.TrimStart().StartsWith("--", StringComparison.Ordinal))
                continue;

            lineas.Add(linea);
        }

        return string.Join(Environment.NewLine, lineas)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => !string.IsNullOrWhiteSpace(s));
    }

    private static string EncontrarRaizRepositorio()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);

        while (directorio is not null)
        {
            if (File.Exists(Path.Combine(directorio.FullName, "Intranet.csproj")))
                return directorio.FullName;

            directorio = directorio.Parent;
        }

        throw new DirectoryNotFoundException("No se encontro la raiz del repositorio.");
    }
}
