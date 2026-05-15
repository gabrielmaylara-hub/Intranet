RESTAURAR BASE SQL SERVER - INTRANET FGET
=========================================

Este restaurador permite crear/restaurar la base SQL Server de la Intranet FGET
desde la carpeta de entrega.

QUE ARCHIVO EJECUTAR
--------------------

EJECUTAR_ASISTENTE.bat
  Modo visual. Uselo primero para validar requisitos, probar conexion,
  restaurar, validar, levantar la app, validar sitio y generar reporte.

EJECUTAR_RESTAURACION_SQLSERVER.bat
  Modo consola. Uselo si prefiere ejecutar el proceso guiado desde terminal.

RESTAURAR_SQLSERVER_FGET.ps1
  Script tecnico principal. Uselo manualmente solo para diagnostico avanzado.

ASISTENTE_RESTAURACION_SQLSERVER.ps1
  Script usado por el asistente visual. Normalmente no se ejecuta directamente.

Advertencia:

- No ejecute todos los archivos.
- No mueva archivos sueltos.
- Conserve completa la estructura de la carpeta de entrega.

Puede copiar la carpeta de entrega a cualquier ubicacion, siempre que conserve
la estructura interna:

ENTREGA INTRANET SQLSERVER 2022 13-05-2026
  01_PROYECTO
  02_BASE_DE_DATOS_SQLSERVER
  03_STORAGE
  05_REPORTES_VALIDACION
  06_RESTAURAR_SQLSERVER

ARCHIVOS
--------

- RESTAURAR_SQLSERVER_FGET.ps1
- EJECUTAR_RESTAURACION_SQLSERVER.bat
- ASISTENTE_RESTAURACION_SQLSERVER.ps1
- EJECUTAR_ASISTENTE.bat
- README_RESTAURAR_SQLSERVER.txt

MODO RECOMENDADO CON ASISTENTE VISUAL
-------------------------------------

Para restaurar con ventana grafica:

1. Abrir la carpeta:

   06_RESTAURAR_SQLSERVER

2. Ejecutar con doble clic:

   EJECUTAR_ASISTENTE.bat

3. Revisar los valores sugeridos:

   Instancia SQL Server: .\SQLEXPRESS
   Base de datos: intranet_fget
   Puerto web: 5077

4. Elegir el tipo de autenticacion:

   - Windows Authentication
   - SQL Authentication

5. Si usa Windows Authentication, no se captura contrasena SQL.

6. Si usa SQL Authentication, capturar usuario y contrasena SQL. La contrasena
   no se muestra, no se guarda y no se escribe en reportes.

7. Usar los botones en este orden:

   1. Validar requisitos
   2. Probar conexion SQL Server
   3. Restaurar base
   4. Validar base
   5. Levantar app
   6. Validar sitio
   7. Abrir sitio
   8. Generar reporte

El asistente usa rutas relativas a su propia carpeta, por lo que la entrega
puede copiarse a otra ubicacion siempre que conserve la estructura interna.

Si no se detecta SDK compatible con .NET 10.0 para ejecutar desde codigo fuente,
el asistente puede
usar la app publicada en:

  09_APP_PUBLICADA

Para ejecutar 09_APP_PUBLICADA se requiere runtime .NET 10.0 o ASP.NET Core
Hosting Bundle compatible con .NET 10.0. Tener solo runtime .NET 8 no es
suficiente para esta app publicada.

Si el puerto 5077 esta ocupado, el asistente sugerira otro puerto disponible,
por ejemplo 5080, 5088 o 5090.

El reporte del asistente se genera en:

  05_REPORTES_VALIDACION\REPORTE_ASISTENTE_SQLSERVER.txt

ERRORES COMUNES
---------------

No se pudo conectar con .\SQLEXPRESS:
- Puede que SQL Server no este instalado.
- Puede que el servicio SQL Server este detenido.
- Puede que la instancia tenga otro nombre.
- Pruebe con localhost, ., NOMBREPC\SQLEXPRESS o SERVIDOR\INSTANCIA.

