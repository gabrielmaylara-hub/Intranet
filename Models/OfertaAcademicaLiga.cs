namespace Intranet.Models;

public class OfertaAcademicaLiga
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Url { get; set; } = string.Empty;
    public int Orden { get; set; }
    public bool Activa { get; set; } = true;
}
