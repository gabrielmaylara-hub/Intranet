# Archivos y subidas

## Límite operativo

La aplicación centraliza el tamaño máximo de archivo en:

```json
"Uploads": {
  "MaxFileSizeBytes": 524288000
}
```

El valor actual equivale a 500 MiB y se usa en dos puntos:

- `Program.cs`: configura `FormOptions.MultipartBodyLengthLimit`, Kestrel y `IISServerOptions`.
- `Services/ArchivoService.cs`: valida el tamaño del archivo antes de guardarlo en `Storage/`.

## IIS

El archivo `web.config` declara `maxAllowedContentLength="524288000"` para alinear el límite de IIS con la configuración de la aplicación.

Si en producción se cambia `Uploads:MaxFileSizeBytes`, también debe actualizarse `web.config`, porque IIS lee ese límite desde XML y no desde `appsettings.json`.

## Validación

Los archivos que superen el límite configurado deben rechazarse con un mensaje claro para el administrador. Los módulos que usan `ArchivoService` heredan esta validación:

- Accesos rápidos: iconos y banners.
- Tutoriales: videos y miniaturas.
- Archivos por sección: PDFs.
- Configuración visual: logo institucional.
