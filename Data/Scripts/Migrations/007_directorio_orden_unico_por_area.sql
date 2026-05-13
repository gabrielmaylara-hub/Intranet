UPDATE directorio d
JOIN (
    SELECT ordenados.id, ordenados.nuevo_orden
    FROM (
        SELECT
            id,
            ROW_NUMBER() OVER (
                PARTITION BY area
                ORDER BY orden ASC, nombre ASC, id ASC
            ) AS nuevo_orden
        FROM directorio
        WHERE area IN (
            SELECT area
            FROM (
                SELECT area
                FROM directorio
                GROUP BY area, orden
                HAVING COUNT(*) > 1
            ) areas_con_orden_duplicado
        )
    ) ordenados
) r ON r.id = d.id
SET d.orden = r.nuevo_orden
WHERE d.orden <> r.nuevo_orden;

SET @indice_orden_existe = (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'directorio'
      AND INDEX_NAME = 'uk_directorio_area_orden'
);

SET @sql_indice_orden = IF(
    @indice_orden_existe = 0,
    'CREATE UNIQUE INDEX uk_directorio_area_orden ON directorio (area, orden)',
    'SELECT 1'
);

PREPARE stmt_indice_orden FROM @sql_indice_orden;
EXECUTE stmt_indice_orden;
DEALLOCATE PREPARE stmt_indice_orden;

INSERT IGNORE INTO schema_migrations (version, name)
VALUES ('007', 'directorio_orden_unico_por_area');
