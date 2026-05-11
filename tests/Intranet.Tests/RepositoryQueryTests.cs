using System.Text.RegularExpressions;

namespace Intranet.Tests;

public class RepositoryQueryTests
{
    [Fact]
    public void Repositorios_NoDebenUsarSelectAsterisco()
    {
        var raiz = EncontrarRaizRepositorio();
        var archivos = Directory.EnumerateFiles(
            Path.Combine(raiz, "Repositories"),
            "*.cs",
            SearchOption.AllDirectories);

        var patron = new Regex("SELECT" + @"\s+\*", RegexOptions.IgnoreCase);
        var coincidencias = archivos
            .SelectMany(archivo => File.ReadLines(archivo)
                .Select((linea, indice) => new
                {
                    Archivo = Path.GetRelativePath(raiz, archivo),
                    Linea = indice + 1,
                    Texto = linea
                }))
            .Where(item => patron.IsMatch(item.Texto))
            .Select(item => $"{item.Archivo}:{item.Linea}")
            .ToList();

        Assert.Empty(coincidencias);
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
