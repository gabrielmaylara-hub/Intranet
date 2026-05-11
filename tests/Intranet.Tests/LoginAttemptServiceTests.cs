using System.Net;
using Intranet.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace Intranet.Tests;

public class LoginAttemptServiceTests
{
    [Fact]
    public void EstaBloqueado_BloqueaAlAlcanzarLimiteConfigurado()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var servicio = CrearServicio(cache, permitLimit: 3, windowSeconds: 300);
        var contexto = CrearContexto("127.0.0.1");

        servicio.RegistrarFallo(contexto, "admin");
        servicio.RegistrarFallo(contexto, "admin");

        Assert.False(servicio.EstaBloqueado(contexto, "admin"));

        servicio.RegistrarFallo(contexto, "admin");

        Assert.True(servicio.EstaBloqueado(contexto, "admin"));
    }

    [Fact]
    public void RegistrarExito_LimpiaIntentosFallidos()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var servicio = CrearServicio(cache, permitLimit: 2, windowSeconds: 300);
        var contexto = CrearContexto("127.0.0.1");

        servicio.RegistrarFallo(contexto, "admin");
        servicio.RegistrarFallo(contexto, "admin");
        Assert.True(servicio.EstaBloqueado(contexto, "admin"));

        servicio.RegistrarExito(contexto, "admin");

        Assert.False(servicio.EstaBloqueado(contexto, "admin"));
    }

    [Fact]
    public void Servicio_ManejaUsuarioVacioEIpAusente()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var servicio = CrearServicio(cache, permitLimit: 1, windowSeconds: 300);
        var contexto = CrearContexto(null);

        servicio.RegistrarFallo(contexto, null);

        Assert.True(servicio.EstaBloqueado(contexto, string.Empty));
    }

    [Fact]
    public async Task EstaBloqueado_ExpiraIntentosPorVentana()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var servicio = CrearServicio(cache, permitLimit: 1, windowSeconds: 1);
        var contexto = CrearContexto("127.0.0.1");

        servicio.RegistrarFallo(contexto, "admin");
        Assert.True(servicio.EstaBloqueado(contexto, "admin"));

        await Task.Delay(TimeSpan.FromMilliseconds(1500));

        Assert.False(servicio.EstaBloqueado(contexto, "admin"));
    }

    private static LoginAttemptService CrearServicio(
        IMemoryCache cache,
        int permitLimit,
        int windowSeconds)
    {
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:LoginRateLimit:PermitLimit"] = permitLimit.ToString(),
                ["Security:LoginRateLimit:WindowSeconds"] = windowSeconds.ToString()
            })
            .Build();

        return new LoginAttemptService(cache, configuracion);
    }

    private static DefaultHttpContext CrearContexto(string? ip)
    {
        var contexto = new DefaultHttpContext();
        contexto.Connection.RemoteIpAddress = ip is null
            ? null
            : IPAddress.Parse(ip);
        return contexto;
    }
}
