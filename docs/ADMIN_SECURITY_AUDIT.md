# Auditoria de seguridad Admin

## 1. Proposito

Documentar la evidencia tecnica de la auditoria no destructiva de seguridad y control de acceso del area administrativa de la Intranet FGET.

La revision se enfoco en autenticacion, autorizacion, proteccion antiforgery, validacion basica de datos y exposicion de informacion sensible. No se aplicaron cambios funcionales durante la auditoria.

## 2. Alcance

Dentro de alcance:

- Rutas administrativas bajo `/Admin`.
- Flujo de login administrativo.
- Acceso sin sesion a rutas protegidas.
- Acceso autenticado a rutas principales.
- Formularios y handlers POST administrativos revisados.
- Proteccion antiforgery.
- Validacion basica en Directorio y Accesos Rapidos.
- Revision de logs generados durante la prueba.
- Endpoints tecnicos de health, solo para confirmar que no exponen secretos.

Fuera de alcance:

- Cambios de codigo.
- Cambios de configuracion.
- Cambios en base de datos.
- Pruebas destructivas.
- Fuerza bruta.
- Fuzzing masivo.
- Revision profunda de infraestructura productiva.

## 3. Estado base

| Elemento | Valor |
|---|---|
| Rama | `main` |
| HEAD | `8a15975 fix(seed): normaliza acentos en accesos rapidos` |
| Tag en HEAD | `intranet-directorio-health-localdev-v1` |
| Estado de Git | Limpio y sincronizado con `origin/main` al inicio de la auditoria |

## 4. Rutas Admin detectadas

| Ruta | Observacion |
|---|---|
| `/Admin` | Dashboard administrativo |
| `/Admin/Index` | Entrada administrativa equivalente |
| `/Admin/Login` | Login administrativo |
| `/Admin/AccesosRapidos` | Administracion de accesos rapidos |
| `/Admin/Directorio` | Administracion del directorio institucional |
| `/Admin/Avisos` | Administracion de avisos |
| `/Admin/Tutoriales` | Administracion de tutoriales |
| `/Admin/Archivos` | Administracion de archivos por seccion |
| `/Admin/Configuracion` | Configuracion administrativa |

## 5. Resultado sin sesion

| Validacion | Resultado observado | Estado |
|---|---|---|
| Acceso a rutas Admin protegidas sin sesion | Redireccion `302` a `/Admin/Login` | Correcto |
| Exposicion de HTML administrativo sin sesion | No se observo contenido administrativo expuesto | Correcto |
| POST sin sesion en handlers probados | Bloqueado con redireccion `302` a login | Correcto |
| `/Admin/Login` sin sesion | Responde `200` como pagina publica de autenticacion | Correcto |

## 6. Resultado con sesion

| Validacion | Resultado observado | Estado |
|---|---|---|
| Login administrativo | Funcional, sin imprimir contrasena | Correcto |
| `/Admin` autenticado | Responde `200` | Correcto |
| `/Admin/AccesosRapidos` autenticado | Responde `200` | Correcto |
| `/Admin/Directorio` autenticado | Responde `200` | Correcto |
| Rutas Admin principales autenticadas | Responden `200` en las pruebas ejecutadas | Correcto |

## 7. Autenticacion y autorizacion

| Control | Estado observado |
|---|---|
| Proteccion de carpeta Admin | Configurada con `AuthorizeFolder("/Admin")` |
| Login anonimo | `/Admin/Login` permitido anonimamente |
| Cookie HttpOnly | Habilitada |
| SameSite | `Strict` |
| SecurePolicy | `SameAsRequest` |
| Redireccion de login | Configurada hacia `/Admin/Login` |
| Health checks | Publicos y sin informacion sensible |

## 8. Antiforgery

| Validacion | Resultado observado | Estado |
|---|---|---|
| Formularios administrativos revisados | Incluyen token antiforgery | Correcto |
| POST autenticado sin token | Responde `400` en pruebas negativas | Correcto |
| Logs por antiforgery | Entradas esperadas por pruebas negativas controladas | Observacion |
| Exposicion de secretos en errores antiforgery | No observada | Correcto |

Las entradas de antiforgery en logs corresponden al comportamiento esperado cuando se envian peticiones POST negativas sin token valido.

## 9. Validacion de datos

| Area | Control observado |
|---|---|
| Directorio | Valida campos obligatorios |
| Directorio | Valida duplicados por `Area + Nombre` |
| Directorio | Valida duplicados por `Area + Extension` |
| Directorio CSV | Limitado a archivos `.csv` |
| Directorio CSV | Limite de tamano de `2 MB` |
| Directorio CSV | Limite de `1000` filas |
| Accesos Rapidos | Valida nombre |
| Accesos Rapidos | Valida URL |
| Accesos Rapidos | Acepta ruta interna `/...` o URL `http/https` |
| Razor | Salida codificada por defecto |

No se observaron salidas administrativas sin codificar de forma riesgosa en las rutas revisadas.

## 10. Logs

| Revision | Resultado |
|---|---|
| Errores criticos | No observados |
| Respuestas `500` | No observadas |
| Secretos en logs | No observados |
| Cadenas de conexion en logs | No observadas |
| Errores esperados | Eventos antiforgery por pruebas negativas sin token |

## 11. Matriz de riesgos

| Severidad | Hallazgo | Estado |
|---|---|---|
| Alta | Ninguno | Sin hallazgos |
| Media | Ninguno | Sin hallazgos |
| Baja | Ninguno explotable | Sin hallazgos explotables |
| Observacion | Logs antiforgery por pruebas negativas | Esperado y no explotable |

## 12. Dictamen

La auditoria no destructiva del area administrativa no identifico hallazgos explotables en autenticacion, autorizacion, proteccion antiforgery ni validaciones basicas revisadas.

El area Admin queda lista sin cambios para esta fase.

## 13. Pendiente futuro para despliegue

Para un despliegue productivo con HTTPS, IIS o reverse proxy, se recomienda revisar si la politica de cookie `SecurePolicy=SameAsRequest` debe elevarse a `SecurePolicy=Always`.

Esta observacion queda como recomendacion futura de despliegue y no implica cambio en esta fase.
