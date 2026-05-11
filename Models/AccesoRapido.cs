namespace Intranet.Models;

public class AccesoRapido
{
    public int     Id               { get; set; }
    public string  Nombre           { get; set; } = string.Empty;
    public string  Url              { get; set; } = string.Empty;
    public string? IconoPath        { get; set; }
    public string? BannerPath       { get; set; }
    public int     Orden            { get; set; }
    public bool    AbreNuevaVentana { get; set; } = true;
    public bool    Activo           { get; set; } = true;
}
