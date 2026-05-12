ALTER TABLE directorio
    ADD COLUMN extension_unica VARCHAR(30)
        GENERATED ALWAYS AS (NULLIF(TRIM(extension), '')) STORED;

CREATE UNIQUE INDEX uk_directorio_area_nombre
    ON directorio (area, nombre);

CREATE UNIQUE INDEX uk_directorio_area_extension_unica
    ON directorio (area, extension_unica);

INSERT IGNORE INTO schema_migrations (version, name)
VALUES ('006', 'directorio_restricciones_unicas');
