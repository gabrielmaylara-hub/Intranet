# Setup local desde cero

Esta guia describe como levantar la Intranet FGET en una estacion de desarrollo limpia.

## 1. Requisitos

- Git.
- SDK de .NET 10.
- MySQL 8.x.
- Cliente `mysql` disponible en `PATH`, o una instalacion portable equivalente.

## 2. Stack de datos

- Motor real: MySQL.
- Base esperada: `intranet_fget`.
- Puerto esperado: `3306`.
- Charset recomendado: `utf8mb4`.
- Acceso a datos: Dapper.
- Provider ADO.NET: MySqlConnector.
- No usa Entity Framework ni `DbContext`.

## 3. Clonar y entrar al proyecto

```powershell
git clone URL_DEL_REPOSITORIO
cd Intranet
```

## 4. Configurar `appsettings.Development.json`

Revisar la clave:

```json
"ConnectionStrings": {
  "MySQL": "Server=localhost;Port=3306;Database=intranet_fget;User=USUARIO;Password=CONTRASENA_DEL_ENTORNO;CharSet=utf8mb4;"
}
```

No versionar contrasenas reales ni cadenas de conexion productivas. Cada entorno debe configurar sus propios valores.

## 5. Crear o inicializar la base de datos

El script principal es:

```text
Data/Scripts/init.sql
```

Ejecutar con el cliente MySQL:

```powershell
Get-Content Data/Scripts/init.sql -Raw |
    mysql --host=127.0.0.1 --port=3306 --user=USUARIO --password --default-character-set=utf8mb4
```

El script crea la base `intranet_fget`, tablas y datos iniciales. Las migraciones idempotentes posteriores se aplican al arrancar la aplicacion mediante `DbInicializador`.

## 6. Restaurar paquetes

```powershell
dotnet restore
```

## 7. Compilar

```powershell
dotnet build
```

## 8. Ejecutar localmente

```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --urls http://127.0.0.1:5077
```

URLs esperadas:

- Sitio publico: `http://127.0.0.1:5077/`
- Panel admin: `http://127.0.0.1:5077/Admin`
- Login admin: `http://127.0.0.1:5077/Admin/Login`

## 9. Usuario administrador inicial

Si no existen usuarios activos, la aplicacion crea un usuario administrador inicial durante el primer arranque. Revisar `README.md` y `Data/DbInicializador.cs` para el mecanismo exacto.

En un ambiente real, cambiar la credencial inicial despues del primer acceso administrativo.

## 10. Verificacion rapida

```powershell
Invoke-WebRequest http://127.0.0.1:5077/ -UseBasicParsing
Invoke-WebRequest http://127.0.0.1:5077/Admin/Login -UseBasicParsing
```

Ambas respuestas deben ser HTTP 200.
