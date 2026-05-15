# Arquitectura tecnica de la Intranet

## Proposito del documento

Este documento resume la arquitectura tecnica de la aplicacion para un ingeniero responsable de despliegue, soporte o restauracion. El foco esta en el flujo ASP.NET Core Razor Pages, acceso a datos, almacenamiento local, health checks y tooling de entrega SQL Server 2022.

## Tipo de aplicacion

La Intranet es una aplicacion web ASP.NET Core Razor Pages con Target Framework `net10.0`.

No es una aplicacion Blazor. No hay componentes `.razor`, circuito SignalR de Blazor ni renderizado interactivo de componentes. La UI se entrega como paginas Razor tradicionales:

- `.cshtml`: markup HTML/Razor, formularios, tablas, enlaces y tag helpers.
- `.cshtml.cs`: clase `PageModel` asociada, donde viven los handlers HTTP, validaciones, llamadas a servicios/repositorios y preparacion del modelo para la vista.

El registro principal se realiza en `Program.cs` mediante `builder.Services.AddRazorPages()` y `app.MapRazorPages()`.

## Relacion entre `.cshtml` y `.cshtml.cs`

Cada pagina Razor puede tener un archivo `.cshtml.cs` con una clase `PageModel`. Por ejemplo:

- `Pages/Admin/Archivos/Index.cshtml`
- `Pages/Admin/Archivos/Index.cshtml.cs`

El archivo `.cshtml` declara `@page` y `@model`. El `@model` apunta al `PageModel` que expone propiedades para la vista, recibe datos de formularios con `[BindProperty]` y define handlers como `OnGetAsync`, `OnPostGuardarAsync` u otros handlers nombrados.

En formularios Razor, `asp-page-handler="Guardar"` invoca el handler `OnPostGuardarAsync`. Un GET normal a la pagina invoca `OnGet` u `OnGetAsync`.

## Flujo de una peticion HTTP

El flujo general es:

1. Kestrel o IIS recibe la peticion.
2. El pipeline configurado en `Program.cs` aplica HTTPS redirection, archivos estaticos, routing, autenticacion y autorizacion.
3. Si la ruta corresponde a una Razor Page, ASP.NET Core instancia el `PageModel` y resuelve sus dependencias desde el contenedor DI.
4. Se ejecuta el handler correspondiente:
   - `OnGet` / `OnGetAsync` para carga de pagina.
   - `OnPost` / `OnPost...Async` para formularios.
5. El `PageModel` llama servicios o repositorios.
6. Los repositorios crean conexiones mediante `ConexionDb` y ejecutan SQL con Dapper.
7. El handler devuelve `Page()`, `RedirectToPage(...)`, `File(...)`, `NotFound()`, `Forbid()` u otro resultado.
8. Razor renderiza el `.cshtml` con las propiedades preparadas por el `PageModel`.

## PageModels, servicios y repositorios

Los `PageModel` coordinan el caso de uso de la pagina. No deberian concentrar reglas transversales complejas cuando ya existe un servicio o repositorio apropiado.

Los servicios encapsulan logica de negocio o infraestructura local:

- `ArchivoService`: guarda y elimina archivos en `Storage`, valida extension, MIME declarado, firma de archivo y path traversal.
- `AuthService`: genera y verifica hashes bcrypt para contrasenas.
- `LoginAttemptService`: controla intentos de login en memoria.

Los repositorios encapsulan acceso a datos. Se registran como scoped en `Program.cs` y usan `ConexionDb` para abrir una conexion por operacion. Ejemplos:

- `ArchivoSeccionRepository`
- `AvisoRepository`
- `TutorialRepository`
- `ConfiguracionRepository`
- `UsuarioRepository`
- `DirectorioRepository`

## Dapper y acceso a datos

La aplicacion usa Dapper como micro-ORM. No usa Entity Framework.

En `Program.cs` se configura:

```csharp
DefaultTypeMap.MatchNamesWithUnderscores = true;
```

Esto permite mapear columnas `snake_case` de la base de datos a propiedades C# en `PascalCase`. Los repositorios usan metodos como:

- `QueryAsync<T>()`
- `QueryFirstOrDefaultAsync<T>()`
- `ExecuteAsync()`
- `ExecuteScalarAsync<T>()`

Las consultas usan parametros anonimos de Dapper, por ejemplo `new { id }`, para evitar concatenar valores de usuario directamente en SQL.

## Proveedor de base de datos

En el estado actual de `main`, la aplicacion esta configurada para MySQL mediante `MySqlConnector`.

