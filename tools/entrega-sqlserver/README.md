# Herramientas de entrega SQL Server 2022

Esta carpeta permite reconstruir una entrega de Intranet FGET SQL Server 2022 desde el proyecto vivo, sin copiar manualmente la carpeta validada del escritorio.

## Uso rapido

Desde la raiz del repositorio:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\entrega-sqlserver\CREAR_ENTREGA_SQLSERVER.ps1 -DestinoBase "C:\Entregas" -RutaRespaldoBaseDatos "C:\Backups\intranet_fget_sqlserver.bak"
```

Para probar el empaquetado sin publicar la app:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\entrega-sqlserver\CREAR_ENTREGA_SQLSERVER.ps1 -DestinoBase "C:\Entregas" -NoPublish
```

## Estructura generada

- `01_PROYECTO`
- `02_BASE_DE_DATOS_SQLSERVER`
- `03_STORAGE`
- `04_DOCUMENTACION_ENTREGA`
- `05_REPORTES_VALIDACION`
- `06_RESTAURAR_SQLSERVER`
- `07_NOTAS_TECNICAS`
- `08_PRERREQUISITOS`
- `09_APP_PUBLICADA`

## Reglas de seguridad

- No versionar respaldos `.bak`, archivos `.mdf`/`.ldf`, dumps, paquetes publicados ni credenciales reales.
- Usar variables de entorno o configuración segura del servidor para contraseñas.
- La plantilla `templates/CREDENCIALES_BASE_SQLSERVER.template.txt` existe solo como guía operativa.
- Los scripts de `06_RESTAURAR_SQLSERVER` deben conservar rutas relativas para que la entrega pueda moverse completa.
