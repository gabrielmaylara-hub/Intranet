-- Fase 1 de usuarios por area:
-- areas_publicacion es un catalogo de permisos editoriales y NO depende de directorio_areas.
-- En fases posteriores, usuario_area podra publicar contenido de su propia area.

CREATE TABLE IF NOT EXISTS areas_publicacion (
    id                  INT          NOT NULL AUTO_INCREMENT,
    nombre              VARCHAR(180) NOT NULL,
    slug                VARCHAR(180) NOT NULL,
    descripcion         VARCHAR(300) NULL,
    orden               INT          NOT NULL DEFAULT 0,
    activa              TINYINT(1)   NOT NULL DEFAULT 1,
    fecha_creacion      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    fecha_actualizacion DATETIME     NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uk_areas_publicacion_slug (slug),
    UNIQUE KEY uk_areas_publicacion_nombre (nombre),
    INDEX idx_areas_publicacion_activa (activa),
    INDEX idx_areas_publicacion_orden (orden)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

SELECT IF(
    COUNT(*) = 0,
    'CREATE UNIQUE INDEX uk_areas_publicacion_slug ON areas_publicacion (slug)',
    'SELECT 1'
) INTO @sql
FROM INFORMATION_SCHEMA.STATISTICS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'areas_publicacion'
  AND INDEX_NAME = 'uk_areas_publicacion_slug';
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SELECT IF(
    COUNT(*) = 0,
    'CREATE UNIQUE INDEX uk_areas_publicacion_nombre ON areas_publicacion (nombre)',
    'SELECT 1'
) INTO @sql
FROM INFORMATION_SCHEMA.STATISTICS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'areas_publicacion'
  AND INDEX_NAME = 'uk_areas_publicacion_nombre';
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SELECT IF(
    COUNT(*) = 0,
    'CREATE INDEX idx_areas_publicacion_activa ON areas_publicacion (activa)',
    'SELECT 1'
) INTO @sql
FROM INFORMATION_SCHEMA.STATISTICS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'areas_publicacion'
  AND INDEX_NAME = 'idx_areas_publicacion_activa';
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SELECT IF(
    COUNT(*) = 0,
    'CREATE INDEX idx_areas_publicacion_orden ON areas_publicacion (orden)',
    'SELECT 1'
) INTO @sql
FROM INFORMATION_SCHEMA.STATISTICS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'areas_publicacion'
  AND INDEX_NAME = 'idx_areas_publicacion_orden';
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

INSERT INTO areas_publicacion
    (nombre, slug, descripcion, orden, activa)
VALUES
    ('Contraloría', 'contraloria', NULL, 1, 1),
    ('Dirección de Asuntos Jurídicos', 'direccion-asuntos-juridicos', NULL, 2, 1),
    ('Dirección de Recursos Humanos y Financieros', 'direccion-recursos-humanos-financieros', NULL, 3, 1),
    ('Dirección General de Desarrollo y Evaluación Institucional', 'direccion-general-desarrollo-evaluacion-institucional', NULL, 4, 1),
    ('Dirección General Administrativa', 'direccion-general-administrativa', NULL, 5, 1),
    ('Visitaduría', 'visitaduria', NULL, 6, 1),
    ('Escuela de la Fiscalía', 'escuela-fiscalia', NULL, 7, 1),
    ('Dirección de Cultura', 'direccion-cultura', NULL, 8, 1),
    ('Dirección General de Delitos Comunes', 'direccion-general-delitos-comunes', NULL, 9, 1),
    ('Despacho', 'despacho', NULL, 10, 1)
ON DUPLICATE KEY UPDATE
    nombre = VALUES(nombre),
    slug = VALUES(slug),
    orden = VALUES(orden),
    activa = 1,
    fecha_actualizacion = CURRENT_TIMESTAMP;

SELECT IF(
    COUNT(*) = 0,
    'ALTER TABLE usuarios_admin ADD COLUMN rol VARCHAR(40) NOT NULL DEFAULT ''admin_general'' AFTER activo',
    'SELECT 1'
) INTO @sql
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'usuarios_admin'
  AND COLUMN_NAME = 'rol';
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SELECT IF(
    COUNT(*) = 0,
    'ALTER TABLE usuarios_admin ADD COLUMN area_publicacion_id INT NULL AFTER rol',
    'SELECT 1'
) INTO @sql
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'usuarios_admin'
  AND COLUMN_NAME = 'area_publicacion_id';
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

UPDATE usuarios_admin
SET rol = 'admin_general'
WHERE rol IS NULL OR rol = '';

UPDATE usuarios_admin
SET area_publicacion_id = NULL
WHERE rol = 'admin_general';

SELECT IF(
    COUNT(*) = 0,
    'CREATE INDEX idx_usuarios_admin_area_publicacion ON usuarios_admin (area_publicacion_id)',
    'SELECT 1'
) INTO @sql
FROM INFORMATION_SCHEMA.STATISTICS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'usuarios_admin'
  AND INDEX_NAME = 'idx_usuarios_admin_area_publicacion';
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SELECT IF(
    COUNT(*) = 0,
    'ALTER TABLE usuarios_admin ADD CONSTRAINT fk_usuarios_admin_area_publicacion FOREIGN KEY (area_publicacion_id) REFERENCES areas_publicacion(id) ON UPDATE CASCADE ON DELETE SET NULL',
    'SELECT 1'
) INTO @sql
FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS
WHERE CONSTRAINT_SCHEMA = DATABASE()
  AND CONSTRAINT_NAME = 'fk_usuarios_admin_area_publicacion'
  AND TABLE_NAME = 'usuarios_admin';
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SELECT IF(
    COUNT(*) = 0,
    'ALTER TABLE usuarios_admin ADD CONSTRAINT chk_usuarios_admin_rol CHECK (rol IN (''admin_general'', ''usuario_area''))',
    'SELECT 1'
) INTO @sql
FROM INFORMATION_SCHEMA.CHECK_CONSTRAINTS
WHERE CONSTRAINT_SCHEMA = DATABASE()
  AND CONSTRAINT_NAME = 'chk_usuarios_admin_rol';
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- MySQL no permite usar area_publicacion_id en un CHECK cuando la misma columna
-- participa en una FK con ON DELETE SET NULL. La regla "usuario_area requiere
-- area" queda validada en backend en esta fase.

INSERT IGNORE INTO schema_migrations (version, name)
VALUES ('011', 'areas_publicacion_y_roles_usuario');
