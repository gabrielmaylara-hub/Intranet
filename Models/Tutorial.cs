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
    public int?     AreaPublicacionId { get; set; }
    public string?  AreaPublicacionNombre { get; set; }
    public int?     CreadoPorUsuarioId { get; set; }
    public int?     ActualizadoPorUsuarioId { get; set; }
    public DateTime? FechaActualizacion { get; set; }
    public DateTime FechaCreacion  { get; set; }
}