El archivo `Intranet.csproj` referencia `MySqlConnector` y `Dapper`. La clase `ConexionDb` lee `ConnectionStrings:MySQL` y crea instancias de `MySqlConnection`.

Punto a confirmar antes de desplegar la aplicacion directamente sobre SQL Server: en `main` no se observa un selector runtime de proveedor MySQL/SQL Server ni una implementacion de `ConexionDb` que cree conexiones SQL Server. El tooling de entrega SQL Server 2022 documentado mas abajo permite generar paquetes y restauradores, pero no cambia por si mismo el proveedor de datos usado por el codigo de aplicacion en `main`.

## Funcion de `ConexionDb`

`ConexionDb` es una fabrica de conexiones. Se registra como singleton porque conserva la cadena ya preparada, pero no mantiene conexiones abiertas.

Responsabilidades actuales:

- Leer `ConnectionStrings:MySQL` desde configuracion.
- Construir una cadena MySQL con `AllowUserVariables = true`.
- Exponer `CrearConexion()` para que repositorios y endpoints creen una conexion nueva y cerrada por operacion.

Consideracion de despliegue: las credenciales reales no deben guardarse en archivos versionados. Deben provenir de variables de entorno, secretos del servidor o configuracion protegida del ambiente.

## Funcion de `DbInicializador`

`DbInicializador` se ejecuta al arrancar la aplicacion desde `Program.cs`.

Responsabilidades actuales:

- Abrir conexion a la base de datos.
- Aplicar un parche defensivo minimo para bases legadas en `AsegurarEstructuraAsync`.
- Aplicar migraciones versionadas desde `Data/Scripts/Migrations`.
- Registrar migraciones aplicadas en `schema_migrations`.
- Sembrar usuario administrador inicial solo si no hay usuarios activos y existe la variable de entorno `INTRANET_ADMIN_INITIAL_PASSWORD`.

Si la preparacion de base de datos falla, el arranque se detiene para evitar operar con una base inconsistente.

## Storage

La aplicacion usa una carpeta `Storage` fuera de `wwwroot` para archivos subidos o administrados desde el panel. La ruta base se configura con `Storage:RutaBase`; si no existe valor, se usa `Storage`.

`ArchivoService` administra escritura y eliminacion:

- Crea nombres unicos o sanitizados.
- Valida extensiones permitidas.
- Valida MIME declarado y firma real.
- Bloquea path traversal.
- Devuelve rutas relativas normalizadas con separador `/`.

`Program.cs` crea un `PhysicalFileProvider` sobre la carpeta Storage y define extensiones permitidas para servir archivos.

Storage masivo no debe versionarse. El repositorio debe conservar solamente estructura minima, por ejemplo `.gitkeep`, cuando aplique.

## Flujo de descargas y archivos servidos

Hay dos patrones principales:

- `/storage/{**ruta}`: sirve archivos por ruta relativa desde Storage, con validacion de extension, existencia fisica y path traversal. Usa `enableRangeProcessing` para permitir streaming, especialmente util en video.
- Endpoints de descarga por identificador:
  - `/descargar/archivo/{id:int}`
  - `/Admin/Archivos/Descargar/{id:int}`
  - `/descargar/tutorial/{id:int}`
  - `/descargar/aviso/{id:int}`
  - `/Admin/Tutoriales/Descargar/{id:int}`

Los endpoints por identificador consultan repositorios para validar que el registro exista y, cuando aplica, que este activo. Luego resuelven el archivo en Storage y devuelven un nombre de descarga sanitizado.

## Health checks

La aplicacion expone endpoints ligeros:

- `/health/live`: confirma que el proceso web esta levantado. No valida base de datos.
- `/health/ready`: abre conexion usando `ConexionDb` y ejecuta `SELECT 1`. Si la dependencia de base de datos no responde, devuelve estado `unready` con HTTP 503.

En el estado actual de `main`, readiness valida la base configurada por `ConexionDb`, es decir MySQL.

## Seguridad relevante para despliegue

Aspectos configurados en `Program.cs` y servicios:

- Autenticacion por cookie, sin ASP.NET Core Identity.
- `/Admin` protegido por autorizacion, excepto `/Admin/Login`.
- Cookies `HttpOnly` y `SameSite=Strict`.
- Antiforgery configurado con cabecera `X-XSRF-TOKEN` para formularios y AJAX.
- Limites de subida configurados en Kestrel, IIS y multipart mediante `Uploads:MaxFileSizeBytes`.
- Validaciones de archivos por extension, MIME y firma.
- Bloqueo de path traversal en lectura, escritura y eliminacion de archivos.
- Hash de contrasenas con bcrypt.
- Usuario administrador inicial solo mediante variable de entorno, sin password fijo en codigo.

