namespace Intranet.Models;

public class Tutorial
{
    public int      Id             { get; set; }
    public string   Titulo         { get; set; } = string.Empty;
    public string?  Descripcion    { get; set; }
    public string?  ArchivoPath    { get; set; }
    public string?  MiniaturaPath  { get; set; }
    public int      Orden          { get; set; }
    public bool     Activo         { get; set; } = true;
    public DateTime FechaCreacion  { get; set; }
}
