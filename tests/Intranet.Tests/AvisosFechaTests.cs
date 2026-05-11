using System.Reflection;
using AvisosPage = Intranet.Pages.Admin.Avisos.IndexModel;

namespace Intranet.Tests;

public class AvisosFechaTests
{
    [Fact]
    public void TryParseFechaAviso_AceptaFechaIsoValida()
    {
        var resultado = TryParseFechaAviso("2026-05-11", out var fecha);

        Assert.True(resultado);
        Assert.Equal(new DateTime(2026, 5, 11), fecha);
    }

    [Theory]
    [InlineData("2026-99-99")]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParseFechaAviso_RechazaFechaInvalidaOSinValor(string? valor)
    {
        var resultado = TryParseFechaAviso(valor, out var fecha);

        Assert.False(resultado);
        Assert.Equal(default, fecha);
    }

    private static bool TryParseFechaAviso(string? valor, out DateTime fecha)
    {
        var metodo = typeof(AvisosPage).GetMethod(
            "TryParseFechaAviso",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(metodo);

        object?[] argumentos = [valor, default(DateTime)];
        var resultado = (bool)metodo.Invoke(null, argumentos)!;
        fecha = (DateTime)argumentos[1]!;
        return resultado;
    }
}
