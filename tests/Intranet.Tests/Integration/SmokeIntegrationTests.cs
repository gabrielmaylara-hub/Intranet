using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Intranet.Tests.Integration;

public class SmokeIntegrationTests : IClassFixture<IntranetWebApplicationFactory>
{
    private readonly IntranetWebApplicationFactory _factory;

    public SmokeIntegrationTests(IntranetWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Home_RespondeOk()
    {
        using var cliente = CrearCliente();

        var respuesta = await cliente.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task AdminSinSesion_RedirigeALogin()
    {
        using var cliente = CrearCliente(permitirRedireccion: false);

        var respuesta = await cliente.GetAsync("/Admin");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Contains("/Admin/Login", respuesta.Headers.Location?.ToString());
    }

    [Fact]
    public async Task LoginAdmin_RespondeOk()
    {
        using var cliente = CrearCliente();

        var respuesta = await cliente.GetAsync("/Admin/Login");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Theory]
    [InlineData("/formatos")]
    [InlineData("/manuales")]
    [InlineData("/tutoriales")]
    public async Task PaginasPublicasPrincipales_RespondenOk(string ruta)
    {
        using var cliente = CrearCliente();

        var respuesta = await cliente.GetAsync(ruta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    private HttpClient CrearCliente(bool permitirRedireccion = true)
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = permitirRedireccion,
            BaseAddress = new Uri("https://localhost")
        });
    }
}
