using System.Reflection;
using AccesosPage = Intranet.Pages.Admin.AccesosRapidos.IndexModel;

namespace Intranet.Tests;

public class AccesosRapidosUrlTests
{
    [Theory]
    [InlineData("/Avisos")]
    [InlineData("/")]
    [InlineData("/Areas/Detalle/1")]
    [InlineData("https://www.tabasco.gob.mx")]
    [InlineData("http://example.com/recurso")]
    public void EsUrlAccesoValida_AceptaRutasInternasYHttp(string url)
    {
        Assert.True(EsUrlAccesoValida(url));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<h1>x</h1>")]
    [InlineData("vbscript:msgbox(1)")]
    [InlineData("file:///c:/windows/win.ini")]
    [InlineData("abc")]
    [InlineData("//evil.com")]
    [InlineData("")]
    [InlineData("   ")]
    public void EsUrlAccesoValida_RechazaUrlsPeligrosasOInvalidas(string? url)
    {
        Assert.False(EsUrlAccesoValida(url));
    }

    private static bool EsUrlAccesoValida(string? url)
    {
        var metodo = typeof(AccesosPage).GetMethod(
            "EsUrlAccesoValida",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(metodo);
        return (bool)metodo.Invoke(null, [url])!;
    }
}
