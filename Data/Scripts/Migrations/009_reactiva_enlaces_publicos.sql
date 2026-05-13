UPDATE sitio_enlaces se
JOIN (
    SELECT 'header_principal' AS grupo, COUNT(*) AS activos
    FROM sitio_enlaces
    WHERE grupo = 'header_principal' AND activo = 1
) estado ON estado.grupo = se.grupo
SET se.activo = 1
WHERE se.grupo = 'header_principal'
  AND estado.activos = 0;

UPDATE sitio_enlaces se
JOIN (
    SELECT 'footer_recursos' AS grupo, COUNT(*) AS activos
    FROM sitio_enlaces
    WHERE grupo = 'footer_recursos' AND activo = 1
) estado ON estado.grupo = se.grupo
SET se.activo = 1
WHERE se.grupo = 'footer_recursos'
  AND estado.activos = 0;

UPDATE sitio_enlaces se
JOIN (
    SELECT 'footer_sistemas' AS grupo, COUNT(*) AS activos
    FROM sitio_enlaces
    WHERE grupo = 'footer_sistemas' AND activo = 1
) estado ON estado.grupo = se.grupo
SET se.activo = 1
WHERE se.grupo = 'footer_sistemas'
  AND estado.activos = 0;

INSERT IGNORE INTO schema_migrations (version, name)
VALUES ('009', 'reactiva_enlaces_publicos');
