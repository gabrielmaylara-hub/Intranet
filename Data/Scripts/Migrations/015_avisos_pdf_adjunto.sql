-- 015: PDF adjunto opcional para Avisos y Comunicados.
-- Forward-only e idempotente: agrega metadatos del PDF sin afectar avisos existentes.

SET @col_pdf_path := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'avisos'
      AND COLUMN_NAME = 'pdf_path'
);

SET @sql := IF(@col_pdf_path = 0,
    'ALTER TABLE avisos ADD COLUMN pdf_path VARCHAR(500) NULL AFTER fecha_actualizacion',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_pdf_nombre := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'avisos'
      AND COLUMN_NAME = 'pdf_nombre_original'
);

SET @sql := IF(@col_pdf_nombre = 0,
    'ALTER TABLE avisos ADD COLUMN pdf_nombre_original VARCHAR(255) NULL AFTER pdf_path',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_pdf_content_type := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'avisos'
      AND COLUMN_NAME = 'pdf_content_type'
);

SET @sql := IF(@col_pdf_content_type = 0,
    'ALTER TABLE avisos ADD COLUMN pdf_content_type VARCHAR(120) NULL AFTER pdf_nombre_original',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_pdf_tamano := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'avisos'
      AND COLUMN_NAME = 'pdf_tamano_bytes'
);

SET @sql := IF(@col_pdf_tamano = 0,
    'ALTER TABLE avisos ADD COLUMN pdf_tamano_bytes BIGINT NULL AFTER pdf_content_type',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

INSERT IGNORE INTO schema_migrations (version, name)
VALUES ('015', 'avisos_pdf_adjunto');
