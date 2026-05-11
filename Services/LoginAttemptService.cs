using System.Security.Cryptography;
using System.Text;
using Intranet.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Intranet.Services;

public class LoginAttemptService : ILoginAttemptService
{
    private const int PermitLimitPredeterminado = 5;
    private const int WindowSecondsPredeterminado = 300;
    private const string PrefijoCache = "admin-login-attempts:";

    private readonly IMemoryCache _cache;
    private readonly int _permitLimit;
    private readonly int _windowSeconds;
    private readonly object _sync = new();

    public LoginAttemptService(IMemoryCache cache, IConfiguration configuration)
    {
        _cache = cache;

        _permitLimit = configuration.GetValue<int?>("Security:LoginRateLimit:PermitLimit")
            ?? PermitLimitPredeterminado;
        _windowSeconds = configuration.GetValue<int?>("Security:LoginRateLimit:WindowSeconds")
            ?? WindowSecondsPredeterminado;

        if (_permitLimit <= 0)
            _permitLimit = PermitLimitPredeterminado;

        if (_windowSeconds <= 0)
            _windowSeconds = WindowSecondsPredeterminado;
    }

    public bool EstaBloqueado(HttpContext httpContext, string? usuario)
    {
        var clave = CrearClave(httpContext, usuario);

        lock (_sync)
        {
            if (!_cache.TryGetValue<LoginAttemptState>(clave, out var estado))
                return false;

            if (estado.ExpiraUtc <= DateTimeOffset.UtcNow)
            {
                _cache.Remove(clave);
                return false;
            }

            return estado.Intentos >= _permitLimit;
        }
    }

    public void RegistrarFallo(HttpContext httpContext, string? usuario)
    {
        var clave = CrearClave(httpContext, usuario);
        var ahora = DateTimeOffset.UtcNow;

        lock (_sync)
        {
            if (!_cache.TryGetValue<LoginAttemptState>(clave, out var estado)
                || estado.ExpiraUtc <= ahora)
            {
                estado = new LoginAttemptState(0, ahora.AddSeconds(_windowSeconds));
            }

            var actualizado = estado with { Intentos = estado.Intentos + 1 };
            _cache.Set(clave, actualizado, actualizado.ExpiraUtc);
        }
    }

    public void RegistrarExito(HttpContext httpContext, string? usuario)
    {
        var clave = CrearClave(httpContext, usuario);

        lock (_sync)
        {
            _cache.Remove(clave);
        }
    }

    private static string CrearClave(HttpContext httpContext, string? usuario)
    {
        var direccionIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "ip-desconocida";
        var usuarioNormalizado = string.IsNullOrWhiteSpace(usuario)
            ? "usuario-vacio"
            : usuario.Trim().ToUpperInvariant();
        var valor = $"{direccionIp}|{usuarioNormalizado}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(valor));

        return PrefijoCache + Convert.ToHexString(hash);
    }

    private readonly record struct LoginAttemptState(int Intentos, DateTimeOffset ExpiraUtc);
}
