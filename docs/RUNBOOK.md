# Runbook operativo

Guia rapida de operacion local, respaldo, restore y solucion de errores comunes.

## 1. Arranque local

Configurar ambiente y levantar la aplicacion:

```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --urls http://127.0.0.1:5077
```

Para exponerla en la red local durante una revision interna:

```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
$env:ASPNETCORE_URLS="http://0.0.0.0:5077"
dotnet run --no-launch-profile --urls http://0.0.0.0:5077
```

## 2. Parada local

Si se ejecuta en consola, detener con `Ctrl+C`.

Si se ejecuto como proceso separado, identificar el proceso que escucha el puerto:

```powershell
Get-NetTCPConnection -LocalPort 5077 -ErrorAction SilentlyContinue |
    Select-Object LocalAddress,LocalPort,State,OwningProcess
```

Detener solo el proceso correspondiente a esta aplicacion.

## 3. MySQL portable de desarrollo

Si existe una instalacion portable bajo `.tools/`, usar:

```powershell
.\scripts\start-mysql-dev.ps1
.\scripts\stop-mysql-dev.ps1
```

Los scripts esperan MySQL local en `127.0.0.1:3306`.

## 4. Backup de MySQL

Usar `mysqldump` sin escribir la contrasena en el comando:

```powershell
$env:MYSQL_PWD = "CONTRASENA_DEL_ENTORNO"
mysqldump --user=USUARIO --host=127.0.0.1 --port=3306 --single-transaction --routines --triggers --events --databases intranet_fget --result-file=.backups/mysql/intranet_fget_YYYYMMDD_HHMMSS.sql
Remove-Item Env:MYSQL_PWD
```

La carpeta `.backups/` esta ignorada por Git.

## 5. Restore basico de MySQL

Restaurar solo en ambiente autorizado y con respaldo previo:

```powershell
Get-Content .backups/mysql/ARCHIVO.sql -Raw |
    mysql --host=127.0.0.1 --port=3306 --user=USUARIO --password --default-character-set=utf8mb4
```

No restaurar sobre produccion sin ventana autorizada y plan de rollback.

## 6. Revision de logs

Revisar segun el entorno:

- Salida de consola de `dotnet run`.
- `logs/`, si existe en la instalacion.
- Event Viewer de Windows.
- Logs de IIS.

No compartir logs con contrasenas, cadenas de conexion ni datos personales.

## 7. Errores comunes

### MySQL no inicia

- Confirmar que existe `.tools/mysql/.../mysqld.exe` si se usa MySQL portable.
- Ejecutar `.\scripts\start-mysql-dev.ps1`.
- Revisar si el puerto `3306` ya esta ocupado.

### Puerto 3306 ocupado

```powershell
Get-NetTCPConnection -LocalPort 3306 -ErrorAction SilentlyContinue |
    Select-Object LocalAddress,LocalPort,State,OwningProcess
```

Si otro MySQL ya esta corriendo, ajustar `appsettings.Development.json` o detener el servicio correcto.

### Conexion fallida

- Revisar `ConnectionStrings:MySQL`.
- Validar host, puerto, base, usuario y permisos.
- Probar conexion con el cliente MySQL:

```powershell
mysql --host=127.0.0.1 --port=3306 --user=USUARIO --password --database=intranet_fget
```

### Base inexistente

Ejecutar el script principal:

```powershell
Get-Content Data/Scripts/init.sql -Raw |
    mysql --host=127.0.0.1 --port=3306 --user=USUARIO --password --default-character-set=utf8mb4
```

### Usuario sin permisos

El usuario configurado debe poder crear/leer/actualizar tablas de `intranet_fget` y ejecutar las migraciones idempotentes del arranque.

### Permisos de Storage/uploads

- Confirmar que existe `Storage/`.
- Confirmar permisos de escritura para la identidad que ejecuta la aplicacion.
- En IIS, revisar permisos del App Pool.

### Fallos de Testcontainers o Docker

- Verificar Docker:

```powershell
docker --version
docker ps
```

- Iniciar Docker Desktop.
- Repetir las pruebas de integracion.
- No apuntar las pruebas de integracion a la BD real `intranet_fget`.

## 8. Smoke test recomendado

```powershell
Invoke-WebRequest http://127.0.0.1:5077/ -UseBasicParsing
Invoke-WebRequest http://127.0.0.1:5077/Admin/Login -UseBasicParsing
```

Resultados esperados:

- `/`: HTTP 200.
- `/Admin/Login`: HTTP 200.
- `/Admin` sin sesion: redireccion a login.
