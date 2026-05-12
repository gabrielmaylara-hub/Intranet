using Dapper;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Intranet.Data;
using Intranet.Repositories;
using Intranet.Repositories.Interfaces;
using Intranet.Services;
using Intranet.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Dapper debe mapear columnas MySQL en snake_case a propiedades C# en PascalCase.
DefaultTypeMap.MatchNamesWithUnderscores = true;

const long maxFileSizeBytesPredeterminado = 524_288_000L;

var maxFileSizeBytes = builder.Configuration.GetValue<long?>(
    "Uploads:MaxFileSizeBytes") ?? maxFileSizeBytesPredeterminado;

if (maxFileSizeBytes <= 0)
    throw new InvalidOperationException("Uploads:MaxFileSizeBytes debe ser mayor que cero.");

// Límite de cuerpo HTTP para ejecución local con Kestrel.
builder.WebHost.ConfigureKestrel(opciones =>
{
    opciones.Limits.MaxRequestBodySize = maxFileSizeBytes;
});

// ─── Razor Pages: protege toda la carpeta /Admin excepto la página de login ──
builder.Services.AddRazorPages(opciones =>
{
    opciones.Conventions.AuthorizeFolder("/Admin");
    opciones.Conventions.AllowAnonymousToPage("/Admin/Login");
});

// ─── Autenticación por cookie de sesión (sin Identity) ───────────────────────
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opciones =>
    {
        opciones.LoginPath           = "/Admin/Login";
        opciones.LogoutPath          = "/Admin/Login";
        opciones.AccessDeniedPath    = "/Admin/Login";
        opciones.ExpireTimeSpan      = TimeSpan.FromHours(8);
        opciones.SlidingExpiration   = true;
        opciones.Cookie.HttpOnly     = true;
        opciones.Cookie.SameSite     = SameSiteMode.Strict;
        opciones.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

// ─── Antifalsificación: acepta token tanto en formulario como en cabecera HTTP ─
// Necesario para peticiones AJAX (reordenamiento con Sortable.js)
builder.Services.AddAntiforgery(opciones =>
{
    opciones.HeaderName = "X-XSRF-TOKEN";
});

// Límite de multipart alineado con el servicio de almacenamiento.
builder.Services.Configure<FormOptions>(opciones =>
{
    opciones.MultipartBodyLengthLimit = maxFileSizeBytes;
});

// Límite de cuerpo HTTP para despliegue in-process en IIS.
builder.Services.Configure<IISServerOptions>(opciones =>
{
    opciones.MaxRequestBodySize = maxFileSizeBytes;
});

// ─── Caché en memoria (configuración del sitio con TTL de 5 minutos) ─────────
builder.Services.AddMemoryCache();

// ─── Acceso a datos: singleton para reutilizar la cadena de conexión ─────────
builder.Services.AddSingleton<ConexionDb>();

// ─── Repositorios ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<IAccesoRapidoRepository,  AccesoRapidoRepository>();
builder.Services.AddScoped<IAvisoRepository,          AvisoRepository>();
builder.Services.AddScoped<ITutorialRepository,       TutorialRepository>();
builder.Services.AddScoped<IArchivoSeccionRepository, ArchivoSeccionRepository>();
builder.Services.AddScoped<IConfiguracionRepository,  ConfiguracionRepository>();
builder.Services.AddScoped<IUsuarioRepository,        UsuarioRepository>();
builder.Services.AddScoped<IDirectorioRepository,     DirectorioRepository>();

// ─── Servicios de negocio ─────────────────────────────────────────────────────
builder.Services.AddScoped<IArchivoService, ArchivoService>();
builder.Services.AddScoped<IAuthService,    AuthService>();
builder.Services.AddSingleton<ILoginAttemptService, LoginAttemptService>();

// ─── Inicializador: siembra el usuario admin si no existe ────────────────────
builder.Services.AddScoped<DbInicializador>();

var app = builder.Build();

var extensionesStoragePermitidas = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    ".pdf", ".png", ".svg", ".jpg", ".jpeg", ".webp", ".mp4"
};

var carpetaStorageConfigurada = app.Configuration["Storage:RutaBase"] ?? "Storage";
var baseStorage = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, carpetaStorageConfigurada));
Directory.CreateDirectory(baseStorage);
var proveedorStorage = new PhysicalFileProvider(baseStorage);
app.Lifetime.ApplicationStopping.Register(() => proveedorStorage.Dispose());

// ─── Sembrar datos iniciales al arrancar ──────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var inicializador = scope.ServiceProvider.GetRequiredService<DbInicializador>();
    await inicializador.InicializarAsync();
}

// ─── Pipeline de middlewares ──────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Archivos estáticos del desarrollador (CSS, JS, imágenes del sitio)
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health/live", () =>
    Results.Json(new { status = "ok" }))
    .AllowAnonymous();

app.MapGet("/health/ready", async (ConexionDb db) =>
{
    try
    {
        await using var con = db.CrearConexion();
        await con.OpenAsync();

        var resultado = await con.ExecuteScalarAsync<int>("SELECT 1");

        return resultado == 1
            ? Results.Json(new { status = "ready" })
            : Results.Json(
                new { status = "unready" },
                statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch
    {
        return Results.Json(
            new { status = "unready" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
})
    .AllowAnonymous();

// ─── Endpoint de Storage: sirve archivos fuera de wwwroot con control ────────
// Punto de extensión: agregar validaciones de sesión por sección aquí en el futuro.
IResult ServirArchivoStorage(string? ruta)
{
    if (string.IsNullOrWhiteSpace(ruta))
        return Results.NotFound();

    var rutaNormalizada = ruta.Replace('\\', '/').TrimStart('/');
    var rutaCompleta = Path.GetFullPath(Path.Combine(
        baseStorage,
        rutaNormalizada.Replace('/', Path.DirectorySeparatorChar)));

    // Prevención de path traversal: la ruta final siempre debe quedar dentro de Storage/.
    var baseStorageConSeparador = baseStorage.TrimEnd(
        Path.DirectorySeparatorChar,
        Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

    if (!rutaCompleta.StartsWith(baseStorageConSeparador, StringComparison.OrdinalIgnoreCase))
        return Results.Forbid();

    var extension = Path.GetExtension(rutaCompleta);
    if (!extensionesStoragePermitidas.Contains(extension))
        return Results.NotFound();

    var rutaRelativaProveedor = Path.GetRelativePath(baseStorage, rutaCompleta).Replace('\\', '/');
    var archivoInfo = proveedorStorage.GetFileInfo(rutaRelativaProveedor);

    if (!archivoInfo.Exists || archivoInfo.PhysicalPath is null)
        return Results.NotFound();

    // Resolución automática de tipo MIME por extensión.
    var proveedor = new FileExtensionContentTypeProvider();
    if (!proveedor.TryGetContentType(archivoInfo.PhysicalPath, out var tipoContenido))
        tipoContenido = "application/octet-stream";

    // enableRangeProcessing permite streaming de video con salto de posición.
    return Results.File(archivoInfo.PhysicalPath, tipoContenido, enableRangeProcessing: true);
}

app.MapGet("/storage/{**ruta}", ServirArchivoStorage).AllowAnonymous();

app.MapRazorPages();

app.Run();

public partial class Program { }
