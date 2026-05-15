# Checklist de validacion de entrega

- Confirmar que la entrega contiene las 9 carpetas esperadas.
- Confirmar que `01_PROYECTO` no contiene `.git`, `bin`, `obj`, `.tools`, `.backups`, temporales ni credenciales.
- Confirmar que `02_BASE_DE_DATOS_SQLSERVER` contiene el respaldo `.bak` o script `.sql` autorizado.
- Confirmar que `03_STORAGE` contiene solo contenido necesario para ejecucion/restauracion.
- Confirmar que `06_RESTAURAR_SQLSERVER` contiene los scripts `.ps1` y `.bat` esperados.
- Confirmar que `09_APP_PUBLICADA` fue generado con `dotnet publish`, o documentar por que se omitio.
- Revisar `05_REPORTES_VALIDACION/REPORTE_EMPAQUETADO.txt`.
- Ejecutar restauracion en ambiente de prueba antes de entregar a operacion.
- Verificar `/health/live`, `/health/ready`, `/` y `/Admin/Login` en la app publicada.
- Confirmar que no se incluyeron contrasenas reales ni archivos de credenciales.
