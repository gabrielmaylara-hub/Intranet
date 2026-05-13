namespace Intranet.Models;

public class AreaPublicacion
{
    public int       Id                 { get; set; }
    public string    Nombre             { get; set; } = string.Empty;
    public string    Slug               { get; set; } = string.Empty;
    public string?   Descripcion        { get; set; }
    public int       Orden              { get; set; }
    public bool      Activa             { get; set; } = true;
    public DateTime  FechaCreacion      { get; set; }
    public DateTime? FechaActualizacion { get; set; }
}