No se deben imprimir ni registrar cadenas de conexion, contrasenas, tokens o hashes.

## Estructura de entrega SQL Server 2022

El proyecto contiene tooling para generar una carpeta de entrega SQL Server 2022 en:

```text
tools/entrega-sqlserver
```

El script principal es:

```text
tools/entrega-sqlserver/CREAR_ENTREGA_SQLSERVER.ps1
```

La estructura generada busca reproducir una entrega con estas carpetas:

- `01_PROYECTO`
- `02_BASE_DE_DATOS_SQLSERVER`
- `03_STORAGE`
- `04_DOCUMENTACION_ENTREGA`
- `05_REPORTES_VALIDACION`
- `06_RESTAURAR_SQLSERVER`
- `07_NOTAS_TECNICAS`
- `08_PRERREQUISITOS`
- `09_APP_PUBLICADA`

El generador copia el proyecto fuente excluyendo artefactos no versionables como `.git`, `bin`, `obj`, temporales, respaldos, paquetes comprimidos, credenciales y carpetas generadas. Tambien puede copiar un respaldo autorizado de base de datos, copiar Storage para la entrega, preparar documentacion minima, copiar restauradores y generar `09_APP_PUBLICADA` mediante `dotnet publish` cuando hay SDK compatible.

## Restauradores de `06_RESTAURAR_SQLSERVER`

Las plantillas de restauracion viven en:

```text
tools/entrega-sqlserver/06_RESTAURAR_SQLSERVER
```

Archivos principales:

- `ASISTENTE_RESTAURACION_SQLSERVER.ps1`
- `RESTAURAR_SQLSERVER_FGET.ps1`
- `EJECUTAR_ASISTENTE.bat`
- `EJECUTAR_RESTAURACION_SQLSERVER.bat`
- `README_RESTAURAR_SQLSERVER.txt`

Los `.bat` usan rutas relativas basadas en `%~dp0`, por lo que deben ejecutarse desde la carpeta completa `06_RESTAURAR_SQLSERVER`. Los scripts PowerShell usan rutas relativas y candidatos de herramientas como `sqlcmd` o `dotnet` para facilitar ejecucion en equipos de despliegue.

Consideracion operativa: los restauradores no deben contener contrasenas hardcodeadas. Si una entrega requiere credenciales, deben comunicarse por un canal seguro y no guardarse dentro del repositorio.

## Archivos que no deben versionarse

No deben entrar al control de versiones:

- Respaldos `.bak`.
- Archivos de base local `.mdf` y `.ldf`.
- Dumps o respaldos locales.
- Credenciales reales, `.env` con secretos, tokens o certificados privados.
- `Storage` masivo con archivos subidos por usuarios.
- `bin`, `obj`, `publish` y paquetes publicados.
- `09_APP_PUBLICADA` generada.
- Carpetas `ENTREGA...` generadas por tooling.
- Logs y reportes de validacion generados en pruebas locales.

## Validaciones recomendadas para despliegue

Antes de entregar o publicar:

1. Confirmar rama y commit exactos.
2. Revisar `git status` y asegurar que no haya cambios locales inesperados.
3. Ejecutar `git diff --check` antes de commitear cambios.
4. Ejecutar `dotnet build Intranet.sln` con SDK compatible con `net10.0`.
5. Confirmar que la cadena de conexion real se define por ambiente seguro.
6. Confirmar que la base de datos responde antes de validar readiness.
7. Validar `/health/live`.
8. Validar `/health/ready`.
9. Validar `/`.
10. Validar `/Admin/Login`.
11. Revisar que Storage exista y tenga permisos de lectura/escritura para la identidad del proceso web.
12. Confirmar que no se incluyeron respaldos, credenciales, Storage masivo, logs ni carpetas de entrega generadas.
13. Si se genera entrega SQL Server 2022, revisar `05_REPORTES_VALIDACION/REPORTE_EMPAQUETADO.txt`.
14. Si se restaura base SQL Server, ejecutar primero en ambiente de prueba y conservar evidencia operativa fuera del repositorio.

## Puntos a confirmar

- Si el despliegue final debe ejecutar la aplicacion directamente contra SQL Server 2022, confirmar la rama o version que contiene el proveedor SQL Server en runtime. En `main`, al momento de este documento, `ConexionDb` usa MySQL.
- Confirmar el procedimiento oficial de provisionamiento de base de datos para ambientes nuevos: migraciones MySQL desde `Data/Scripts/Migrations` o restauracion SQL Server desde una entrega generada.
- Confirmar politicas institucionales para resguardo y transferencia de respaldos y credenciales fuera de Git.
