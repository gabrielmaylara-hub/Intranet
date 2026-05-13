using Intranet.Services.Interfaces;
using BC = BCrypt.Net.BCrypt;

namespace Intranet.Services;

/// <summary>
/// Servicio de autenticación con hashing bcrypt (factor de costo 12).
/// </summary>
public class AuthService : IAuthService
{
    public string HashearPassword(string password) =>
        BC.HashPassword(password, BC.GenerateSalt(12));

    public bool VerificarPassword(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            return BC.Verify(password, hash);
        }
        catch (Exception ex) when (
            ex is ArgumentException ||
            ex.GetType().Name.Contains("Salt", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
    }
}
