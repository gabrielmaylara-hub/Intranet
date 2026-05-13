-- Fase 3 de usuarios por area:
-- permite asociar avisos a areas_publicacion sin afectar avisos historicos.
-- Los avisos existentes quedan sin area y solo los administra admin_general.

SELECT IF(
    COUNT(*) = 0,
    'ALTER TABLE avisos ADD COLUMN area_publicacion_id INT NULL AFTER orden',
    'SELECT 1'
) INTO @sql
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'avisos'
  AND COLUMN_NAME = 'area_publicacion_id';
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SELECT IF(
    COUNT(*) = 0,
    'ALTER TABLE avisos ADD COLUMN creado_por_usuario_id INT NULL AFTER area_publicacion_id',
    'SELECT 1'
) INTO @sql
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'avisos'
  AND COLUMN_NAME = 'creado_por_usuario_id';
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SELECT IF(
    COUNT(*) = 0,
    'ALTER TABLE avisos ADD COLUMN actualizado_por_usuario_id INT NULL AFTER creado_por_usuario_id',
    'SELECT 1'
) INTO @sql
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'avisos'
  AND COLUMN_NAME = 'actualizado_por_usuario_id';
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SELECT IF(
    COUNT(*) = 0,
    'ALTER TABLE avisos ADD COLUMN fecha_actualizacion DATETIME NULL AFTER actualizado_por_usuario_id',
    'SELECT 1'
) INTO @sql
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'avisos'
  AND COLUMN_NAME = 'fecha_actualizacion';
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SELECT IF(
    COUNT(*) = 0,
    'CREATE INDEX idx_avisos_area_publicacion ON avisos (area_publicacion_id)',
    'SELECT 1'
) INTO @sql
FROM INFORMATION_SCHEMA.STATISTICS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'avisos'
  AND INDEX_NAME = 'idx_avisos_area_publicacion';
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SELECT IF(
    COUNT(*) = 0,
    'CREATE INDEX idx_avisos_activo_area ON avisos (activo, area_publicacion_id)',
    'SELECT 1'
) INTO @sql
FROM INFORMATION_SCHEMA.STATISTICS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'avisos'
  AND INDEX_NAME = 'idx_avisos_activo_area';
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SELECT IF(
    COUNT(*) = 0,
    'CREATE INDEX idx_avisos_fecha_area ON avisos (fecha_publicacion, area_publicacion_id)',
    'SELECT 1'
) INTO @sql
FROM INFORMATION_SCHEMA.STATISTICS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'avisos'
  AND INDEX_NAME = 'idx_avisos_fecha_area';
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SELECT IF(
    COUNT(*) = 0,
    'ALTER TABLE avisos ADD CONSTRAINT fk_avisos_area_publicacion FOREIGN KEY (area_publicacion_id) REFERENCES areas_publicacion(id) ON UPDATE CASCADE ON DELETE SET NULL',
    'SELECT 1'
) INTO @sql
FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS
WHERE CONSTRAINT_SCHEMA = DATABASE()
  AND CONSTRAINT_NAME = 'fk_avisos_area_publicacion'
  AND TABLE_NAME = 'avisos';
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SELECT IF(
    COUNT(*) = 0,
    'ALTER TABLE avisos ADD CONSTRAINT fk_avisos_creado_por_usuario FOREIGN KEY (creado_por_usuario_id) REFERENCES usuarios_admin(id) ON UPDATE CASCADE ON DELETE SET NULL',
    'SELECT 1'
) INTO @sql
FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS
WHERE CONSTRAINT_SCHEMA = DATABASE()
  AND CONSTRAINT_NAME = 'fk_avisos_creado_por_usuario'
  AND TABLE_NAME = 'avisos';
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SELECT IF(
    COUNT(*) = 0,
    'ALTER TABLE avisos ADD CONSTRAINT fk_avisos_actualizado_por_usuario FOREIGN KEY (actualizado_por_usuario_id) REFERENCES usuarios_admin(id) ON UPDATE CASCADE ON DELETE SET NULL',
    'SELECT 1'
) INTO @sql
FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS
WHERE CONSTRAINT_SCHEMA = DATABASE()
  AND CONSTRAINT_NAME = 'fk_avisos_actualizado_por_usuario'
  AND TABLE_NAME = 'avisos';
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

INSERT IGNORE INTO schema_migrations (version, name)
VALUES ('013', 'avisos_area_publicacion');