Timed out waiting for pipe:
- Es un error tecnico de conexion local.
- Normalmente significa que .\SQLEXPRESS no existe o no esta iniciado.
- La base no se modifica si falla en la prueba de conexion.

No se encontro sqlcmd:
- sqlcmd sirve para restaurar y validar, pero no instala SQL Server.
- La entrega incluye 08_PRERREQUISITOS\sqlcmd\sqlcmd.exe.
- Tambien puede restaurar manualmente con SSMS y luego validar con el asistente.

Usuario sin permisos:
- Use Windows Authentication con un usuario administrador de SQL Server, o SQL
  Authentication con permisos para crear/restaurar bases.

Puerto web ocupado:
- Use otro puerto, por ejemplo 5080, 5088 o 5090.

Falta runtime/SDK .NET 10:
- Desde codigo fuente se requiere SDK compatible con .NET 10.0.
- Para 09_APP_PUBLICADA se requiere runtime .NET 10.0 o ASP.NET Core Hosting
  Bundle compatible con .NET 10.0.
- Runtime .NET 8 no es suficiente.

Storage no encontrado:
- No borre Storage.
- Revise que carpeta se detecto en el asistente.
- Desde codigo fuente se usa 01_PROYECTO\Storage.
- Desde app publicada se usa 09_APP_PUBLICADA\Storage.
- 03_STORAGE\Storage puede servir como respaldo de entrega, pero no cambia la
  ruta usada por la app.

QUE ENVIAR A SOPORTE
--------------------

- Captura del error.
- Reporte generado en 05_REPORTES_VALIDACION.
- Instancia SQL usada.
- Tipo de autenticacion, sin contrasena.
- Puerto web.
- Archivo .bak usado.
- Ultimas lineas del diagnostico inferior del asistente.
- Confirmar si SQL Server 2022 esta instalado y si el servicio esta iniciado.

SI NO TIENE SQLCMD INSTALADO
----------------------------

El restaurador y el asistente buscan sqlcmd en este orden:

1. PATH del equipo.
2. Rutas comunes de Microsoft SQL Server.
3. sqlcmd incluido en la entrega:

   08_PRERREQUISITOS\sqlcmd\sqlcmd.exe

Importante: sqlcmd sirve para restaurar/validar, pero no instala el motor SQL
Server. SQL Server 2022 debe estar instalado aparte o disponible en una
instancia remota.

Si no se encuentra sqlcmd, hay tres opciones:

1. Instalar Microsoft sqlcmd / SQL Server Command Line Utilities.
2. Restaurar el archivo .bak manualmente desde SQL Server Management Studio
   (SSMS) y despues usar el asistente para Validar base, Levantar app y
   Validar sitio.
3. Indicar manualmente la ruta completa de sqlcmd.exe cuando el restaurador la
   solicite.

Para instalar sqlcmd desde fuente oficial:

  winget install sqlcmd

Tambien puede descargarse desde:

  https://github.com/microsoft/go-sqlcmd/releases

COMO EJECUTAR
-------------

Modo consola:

1. Abrir la carpeta:

   06_RESTAURAR_SQLSERVER

2. Ejecutar con doble clic:

   EJECUTAR_RESTAURACION_SQLSERVER.bat

3. El script mostrara:

   VALORES SUGERIDOS PARA LA RESTAURACION

   Instancia SQL Server : .\SQLEXPRESS
   Base de datos        : intranet_fget
   Puerto web           : 5077
   Proveedor            : SqlServer

   Estos valores se usaran para probar la conexion, restaurar la base y
   levantar la aplicacion.

   La base de datos todavia no sera modificada. La restauracion requiere
   confirmacion posterior.

   Desea continuar con estos valores?

4. Seleccione:

   [S] Si, usar estos valores y continuar
   [N] No, capturar otros valores

   Seleccione una opcion [S/N]:

5. Elija metodo de autenticacion:

   1. Windows Authentication
   2. SQL Authentication

