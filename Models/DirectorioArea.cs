namespace Intranet.Models;

public class DirectorioArea
{
    public int     Id        { get; set; }
    public string  Nombre    { get; set; } = string.Empty;
    public string? Titular   { get; set; }
    public string? Ubicacion { get; set; }
    public string? Correo    { get; set; }
    public int     Orden     { get; set; }
    public bool    Activo    { get; set; } = true;
}
