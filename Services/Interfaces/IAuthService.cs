namespace Intranet.Services.Interfaces;

public interface IAuthService
{
    /// <summary>Genera un hash bcrypt de la contraseña proporcionada.</summary>
    string HashearPassword(string password);

    /// <summary>Verifica si una contraseña coincide con su hash bcrypt almacenado.</summary>
    bool VerificarPassword(string password, string hash);
}
