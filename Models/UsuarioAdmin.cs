namespace Intranet.Models;

public class UsuarioAdmin
{
    public int    Id             { get; set; }
    public string Usuario        { get; set; } = string.Empty;
    public string PasswordHash   { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public bool   Activo         { get; set; } = true;
}
