# Baseline de rendimiento y concurrencia

## 1. Proposito

Documentar el comportamiento base de la Intranet FGET bajo carga local controlada, antes de introducir cache, ajustes de headers o cambios de despliegue. Este documento sirve como punto de comparacion para futuras pruebas.

## 2. Contexto tecnico

- Rama: `main`.
- Tag probado: `intranet-db-security-integrity-v1`.
- Aplicacion local: `0.0.0.0:5077`.
- Base local: `intranet_fget`.
- Tipo de prueba: no destructiva, solo solicitudes `GET`.
- No se ejecutaron `POST`.
- No se modificaron datos.

## 3. Rutas probadas

- `/health/live`
- `/health/ready`
- `/`
- `/Directorio`
- `/directorio`
- `/Admin/Login`

## 4. Resultado individual por ruta

| Ruta | Codigo | Tiempo aproximado |
|---|---:|---:|
| `/health/live` | 200 | 148.46 ms |
| `/health/ready` | 200 | 24.08 ms |
| `/` | 200 | 137.05 ms |
| `/Directorio` | 200 | 75.73 ms |
| `/directorio` | 200 | 60.35 ms |
| `/Admin/Login` | 200 | 42.65 ms |

## 5. Concurrencia: 10 usuarios durante 30 segundos

| Metrica | Resultado |
|---|---:|
| Solicitudes | 18,049 |
| Exito | 100% |
| Errores | 0 |
| Latencia promedio | 16.12 ms |
| p95 | 30 ms |
| p99 | 38 ms |
| RPS aproximado | 601.43 |
| CPU aproximada | 10.36% |
| RAM | 89.19 MB a 147.84 MB |

## 6. Concurrencia: 25 usuarios durante 60 segundos

| Metrica | Resultado |
|---|---:|
| Solicitudes | 31,864 |
| Exito | 100% |
| Errores | 0 |
| Latencia promedio | 46.58 ms |
| p95 | 79 ms |
| p99 | 102 ms |
| RPS aproximado | 530.89 |
| CPU aproximada | 10.03% |
| RAM | 147.84 MB a 155.46 MB |

## 7. Concurrencia: 50 usuarios durante 60 segundos

| Metrica | Resultado |
|---|---:|
| Solicitudes | 32,313 |
| Exito | 100% |
| Errores | 0 |
| Latencia promedio | 92.42 ms |
| p95 | 134 ms |
| p99 | 164 ms |
| RPS aproximado | 537.93 |
| CPU aproximada | 9.57% |
| RAM | 155.46 MB a 159.84 MB |

## 8. Logs

- Sin errores `500`.
- Sin timeouts.
- Sin errores SQL.
- Sin secretos expuestos.
- Se observo una advertencia local esperada: `Failed to determine the https port for redirect`.

## 9. Limitaciones

- Prueba ejecutada en entorno local.
- No equivale a una prueba bajo IIS ni a una prueba de produccion.
- El grupo de rutas incluye endpoints ligeros como `health`.
- No se probaron escrituras concurrentes.
- No se probaron formularios ni acciones `POST`.

## 10. Dictamen

El comportamiento base es sano para el uso esperado en PC dentro de la intranet. No se requiere cache inmediato con los datos actuales.

Se recomienda considerar cache publico solo si aparecen picos reales de uso, latencia sostenida en Home o Directorio, o si futuras pruebas bajo IIS muestran degradacion.

## 11. Proximas pruebas sugeridas

- Prueba focal solo para Home y Directorio.
- Prueba bajo IIS o un entorno equivalente al despliegue final.
- Prueba comparativa antes/despues si se implementa cache en el futuro.
