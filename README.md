# Intranet FGET Tabasco

Proyecto ASP.NET Core Razor Pages para la intranet institucional de la Fiscalia General del Estado de Tabasco.

## Stack

- ASP.NET Core Razor Pages, .NET 10.
- MySQL 8.0 o superior.
- Dapper, sin Entity Framework.
- HTML, CSS y JavaScript puro.
- Sortable.js local para reordenamiento en el panel de administracion.
- Preparado para despliegue en IIS mediante `web.config`.

## Requisitos

- SDK de .NET 10.
- MySQL 8.0 o superior.
- Para IIS: ASP.NET Core Hosting Bundle compatible con .NET 10.

## Documentacion operativa

- `docs/SETUP.md`: instalacion local desde cero.
- `docs/TESTING.md`: ejecucion de pruebas unitarias e integracion.
- `docs/DEPLOY.md`: publicacion y despliegue en IIS.
- `docs/RUNBOOK.md`: operacion, respaldo, restore y errores comunes.
- `docs/BASE_DE_DATOS.md`: guia operativa de MySQL y versionado de esquema.
- `docs/ARCHIVOS_Y_SUBIDAS.md`: limites y comportamiento de archivos subidos.

## Estructura del proyecto

- `Pages/`: Razor Pages publicas y administrativas.
- `Models/`: modelos de dominio usados por vistas y repositorios.
- `Repositories/`: acceso a datos con Dapper.
- `Services/`: servicios de aplicacion, almacenamiento y seguridad.
- `Data/`: conexion, inicializador y scripts SQL.
- `Data/Scripts/`: script principal y migraciones idempotentes.
- `Storage/`: archivos subidos por administracion, fuera de `wwwroot`.
- `wwwroot/`: recursos estaticos publicos.
- `tests/Intranet.Tests/`: pruebas focales e integracion.
- `scripts/`: utilidades locales, incluyendo MySQL portable de desarrollo.

## Arranque local desde cero

1. Clonar el repositorio.
2. Crear una base de datos MySQL ejecutando `Data/Scripts/init.sql`:

```powershell
Get-Content Data/Scripts/init.sql -Raw |
    mysql --host=127.0.0.1 --port=3306 --user=USUARIO --password --default-character-set=utf8mb4
```

3. Revisar la cadena `ConnectionStrings:MySQL` en `appsettings.Development.json`.
4. Restaurar dependencias:

```powershell
dotnet restore
```

5. Compilar:

```powershell
dotnet build
```

6. Ejecutar en ambiente de desarrollo:

```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --urls http://127.0.0.1:5077
```

La vista publica queda disponible en `http://127.0.0.1:5077/`.
El panel administrativo queda disponible en `http://127.0.0.1:5077/Admin`.

Resumen de ejecucion local:

```powershell
dotnet restore
dotnet build
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --urls http://127.0.0.1:5077
```

No versionar contrasenas reales, cadenas de conexion productivas ni archivos `.env` con secretos. Cada entorno debe configurar sus credenciales fuera del control de versiones.

## Usuario de desarrollo

Al primer arranque, si no existen usuarios activos, la aplicacion siembra:

- Usuario: `admin`
- Contrasena: `Fget2025*`

La contrasena se guarda con hash BCrypt desde `DbInicializador`.

## Base de datos

El script principal esta en `Data/Scripts/init.sql` e incluye:

- Creacion de tablas.
- Configuracion inicial del sitio.
- Accesos rapidos iniciales.
- Estructura para avisos, tutoriales y archivos por seccion.

El usuario administrador tambien puede ser sembrado automaticamente por la aplicacion al primer arranque.

Las migraciones idempotentes viven en `Data/Scripts/Migrations/` y se registran en
`schema_migrations`. Consulta la guia operativa en `docs/BASE_DE_DATOS.md`.

## Almacenamiento

Los archivos subidos por el administrador viven fuera de `wwwroot`, en `Storage/`.
La ruta publica `/storage/{ruta}` sirve unicamente extensiones permitidas y valida que el archivo fisico permanezca dentro de esa carpeta.

El tamano maximo de subida se configura con `Uploads:MaxFileSizeBytes`. La guia operativa esta en `docs/ARCHIVOS_Y_SUBIDAS.md`.

Por seguridad, los archivos reales subidos por administracion no se versionan en Git. El repositorio conserva solo la estructura de carpetas mediante archivos `.gitkeep`.

## MySQL portable de desarrollo

La carpeta `.tools/` no se versiona. Si se instala un MySQL portable local, los scripts incluidos pueden ayudar a iniciarlo o detenerlo:

```powershell
.\scripts\start-mysql-dev.ps1
.\scripts\stop-mysql-dev.ps1
```

Valores usados por el entorno local actual:

- Servidor: `127.0.0.1`
- Puerto: `3306`
- Usuario: `root`
- Contrasena: configurada localmente, no versionar valores reales

Si se usa otra instalacion de MySQL, basta con ajustar `appsettings.Development.json`.

## Despliegue en IIS

1. Publicar la aplicacion:

```powershell
dotnet publish -c Release -o publish
```

2. Copiar el contenido de `publish/` al sitio configurado en IIS.
3. Ajustar `appsettings.json` con la cadena MySQL de produccion.
4. Verificar permisos de escritura para la carpeta `Storage/`.

En produccion se debe cambiar la contrasena inicial despues del primer acceso administrativo.
