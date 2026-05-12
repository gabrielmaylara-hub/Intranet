# Despliegue en IIS

Esta guia describe un despliegue basico en IIS para la Intranet FGET.

## 1. Requisitos del servidor

- Windows Server o Windows con IIS habilitado.
- ASP.NET Core Hosting Bundle compatible con .NET 10.
- MySQL 8.x accesible desde el servidor web.
- Permisos para crear sitio, App Pool y carpeta de publicacion.

## 2. Publicar la aplicacion

Desde la raiz del proyecto:

```powershell
dotnet restore
dotnet publish -c Release -o publish
```

La carpeta `publish/` contiene los archivos que se copian al sitio IIS.

## 3. Preparar carpeta de publicacion

Ejemplo:

```text
C:\inetpub\IntranetFGET
```

Copiar el contenido de `publish/` a esa carpeta.

La identidad del App Pool debe tener permisos de lectura sobre la carpeta publicada y permisos de escritura sobre `Storage/` si los uploads se almacenan ahi.

## 4. App Pool

Configuracion sugerida:

- .NET CLR version: `No Managed Code`.
- Modo pipeline: Integrated.
- Identidad: cuenta de aplicacion o cuenta administrada con permisos minimos.

## 5. Crear sitio IIS

Configurar:

- Nombre del sitio: `Intranet FGET`.
- Ruta fisica: carpeta publicada.
- Binding: host/puerto definido por infraestructura.
- App Pool: el creado para la aplicacion.

## 6. Configuracion de produccion

Actualizar `appsettings.json` o usar variables de entorno/secretos del servidor para:

- `ConnectionStrings:MySQL`.
- `Storage:RutaBase`, si se usa una ruta distinta.
- `Uploads:MaxFileSizeBytes`, si cambia el limite operativo.

No versionar contrasenas reales ni cadenas productivas.

Si se cambia `Uploads:MaxFileSizeBytes`, mantener alineado `web.config` en:

```xml
<requestLimits maxAllowedContentLength="524288000" />
```

## 7. Base de datos

Inicializar MySQL con:

```powershell
Get-Content Data/Scripts/init.sql -Raw |
    mysql --host=HOST_MYSQL --port=3306 --user=USUARIO --password --default-character-set=utf8mb4
```

Al arrancar, la aplicacion ejecuta migraciones idempotentes desde `Data/Scripts/Migrations/` y registra el estado en `schema_migrations`.

No ejecutar scripts destructivos contra produccion sin respaldo y ventana autorizada.

## 8. Logs

Revisar:

- Logs configurados por IIS.
- Event Viewer de Windows.
- Carpeta `logs/` si el entorno la usa.
- `stdoutLogEnabled` en `web.config` solo para diagnostico temporal. Desactivarlo despues de resolver el problema.

No publicar logs con contrasenas, cadenas de conexion o datos sensibles.

## 9. Smoke test posterior al despliegue

Validar:

- `/` responde 200.
- `/Admin/Login` responde 200.
- `/Admin` sin sesion redirige a login.
- Login admin funciona.
- Carga de archivos escribe en `Storage/`.

## 10. Rollback basico

1. Mantener respaldo de la carpeta publicada anterior.
2. Mantener respaldo reciente de MySQL.
3. Si el despliegue falla, detener el sitio IIS.
4. Restaurar carpeta publicada anterior.
5. Revisar que `appsettings.json` y `Storage/` apunten al entorno correcto.
6. Iniciar el sitio y ejecutar smoke test.

Si hubo cambios de base de datos, evaluar rollback de BD con respaldo. No revertir manualmente tablas sin plan validado.
