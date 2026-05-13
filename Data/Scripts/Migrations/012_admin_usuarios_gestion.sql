-- Metadatos minimos para administrar usuarios desde el panel Admin.
-- No cambia contrasenas ni usuarios existentes.

SELECT IF(
    COUNT(*) = 0,
    'ALTER TABLE usuarios_admin ADD COLUMN fecha_creacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP AFTER area_publicacion_id',
    'SELECT 1'
) INTO @sql
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'usuarios_admin'
  AND COLUMN_NAME = 'fecha_creacion';
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SELECT IF(
    COUNT(*) = 0,
    'ALTER TABLE usuarios_admin ADD COLUMN fecha_actualizacion DATETIME NULL AFTER fecha_creacion',
    'SELECT 1'
) INTO @sql
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'usuarios_admin'
  AND COLUMN_NAME = 'fecha_actualizacion';
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SELECT IF(
    COUNT(*) = 0,
    'ALTER TABLE usuarios_admin ADD COLUMN ultimo_acceso DATETIME NULL AFTER fecha_actualizacion',
    'SELECT 1'
) INTO @sql
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'usuarios_admin'
  AND COLUMN_NAME = 'ultimo_acceso';
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

INSERT IGNORE INTO schema_migrations (version, name)
VALUES ('012', 'admin_usuarios_gestion');
