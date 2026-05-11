# Base de datos

## Motor

La intranet usa MySQL 8.0 o superior mediante MySqlConnector y Dapper.

## Configuracion

Las cadenas de conexion se definen en:

- `appsettings.json`
- `appsettings.Development.json`

La clave usada por la aplicacion es:

```text
ConnectionStrings:MySQL
```

La base local de desarrollo se llama `intranet_fget` y el puerto por defecto es `3306`.
No documentar ni versionar contrasenas reales; cada entorno debe configurar sus credenciales.

## Scripts

- `Data/Scripts/init.sql`: crea la base, tablas y datos iniciales para una instalacion nueva.
- `Data/Scripts/Migrations/*.sql`: migraciones idempotentes aplicadas al arranque por `DbInicializador`.

## Control de version de esquema

La tabla `schema_migrations` registra las migraciones aplicadas:

- `version`: identificador de la migracion.
- `name`: descripcion corta.
- `applied_at`: fecha de aplicacion.

La migracion `002` registra como baseline el estado actual conocido de las tablas existentes.

## Verificar conexion

Con MySQL disponible en el `PATH` o usando el cliente portable:

```powershell
mysql --host=127.0.0.1 --port=3306 --user=USUARIO --database=intranet_fget
```

Consulta de verificacion:

```sql
SELECT version, name, applied_at
FROM schema_migrations
ORDER BY version;
```

## Respaldar

Usar `mysqldump` sin imprimir contrasenas en consola. Ejemplo:

```powershell
$env:MYSQL_PWD = "CONTRASENA_DEL_ENTORNO"
mysqldump --user=USUARIO --host=127.0.0.1 --port=3306 --single-transaction --routines --triggers --events --databases intranet_fget --result-file=.backups/mysql/intranet_fget_YYYYMMDD_HHMMSS.sql
Remove-Item Env:MYSQL_PWD
```

La carpeta `.backups/` esta ignorada por Git.

## Advertencias

- `.tools/mysql-data` no se versiona en Git.
- No copiar la carpeta fisica de MySQL como respaldo principal mientras `mysqld` este en ejecucion.
- Para reconstruir desde cero, crear la BD con `Data/Scripts/init.sql` y luego arrancar la aplicacion para aplicar migraciones idempotentes.
