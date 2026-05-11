namespace Intranet.Models;

public class Aviso
{
    public int      Id                { get; set; }
    public string   Titulo            { get; set; } = string.Empty;
    public string?  Contenido         { get; set; }
    public DateTime FechaPublicacion  { get; set; }
    public bool     Activo            { get; set; } = true;
    public int      Orden             { get; set; }
}
