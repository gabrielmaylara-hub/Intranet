namespace Intranet.Models;

public class UsuarioAdmin
{
    public int    Id             { get; set; }
    public string Usuario        { get; set; } = string.Empty;
    public string PasswordHash   { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public bool   Activo         { get; set; } = true;
    public string Rol            { get; set; } = "admin_general";
    public int?   AreaPublicacionId { get; set; }
    public string? AreaPublicacionNombre { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
    public DateTime? UltimoAcceso { get; set; }
}
