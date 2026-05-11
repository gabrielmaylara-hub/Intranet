using Microsoft.AspNetCore.Http;

namespace Intranet.Services.Interfaces;

public interface ILoginAttemptService
{
    bool EstaBloqueado(HttpContext httpContext, string? usuario);
    void RegistrarFallo(HttpContext httpContext, string? usuario);
    void RegistrarExito(HttpContext httpContext, string? usuario);
}
