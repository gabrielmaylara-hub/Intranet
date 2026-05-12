# Pruebas

El proyecto tiene pruebas en:

```text
tests/Intranet.Tests/
```

Incluyen pruebas focales de servicios, validaciones y pruebas de integracion HTTP con `WebApplicationFactory`.

## 1. Requisitos

- SDK de .NET 10.
- Docker Desktop en ejecucion para pruebas de integracion.
- Acceso a internet o cache NuGet local para restaurar paquetes.

Las pruebas de integracion usan `Testcontainers.MySql` para levantar MySQL aislado. No deben usar la base real `intranet_fget`.

## 2. Restaurar y compilar

```powershell
dotnet restore
dotnet build
```

## 3. Ejecutar todas las pruebas

```powershell
dotnet test tests/Intranet.Tests/Intranet.Tests.csproj
```

## 4. Ejecutar pruebas focales por filtro

Pruebas de `ArchivoService`:

```powershell
dotnet test tests/Intranet.Tests/Intranet.Tests.csproj --filter "FullyQualifiedName~ArchivoService"
```

Pruebas de integracion:

```powershell
dotnet test tests/Intranet.Tests/Intranet.Tests.csproj --filter "FullyQualifiedName~Integration"
```

Pruebas generales del proyecto de pruebas:

```powershell
dotnet test tests/Intranet.Tests/Intranet.Tests.csproj --filter "FullyQualifiedName~Intranet.Tests"
```

## 5. Pruebas de integracion

Las pruebas de integracion usan:

- `Microsoft.AspNetCore.Mvc.Testing`.
- `Testcontainers.MySql`.
- MySQL aislado.
- Storage temporal.
- Ambiente `Testing`.

La factory de integracion sobrescribe la cadena `ConnectionStrings:MySQL` para apuntar al contenedor temporal. Al finalizar, el contenedor y el Storage temporal deben limpiarse.

## 6. Si Docker no esta disponible

Las pruebas unitarias/focales que no dependen de Testcontainers pueden ejecutarse con filtros especificos. Las pruebas de integracion fallaran si Docker no esta instalado o no esta corriendo.

Pasos sugeridos:

1. Verificar Docker:

```powershell
docker --version
docker ps
```

2. Iniciar Docker Desktop.
3. Repetir el comando de pruebas de integracion.

No reemplazar el laboratorio de integracion por la base real `intranet_fget`.

## 7. Salidas de prueba

No versionar:

- `bin/`
- `obj/`
- `TestResults/`
- archivos `.trx`
- logs temporales
