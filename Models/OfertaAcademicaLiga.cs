namespace Intranet.Models;

public class OfertaAcademicaLiga
{
    public const string ClaveConfiguracion = "capacitacion_ligas_json";
    public const string ClaveSemillaAplicada = "capacitacion_ligas_seeded";

    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Url { get; set; } = string.Empty;
    public int Orden { get; set; }
    public bool Activa { get; set; } = true;

    public static OfertaAcademicaLiga CrearSigaacej(
        IReadOnlyDictionary<string, string> config,
        int id,
        int orden)
    {
        return new OfertaAcademicaLiga
        {
            Id = id,
            Titulo = config.GetValueOrDefault(
                "pagina_capacitacion_externo_titulo",
                "Sistema Integral de Gestión Académica"),
            Descripcion = config.GetValueOrDefault(
                "pagina_capacitacion_externo_descripcion",
                "Accede al sistema SIGAACEJ del Tribunal Superior de Justicia del Estado de Tabasco para consultar la oferta académica institucional compartida."),
            Url = config.GetValueOrDefault(
                "pagina_capacitacion_externo_boton_url",
                "https://sigaacej.tsj-tabasco.gob.mx/"),
            Orden = orden,
            Activa = EsActivo(config.GetValueOrDefault("pagina_capacitacion_externo_activo", "1"))
        };
    }

    public static bool EsSigaacej(OfertaAcademicaLiga liga) =>
        liga.Titulo.Equals("Sistema Integral de Gestión Académica", StringComparison.OrdinalIgnoreCase) ||
        liga.Url.Contains("sigaacej.tsj-tabasco.gob.mx", StringComparison.OrdinalIgnoreCase);

    public static bool EsActivo(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ||
        valor.Equals("1", StringComparison.OrdinalIgnoreCase) ||
        valor.Equals("true", StringComparison.OrdinalIgnoreCase) ||
        valor.Equals("activo", StringComparison.OrdinalIgnoreCase);
}
