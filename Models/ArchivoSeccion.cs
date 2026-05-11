namespace Intranet.Models;

public class ArchivoSeccion
{
    public int     Id          { get; set; }
    public string  Seccion     { get; set; } = string.Empty;
    public string  Nombre      { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string  ArchivoPath { get; set; } = string.Empty;
    public int     Orden       { get; set; }
    public bool    Activo      { get; set; } = true;
}
