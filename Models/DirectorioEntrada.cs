namespace Intranet.Models;

public class DirectorioEntrada
{
    public int    Id        { get; set; }
    public string Area      { get; set; } = string.Empty;
    public string Nombre    { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public int    Orden     { get; set; }
    public bool   Activo    { get; set; } = true;
}
