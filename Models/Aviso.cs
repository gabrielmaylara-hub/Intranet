namespace Intranet.Models;

public class Aviso
{
    public int      Id                { get; set; }
    public string   Titulo            { get; set; } = string.Empty;
    public string?  Contenido         { get; set; }
    public DateTime FechaPublicacion  { get; set; }
    public bool     Activo            { get; set; } = true;
    public int      Orden             { get; set; }
    public int?     AreaPublicacionId { get; set; }
    public string?  AreaPublicacionNombre { get; set; }
    public int?     CreadoPorUsuarioId { get; set; }
    public int?     ActualizadoPorUsuarioId { get; set; }
    public DateTime? FechaActualizacion { get; set; }
}
