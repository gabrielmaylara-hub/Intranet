using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Intranet.Tests.Integration;

internal sealed class IntegrationAuthHelper
{
    private const string UsuarioAdmin = "admin";
    private static readonly string PasswordAdmin = string.Concat("Fget", "2025", "*");

    private readonly IntranetWebApplicationFactory _factory;

    public IntegrationAuthHelper(IntranetWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public HttpClient CrearCliente(bool permitirRedireccion = true)
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = permitirRedireccion,
            BaseAddress = new Uri("https://localhost")
        });
    }

    public async Task<HttpClient> CrearClienteAutenticadoAsync()
    {
        var cliente = CrearCliente(permitirRedireccion: false);
        await LoginAsync(cliente);
        return cliente;
    }

    public async Task<HttpResponseMessage> LoginAsync(
        HttpClient cliente,
        string? returnUrl = null,
        bool credencialValida = true)
    {
        var rutaLogin = string.IsNullOrWhiteSpace(returnUrl)
            ? "/Admin/Login"
            : $"/Admin/Login?ReturnUrl={Uri.EscapeDataString(returnUrl)}";

        var htmlLogin = await ObtenerHtmlAsync(cliente, rutaLogin);
        var token = ExtraerTokenAntiforgery(htmlLogin);

        var campos = new Dictionary<string, string>
        {
            ["Usuario"] = UsuarioAdmin,
            ["Password"] = credencialValida ? PasswordAdmin : "credencial_invalida_controlada",
            ["__RequestVerificationToken"] = token
        };

        if (!string.IsNullOrWhiteSpace(returnUrl))
            campos["ReturnUrl"] = returnUrl;

        return await cliente.PostAsync("/Admin/Login", new FormUrlEncodedContent(campos));
    }

    public async Task<HttpResponseMessage> LogoutAsync(HttpClient cliente)
    {
        var htmlAdmin = await ObtenerHtmlAsync(cliente, "/Admin");
        var token = ExtraerTokenAntiforgery(htmlAdmin);

        var campos = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        };

        return await cliente.PostAsync("/Admin/Login?handler=Salir", new FormUrlEncodedContent(campos));
    }

    public static string ExtraerTokenAntiforgery(string html)
    {
        var coincidencia = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"",
            RegexOptions.IgnoreCase);

        if (!coincidencia.Success)
        {
            coincidencia = Regex.Match(
                html,
                "value=\"([^\"]+)\"[^>]*name=\"__RequestVerificationToken\"",
                RegexOptions.IgnoreCase);
        }

        if (!coincidencia.Success)
            throw new InvalidOperationException("No se encontro el token antifalsificacion.");

        return WebUtility.HtmlDecode(coincidencia.Groups[1].Value);
    }

    private static async Task<string> ObtenerHtmlAsync(HttpClient cliente, string ruta)
    {
        var respuesta = await cliente.GetAsync(ruta);
        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadAsStringAsync();
    }
}