WINDOWS AUTHENTICATION
----------------------

Si su usuario de Windows tiene permisos para crear/restaurar bases en SQL
Server, use Windows Authentication. En este modo no se requiere password SQL.

SQL AUTHENTICATION
------------------

Si usa SQL Authentication, el script pedira:

- usuario SQL Server
- contrasena SQL Server

La contrasena se lee de forma segura:

- no se muestra en pantalla,
- no se guarda en archivos,
- no se escribe en el reporte,
- no viene incluida en la entrega por seguridad.

RESPALDO
--------

El script detecta automaticamente un respaldo dentro de:

  02_BASE_DE_DATOS_SQLSERVER

Prioridad:

1. Si existe archivo .bak, restaura el .bak mas reciente.
2. Si no existe .bak pero existe .sql, ejecuta el .sql mas reciente.
3. Si no existe .bak ni .sql, se detiene con mensaje claro.

BASE EXISTENTE
--------------

Si la base destino ya existe, el script avisa:

  La base intranet_fget ya existe.

Para restaurar encima debe confirmar escribiendo:

  RESTAURAR

Si no confirma, el script se detiene sin modificar la base.

REPORTE
-------

Al terminar genera:

  05_REPORTES_VALIDACION\REPORTE_RESTAURACION_SQLSERVER.txt

El reporte incluye:

- fecha,
- instancia usada,
- base restaurada,
- tipo de autenticacion, sin contrasena,
- respaldo usado,
- resultado,
- conteos basicos,
- errores, si hubo.

TABLAS VALIDADAS DESPUES DE RESTAURAR
-------------------------------------

El asistente visual y el restaurador de consola validan la misma lista minima:

- usuarios_admin
- areas_publicacion
- configuracion_sitio
- accesos_rapidos
- sitio_enlaces
- archivos_seccion
- avisos
- tutoriales
- directorio_areas
- directorio

Si alguna tabla no existe, la validacion lo reporta como error para revisar
el respaldo o el script usado.

DETECCION DE NOMBRES LOGICOS DEL .BAK
-------------------------------------

Para restaurar un .bak, el restaurador lee los nombres logicos con
RESTORE FILELISTONLY. Si falla esa deteccion, revise que el .bak sea valido.
Tambien puede restaurar manualmente con SQL Server Management Studio (SSMS)
y luego usar el asistente para Validar base, Levantar app y Validar sitio.

PERMISOS
--------

Si aparece un error de permisos, use un usuario con permisos de restauracion,
por ejemplo un administrador de SQL Server, o solicite apoyo al DBA.

Si SQL Server no permite leer el archivo .bak, revise que el servicio de SQL
Server tenga permisos sobre la carpeta de entrega. Como alternativa, use un
respaldo .sql en la carpeta 02_BASE_DE_DATOS_SQLSERVER.

SQLCMD
------

El script requiere sqlcmd.exe. Lo busca automaticamente en PATH, rutas comunes
de Microsoft SQL Server y la carpeta:

  08_PRERREQUISITOS\sqlcmd\sqlcmd.exe

Si no lo encuentra, instale Microsoft sqlcmd o SQL Server Command Line
Utilities desde fuente oficial de Microsoft.

CONFIGURACION DE LA APP
-----------------------

Despues de restaurar, el script muestra valores para levantar la aplicacion:

  DatabaseProvider=SqlServer
  ASPNETCORE_URLS=http://127.0.0.1:5077

Para Windows Authentication, la cadena sugerida usa Trusted_Connection=True.

Para SQL Authentication, la cadena mostrara marcadores:

  User Id=<usuario>;Password=<capturar>

Capture la contrasena de forma segura en el entorno donde se levantara la
aplicacion. No guarde contrasenas en appsettings ni en este README.

SEGURIDAD
---------

- No hardcodear contrasenas.
- No compartir contrasenas en texto plano.
- Cambiar credenciales temporales despues de montar, si aplica.
- Mantener la carpeta Storage junto con el proyecto.
- No borrar Storage.
