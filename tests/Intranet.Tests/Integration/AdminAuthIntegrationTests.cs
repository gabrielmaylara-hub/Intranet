using System.Net;

namespace Intranet.Tests.Integration;

public class AdminAuthIntegrationTests : IClassFixture<IntranetWebApplicationFactory>
{
    private readonly IntegrationAuthHelper _auth;

    public AdminAuthIntegrationTests(IntranetWebApplicationFactory factory)
    {
        _auth = new IntegrationAuthHelper(factory);
    }

    [Fact]
    public async Task LoginAdmin_ConAntiforgery_RedireccionaYPermiteAdmin()
    {
        using var cliente = _auth.CrearCliente(permitirRedireccion: false);

        var loginGet = await cliente.GetAsync("/Admin/Login");
        Assert.Equal(HttpStatusCode.OK, loginGet.StatusCode);

        var loginPost = await _auth.LoginAsync(cliente);
        Assert.Equal(HttpStatusCode.Redirect, loginPost.StatusCode);
        Assert.Contains("/Admin", loginPost.Headers.Location?.ToString());

        var admin = await cliente.GetAsync("/Admin");
        Assert.Equal(HttpStatusCode.OK, admin.StatusCode);
    }

    [Fact]
    public async Task Logout_CierraSesion_YAdminVuelveARedirigir()
    {
        using var cliente = await _auth.CrearClienteAutenticadoAsync();

        var logout = await _auth.LogoutAsync(cliente);
        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        Assert.Contains("/Admin/Login", logout.Headers.Location?.ToString());

        var admin = await cliente.GetAsync("/Admin");
        Assert.Equal(HttpStatusCode.Redirect, admin.StatusCode);
        Assert.Contains("/Admin/Login", admin.Headers.Location?.ToString());
    }

    [Fact]
    public async Task LoginIncorrecto_NoAutentica_YMuestraMensajeGenerico()
    {
        using var cliente = _auth.CrearCliente(permitirRedireccion: false);

        var loginPost = await _auth.LoginAsync(cliente, credencialValida: false);
        Assert.Equal(HttpStatusCode.OK, loginPost.StatusCode);

        var contenido = await loginPost.Content.ReadAsStringAsync();
        Assert.Contains("Usuario o", contenido);

        var admin = await cliente.GetAsync("/Admin");
        Assert.Equal(HttpStatusCode.Redirect, admin.StatusCode);
        Assert.Contains("/Admin/Login", admin.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Login_RespetaReturnUrlInterno()
    {
        using var cliente = _auth.CrearCliente(permitirRedireccion: false);

        var loginPost = await _auth.LoginAsync(cliente, "/Admin/AccesosRapidos");

        Assert.Equal(HttpStatusCode.Redirect, loginPost.StatusCode);
        Assert.Equal("/Admin/AccesosRapidos", loginPost.Headers.Location?.ToString());

        var destino = await cliente.GetAsync(loginPost.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, destino.StatusCode);
    }
}
