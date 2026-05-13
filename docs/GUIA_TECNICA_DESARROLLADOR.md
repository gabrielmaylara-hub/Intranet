# Guia tecnica para desarrollador

## Proposito

Esta guia resume las partes delicadas de la Intranet FGET para que un desarrollador pueda dar mantenimiento sin romper el arranque, la base de datos, la seguridad del Admin, el Directorio, la configuracion publica ni los uploads.

No sustituye las pruebas. Antes de entregar cualquier cambio, ejecuta build, smoke test y validaciones manuales de las rutas principales.

## Arquitectura general

La aplicacion es una intranet ASP.NET Core Razor Pages con MySQL. La logica esta separada en:

- `Pages/`: vistas publicas y paginas Admin.
- `Models/`: modelos simples que reflejan tablas o datos de formulario.
- `Repositories/`: acceso a datos con consultas parametrizadas.
- `Services/`: reglas transversales como autenticacion y archivos.
- `Data/`: conexion MySQL, inicializacion y migraciones.
- `wwwroot/`: CSS, JS y assets publicos.
- `Storage/`: archivos subidos por el sistema. No se versiona.

## Flujo de arranque

`Program.cs` registra servicios, autenticacion, autorizacion, health checks, rutas Razor Pages y el endpoint controlado para servir archivos desde `Storage/`.

Durante el arranque se ejecuta `DbInicializador.InicializarAsync()`. Ese proceso garantiza estructura minima, aplica migraciones pendientes y deja seeds basicos cuando corresponde. Si la BD esta vacia, la aplicacion debe poder recrearla usando solo archivos versionados.

## Flujo de migraciones

Las migraciones viven en `Data/Scripts/Migrations/` y se ejecutan en orden alfabetico por prefijo numerico, por ejemplo `001`, `002`, `003`.

La tabla `schema_migrations` es la fuente de verdad. El runner consulta esa tabla y solo ejecuta migraciones pendientes. Esto evita que una migracion futura no idempotente se vuelva a ejecutar en cada arranque.

Reglas importantes:

- No edites una migracion ya aplicada para corregir produccion o una BD existente.
- Crea una migracion nueva con el siguiente numero.
- Mantén las migraciones sin `USE intranet_fget`; la app puede apuntar a otra base.
- No borres ni trunques datos sin autorizacion expresa.
- Si agregas tablas o seeds necesarios para una BD vacia, valida recreacion desde cero.

## Configuracion local

La cadena MySQL debe venir de configuracion local no versionada, normalmente `ConnectionStrings__MySQL` en la sesion o en un archivo ignorado por Git.

No guardes secretos reales en:

- `appsettings.json`
- `appsettings.Development.json`
- documentos versionados
- migraciones
- scripts compartidos

## Credenciales temporales y variables de entorno

El administrador inicial no tiene contrasena fija en codigo. Si la tabla `usuarios_admin` esta vacia, el bootstrap usa:

```text
INTRANET_ADMIN_INITIAL_PASSWORD
```

Si esa variable no existe, la app arranca pero no crea el usuario inicial. Esto evita credenciales embebidas.

Para smoke test local con login se usan:

```text
INTRANET_SMOKE_ADMIN_USER
INTRANET_SMOKE_ADMIN_PASSWORD
```

No imprimas esos valores ni los subas a Git.

## Modulos principales

- Home: renderiza textos configurables, accesos rapidos, avisos publicados, tutoriales y footer.
- Directorio publico: muestra extensiones agrupadas por area y permite busqueda.
- Admin Directorio: administra areas, extensiones, importacion CSV y orden visual.
- Admin Configuracion: administra identidad, Home, menu superior, paginas publicas, footer, colores y logo.
- Archivos: administra recursos descargables o multimedia por seccion.
- Auth: login, logout y cambio de contrasena propia.
- Health: endpoints tecnicos `/health/live` y `/health/ready`.

## Reglas criticas del Directorio

Dentro de una misma area:

- No debe repetirse el mismo nombre.
- No debe repetirse la misma extension.
- No debe repetirse el mismo orden interno.

El orden interno aplica solo dentro del area. Puede repetirse en areas distintas.

El Admin permite reordenar extensiones arrastrando dentro de la misma area. El backend valida que todos los IDs enviados pertenezcan a esa area antes de guardar. El repositorio usa ordenes temporales negativos antes de asignar `1..N` para evitar choques con el indice unico de area + orden.

## Importacion CSV del Directorio

La plantilla visible pide:

```text
Area,Nombre,Extension,Titular,Ubicacion,Correo
```

`Orden` y `Activo` siguen siendo compatibles para CSV legados, pero no son obligatorios.

Reglas:

- El area debe existir previamente en Datos por area.
- La importacion primero previsualiza y no escribe datos.
- Si hay errores o conflictos, se bloquea confirmar.
- Si falta orden, el sistema asigna el siguiente disponible dentro del area.
- Si falta activo, la extension queda activa por defecto.

## Usuarios por area y areas de publicacion

`areas_publicacion` es un catalogo editorial para permisos de publicacion. Es distinto de `directorio_areas`, que pertenece al Directorio telefonico y no debe usarse como fuente de permisos.

