namespace Intranet.Models;

public class SitioEnlace
{
    public int    Id     { get; set; }
    public string Grupo  { get; set; } = string.Empty;
    public string Texto  { get; set; } = string.Empty;
    public string Url    { get; set; } = string.Empty;
    public int    Orden  { get; set; }
    public bool   Activo { get; set; } = true;
}
