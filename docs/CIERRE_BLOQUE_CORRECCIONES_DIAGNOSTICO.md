# Cierre técnico del bloque de correcciones del diagnóstico

## Objetivo del bloque

Este bloque corrigió hallazgos técnicos, de seguridad, consistencia y mantenibilidad detectados en el diagnóstico inicial del proyecto Intranet FGET. Las correcciones se aplicaron por microfases para mantener trazabilidad, reducir riesgo operativo y validar cada cambio antes de continuar.

## Estado final de Git

- Rama: main
- Estado: sincronizado con origin/main
- Dirty: No
- Último commit: 064321c test: agrega pruebas focales de seguridad y consistencia

## Validaciones finales

- git diff --check: OK
- dotnet build -p:UseAppHost=false: OK
- dotnet test focal: 24 superadas, 0 fallidas

Smoke local:

- /: 200
- /Admin sin sesión: 302 a login
- /Admin/Login GET: 200
- login admin: OK
- /Admin autenticado: 200

## Resumen de microfases

| Microfase | Commit | Riesgo atendido | Estado |
|---|---|---|---|
| 1 | 64652ff chore(db): agrega versionado minimo de base de datos | Respaldo, schema_migrations y baseline | Cerrada |
| 2 | f83e69e fix(accesos): valida urls y reordenamiento | URLs peligrosas y falso éxito en reordenamiento | Cerrada |
| 3 | 4c5ac2d fix(accesos): asegura consistencia entre archivos y bd | Consistencia archivo físico / BD en accesos rápidos | Cerrada |
| 4 | f402115 fix(tutoriales): asegura consistencia entre archivos y bd | Consistencia archivo físico / BD en tutoriales | Cerrada |
| 5 | bda2aee fix(archivos): asegura consistencia entre archivos y bd | Consistencia archivo físico / BD en archivos PDF | Cerrada |
| 6 | 5717f21 chore(upload): alinea limite de subida | Límites de subida desalineados | Cerrada |
| 7 | 7949845 fix(avisos): valida fecha de publicacion | Fecha inválida con fallback silencioso a hoy | Cerrada |
| 8 | 2479ded security(login): limita intentos de acceso | Fuerza bruta en login administrativo | Cerrada |
| 9 | 7696466 refactor(data): usa columnas explicitas en consultas | Uso de SELECT * | Cerrada |
| 10 | 064321c test: agrega pruebas focales de seguridad y consistencia | Falta de pruebas focales automatizadas | Cerrada |

## Riesgos corregidos

- La base de datos cuenta con respaldo, baseline y versionado mínimo.
- Las URLs de accesos rápidos se validan contra esquemas peligrosos.
- El reordenamiento ya no muestra éxito falso.
- Los flujos de archivos físicos y BD fueron protegidos en accesos rápidos, tutoriales y archivos/PDF.
- El límite de subida quedó centralizado y alineado.
- Las fechas de avisos se validan con formato estricto.
- El login administrativo cuenta con bloqueo temporal de intentos.
- Las consultas dejaron de usar SELECT *.
- Se agregaron pruebas focales automatizadas.

## Alcance de pruebas agregadas

- Validación de URL de accesos rápidos.
- Validación de fecha de avisos.
- LoginAttemptService.
- ArchivoService y límite de subida.
- Verificación textual contra SELECT *.

No se agregaron pruebas de integración con MySQL real en esta fase para evitar dependencia de infraestructura local.

## Pendiente futuro recomendado

- Agregar pruebas de integración con WebApplicationFactory.
- Usar una base de datos controlada para pruebas HTTP + persistencia.
- Evaluar pruebas de flujos completos admin cuando el proyecto tenga fixture estable.

## Dictamen final

El bloque de correcciones queda cerrado. El proyecto se encuentra sincronizado con origin/main, sin cambios locales pendientes, con build correcto, pruebas focales aprobadas y smoke local satisfactorio.
