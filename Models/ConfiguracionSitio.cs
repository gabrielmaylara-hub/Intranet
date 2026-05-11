namespace Intranet.Models;

public class ConfiguracionSitio
{
    public string  Clave       { get; set; } = string.Empty;
    public string? Valor       { get; set; }
    public string  Tipo        { get; set; } = "texto";
    public string? Descripcion { get; set; }
}
