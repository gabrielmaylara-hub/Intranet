# Bitácora del proyecto

## 2026-05-14 - Generador de entrega SQL Server 2022

- Commit integrado en `main`: `4ec17ae chore(release): agrega generador de entrega SQL Server 2022`.
- Ubicación del tooling: `tools/entrega-sqlserver`.
- Alcance versionado: generador de entrega, README, checklist, plantilla de credenciales con placeholders y restauradores SQL Server.
- Exclusiones confirmadas: no se incluyeron respaldos, credenciales reales, Storage masivo, app publicada, logs, reportes de prueba ni carpetas de entrega generadas.
- Método de integración: cherry-pick sobre `main` para evitar arrastrar cambios funcionales no deseados desde la rama base.
- Validación de build: `dotnet build Intranet.sln` exitoso con SDK .NET `10.0.100`.
- Validación runtime: no ejecutada porque MySQL local no estaba disponible. No bloqueó el cierre porque el cambio corresponde a tooling de entrega, no a comportamiento funcional de la aplicación.
