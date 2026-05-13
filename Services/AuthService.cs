using Intranet.Services.Interfaces;
using BC = BCrypt.Net.BCrypt;

namespace Intranet.Services;

/// <summary>
/// Servicio de autenticación con hashing bcrypt (factor de costo 12).
/// </summary>
public class AuthService : IAuthService
{
    // bcrypt ya incluye salt y costo dentro del hash. No guardar ni mostrar el
    // hash en logs; basta conservarlo en usuarios_admin.password_hash.
    public string HashearPassword(string password) =>
        BC.HashPassword(password, BC.GenerateSalt(12));

    public bool VerificarPassword(string password, string hash)
    {
        // Un hash nulo, vacio o corrupto no debe tumbar el login. Se rechaza
        // como credencial invalida para que el usuario vea un mensaje generico
        // y la app no exponga stack trace ni detalles de bcrypt.
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
