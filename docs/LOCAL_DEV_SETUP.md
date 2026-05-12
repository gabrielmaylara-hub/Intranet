# Arranque local de desarrollo

Este documento describe el arranque local de la Intranet FGET en un entorno de desarrollo reproducible. No contiene contraseñas reales, cadenas de conexión completas ni datos productivos.

## 1. Requisitos locales

- .NET SDK compatible con el proyecto.
- Git.
- MySQL local o portable.
- PowerShell para ejecutar scripts locales.
- Acceso al repositorio en una carpeta de trabajo local.

## 2. Estructura local esperada

El entorno local puede usar herramientas y datos fuera del control de Git:

- `.tools/mysql/`: binarios locales de MySQL portable.
- `.tools/mysql-data`: datadir activo de MySQL local.
- `.tools/mysql-data.respaldo_*`: respaldos locales de datadir antes de reconstrucciones.
- `.backups/mysql/`: respaldos SQL locales.

Estas rutas estan ignoradas por Git y no deben incluirse en commits.

## 3. Base de datos local

- Motor esperado: MySQL.
- Base esperada: `intranet_fget`.
- Usuario local esperado para la aplicacion: `intranet_local`.

La contrasena del usuario local debe guardarse solo en un mecanismo local ignorado por Git, por ejemplo un archivo `.env.local`, `.env.intranet-local` o una variable de entorno de la sesion.

No escribas contrasenas reales en:

- `appsettings.json`
- `appsettings.Development.json`
- documentos versionados
- scripts versionados
- comentarios de codigo

## 4. Archivos que nunca deben subirse

No versionar:

- `.tools/`
- `.backups/`
- dumps SQL locales
- datadir de MySQL
- `.env`
- `.env.local`
- `.env.*.local`
- `.env.intranet-local`
- contrasenas
- cadenas de conexion reales
- respaldos con datos reales

## 5. Configuracion local de secretos

La aplicacion lee la cadena `ConnectionStrings:MySQL`. Para desarrollo local, configurala mediante una variable de entorno de sesion o un archivo local ignorado.

La configuracion debe incluir, sin versionarse:

- servidor local de MySQL
- puerto local de MySQL
- base `intranet_fget`
- usuario `intranet_local`
- contrasena local del entorno
- charset `utf8mb4`

No imprimas la contrasena en consola, capturas, logs o reportes.

## 6. Levantar MySQL local

Pasos generales:

1. Verificar que MySQL local no este ya escuchando en el puerto esperado.
2. Si se usa MySQL portable, confirmar que exista `.tools/mysql/`.
3. Confirmar que exista `.tools/mysql-data`.
4. Si el datadir no existe, reconstruirlo solo con respaldo o scripts autorizados.
5. Levantar MySQL con el script local disponible, por ejemplo `scripts/start-mysql-dev.ps1`, si aplica.
6. Validar que la base `intranet_fget` exista.
7. Validar que el usuario `intranet_local` tenga permisos sobre `intranet_fget`.

No ejecutes scripts destructivos contra una base con datos sin respaldo y autorizacion previa.

## 7. Levantar la aplicacion

Pasos generales:

1. Configurar la variable local `ConnectionStrings__MySQL` sin mostrar su valor.
2. Configurar la URL local de ASP.NET Core para escuchar en el puerto `5077`.
3. Ejecutar la aplicacion con `dotnet run` desde la raiz del proyecto.
4. Si hay una instancia anterior ocupando el puerto, detener solo el proceso local de esta aplicacion.

La aplicacion debe quedar disponible en:

- `http://127.0.0.1:5077/`

## 8. Validaciones minimas

Ejecutar las validaciones sin imprimir secretos:

- `http://127.0.0.1:5077/health/live` debe responder `200`.
- `http://127.0.0.1:5077/health/ready` debe responder `200` cuando MySQL este disponible.
- `http://127.0.0.1:5077/` debe responder `200`.
- `http://127.0.0.1:5077/Admin/Login` debe responder `200`.
- `http://127.0.0.1:5077/Admin` sin sesion debe redirigir a login.

Para validar login admin, usa las credenciales aplicativas autorizadas para el entorno local sin imprimir la contrasena en consola, logs o reportes.

## 9. Nota de seguridad

- No usar datos productivos en el entorno local.
- No imprimir secretos.
- No subir respaldos.
- No subir dumps.
- No subir datadir.
- No modificar `appsettings*.json` con secretos reales.
- No mezclar credenciales de MySQL con credenciales aplicativas.
- Si se comparte evidencia, redactar cualquier valor sensible.

## 10. Checklist final

Antes de dar por levantado el entorno local:

- [ ] MySQL local esta activo.
- [ ] La base `intranet_fget` existe.
- [ ] El usuario `intranet_local` puede acceder a la base.
- [ ] La cadena local esta configurada fuera de Git.
- [ ] La aplicacion escucha en `5077`.
- [ ] `/health/live` responde `200`.
- [ ] `/health/ready` responde `200`.
- [ ] `/` responde `200`.
- [ ] `/Admin/Login` responde `200`.
- [ ] `/Admin` sin sesion redirige a login.
- [ ] No se modificaron `appsettings*.json` con secretos.
- [ ] No hay archivos locales sensibles en `git status`.