Roles base:

- `admin_general`: administra todo el panel.
- `usuario_area`: administra directamente el contenido habilitado de su propia area.

Reglas de esta fase:

- Cada usuario de area pertenece a una sola area de publicacion.
- `admin_general` puede no tener area asignada.
- No borrar areas con usuarios asociados; usar desactivar.
- Avisos ya aplica permisos por area. Tutoriales y Archivos siguen pendientes para fases posteriores.
- La administracion de usuarios vive en `/Admin/Usuarios` y solo debe verla `admin_general`.
- No desactives ni cambies el rol del ultimo `admin_general` activo; el backend lo bloquea para evitar dejar el panel sin acceso.
- Para crear o resetear un usuario se captura una contrasena temporal desde Admin. El valor no se muestra despues de guardar y solo se conserva el hash.
- `usuario_area` no debe acceder a Configuracion, Usuarios ni Areas de publicacion; la proteccion debe existir en backend, no solo en el menu.

### Avisos por area

La tabla `avisos` tiene `area_publicacion_id`, `creado_por_usuario_id`, `actualizado_por_usuario_id` y `fecha_actualizacion`.

Reglas:

- `admin_general` puede ver y administrar todos los avisos.
- `usuario_area` solo puede ver y operar avisos con su mismo `area_publicacion_id`.
- Los avisos historicos sin area quedan visibles y administrables solo para `admin_general`.
- Al crear un aviso, `usuario_area` no elige area: el backend asigna automaticamente la suya.
- Al editar, publicar, desactivar o eliminar, el backend vuelve a validar el area del aviso para bloquear manipulacion de IDs.
- No apliques esta regla todavia a Tutoriales sin una migracion y validaciones equivalentes.

Areas semilla oficiales:

- Contraloria
- Direccion de Asuntos Juridicos
- Direccion de Recursos Humanos y Financieros
- Direccion General de Desarrollo y Evaluacion Institucional
- Direccion General Administrativa
- Visitaduria
- Escuela de la Fiscalia
- Direccion de Cultura
- Direccion General de Delitos Comunes
- Despacho

## Configuracion publica

Los textos visibles de Header, Home, paginas publicas y Footer se guardan en `configuracion_sitio`. Los enlaces configurables se guardan por grupo en `sitio_enlaces`.

No uses el texto visible como clave tecnica. Las claves de `configuracion_sitio` y los grupos de `sitio_enlaces` deben mantenerse alineados entre migraciones, repositorio y Razor.

La pantalla Admin Configuracion usa POST-Redirect-GET para evitar el reenvio de formulario al actualizar despues de guardar.

## Storage y uploads

`Storage/` contiene archivos locales necesarios para que la instalacion se vea igual, pero no se versiona en Git.

Los archivos subidos se validan por:

- extension permitida
- MIME declarado
- firma real del archivo cuando aplica
- tamano maximo
- ruta dentro de `Storage/`
- bloqueo de path traversal

No sirvas archivos subidos directamente desde `wwwroot` si ya pasan por el endpoint controlado `/storage/{ruta}`.

## Health checks

- `/health/live`: responde si la app esta levantada. No valida dependencias.
- `/health/ready`: valida que dependencias minimas, como MySQL, esten disponibles.

Ningun endpoint de health debe exponer cadenas de conexion, rutas internas sensibles ni secretos.

## Restaurar BD

Usa dumps con charset `utf8mb4`. Para restaurar, usa redireccion directa del cliente MySQL, no tuberias de texto:

```powershell
mysql --host=127.0.0.1 --port=3306 --user=<usuario> --default-character-set=utf8mb4 <base> < BaseDatos\intranet_fget_entrega.sql
```

No uses:

```powershell
Get-Content archivo.sql | mysql
```

Ese patron puede romper UTF-8.

## Validaciones recomendadas

Compilacion:

```powershell
C:\Recuperacion\tools\dotnet10\dotnet.exe build
```

Smoke test local:

```powershell
.\scripts\smoke-local.ps1
```

Rutas minimas:

- `/health/live`
- `/health/ready`
- `/`
- `/Directorio`
- `/Admin/Login`
- `/Admin/Directorio`
- `/Admin/Configuracion`

## Carpetas y archivos que no deben versionarse

- `.env`
- `.env.local`
- `.env.*.local`
- `.env.intranet-local`
- `.tools/mysql-data`
- `.tools/mysql-data.respaldo_*`
- `.backups/`
- dumps SQL temporales
- `bin/`
- `obj/`
- `.vs/`
- archivos reales dentro de `Storage/`
- logs locales

## Que no debe tocarse sin respaldo

- datadir local de MySQL
- dumps de entrega validados
- migraciones ya aplicadas
- hashes o usuarios admin
- reglas de unicidad del Directorio
- validaciones de upload
- configuracion con secretos locales
- assets de `Storage/` usados por la entrega USB

Antes de tocar cualquiera de esos puntos, crea respaldo, documenta el objetivo y valida que la app siga arrancando contra una base restaurada.
