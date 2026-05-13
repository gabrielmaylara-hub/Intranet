CREATE TABLE IF NOT EXISTS sitio_enlaces (
    id     INT          NOT NULL AUTO_INCREMENT,
    grupo  VARCHAR(80)  NOT NULL,
    texto  VARCHAR(150) NOT NULL,
    url    VARCHAR(500) NOT NULL,
    orden  INT          NOT NULL DEFAULT 0,
    activo TINYINT(1)   NOT NULL DEFAULT 1,
    PRIMARY KEY (id),
    INDEX idx_sitio_enlaces_grupo (grupo, activo, orden)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT IGNORE INTO configuracion_sitio (clave, valor, tipo, descripcion) VALUES
('header_subtitulo', 'Fiscalía General del Estado de Tabasco', 'texto', 'Subtitulo de la marca en el header publico'),
('home_hero_etiqueta', 'Portal institucional', 'texto', 'Etiqueta del hero del Home'),
('home_hero_titulo', 'INTRANET FGET', 'texto', 'Titulo principal del hero del Home'),
('home_hero_descripcion', 'Punto de acceso para sistemas, formatos, manuales, solicitudes y recursos de trabajo de la Fiscalía General del Estado de Tabasco.', 'texto', 'Descripcion principal del hero del Home'),
('home_buscador_placeholder', 'Busca formatos, correo, manuales, capacitación...', 'texto', 'Placeholder del buscador principal del Home'),
('home_accesos_etiqueta', 'Directorio de servicios', 'texto', 'Etiqueta de la seccion de accesos rapidos'),
('home_accesos_titulo', 'ACCESOS RÁPIDOS', 'texto', 'Titulo de la seccion de accesos rapidos'),
('home_accesos_descripcion', 'Accesos concentrados para consulta del personal. Cada elemento dirige al sistema, sección o recurso correspondiente.', 'texto', 'Descripcion de la seccion de accesos rapidos'),
('home_avisos_etiqueta', 'Comunicación interna', 'texto', 'Etiqueta de avisos en Home'),
('home_avisos_titulo', 'AVISOS Y COMUNICADOS', 'texto', 'Titulo de avisos en Home'),
('home_avisos_vacio', 'No hay avisos publicados en este momento.', 'texto', 'Mensaje cuando no hay avisos publicados'),
('home_tutoriales_etiqueta', 'Material de apoyo', 'texto', 'Etiqueta de tutoriales en Home'),
('home_tutoriales_titulo', 'TUTORIALES Y VIDEOS', 'texto', 'Titulo de tutoriales en Home'),
('home_tutoriales_ver_todos', 'Ver todos →', 'texto', 'Texto del enlace a todos los tutoriales'),
('home_tutoriales_vacio', 'No hay tutoriales publicados en este momento.', 'texto', 'Mensaje cuando no hay tutoriales publicados'),
('footer_recursos_titulo', 'RECURSOS', 'texto', 'Titulo de la columna de recursos del footer'),
('footer_sistemas_titulo', 'SISTEMAS', 'texto', 'Titulo de la columna de sistemas del footer'),
('footer_contacto_titulo', 'CONTACTO', 'texto', 'Titulo de la columna de contacto del footer'),
('footer_copyright', '© 2026 Fiscalía General del Estado de Tabasco. Todos los derechos reservados.', 'texto', 'Texto de derechos reservados del footer');

INSERT INTO sitio_enlaces (grupo, texto, url, orden, activo)
SELECT grupo, texto, url, orden, activo
FROM (
    SELECT 'header_principal' AS grupo, 'Formatos' AS texto, '/formatos' AS url, 1 AS orden, 1 AS activo
    UNION ALL SELECT 'header_principal', 'Manuales', '/manuales', 2, 1
    UNION ALL SELECT 'header_principal', 'DGIE', '/dgie', 3, 1
    UNION ALL SELECT 'header_principal', 'Identidad', '/identidad', 4, 1
    UNION ALL SELECT 'header_principal', 'Capacitación', '/capacitacion', 5, 1
    UNION ALL SELECT 'header_principal', 'Tutoriales', '/tutoriales', 6, 1
) semillas_header
WHERE NOT EXISTS (
    SELECT 1 FROM sitio_enlaces WHERE grupo = 'header_principal'
);

INSERT INTO sitio_enlaces (grupo, texto, url, orden, activo)
SELECT grupo, texto, url, orden, activo
FROM (
    SELECT 'footer_recursos' AS grupo, 'Formatos Contraloría' AS texto, '/formatos' AS url, 1 AS orden, 1 AS activo
    UNION ALL SELECT 'footer_recursos', 'Manuales Justicia NET', '/manuales', 2, 1
    UNION ALL SELECT 'footer_recursos', 'Solicitudes DGIE', '/dgie', 3, 1
    UNION ALL SELECT 'footer_recursos', 'Identidad Gráfica', '/identidad', 4, 1
    UNION ALL SELECT 'footer_recursos', 'Oferta Académica', '/capacitacion', 5, 1
    UNION ALL SELECT 'footer_recursos', 'Tutoriales', '/tutoriales', 6, 1
) semillas_footer_recursos
WHERE NOT EXISTS (
    SELECT 1 FROM sitio_enlaces WHERE grupo = 'footer_recursos'
);

INSERT INTO sitio_enlaces (grupo, texto, url, orden, activo)
SELECT grupo, texto, url, orden, activo
FROM (
    SELECT 'footer_sistemas' AS grupo, 'Página web FGE' AS texto, 'https://www.fiscaliatabasco.gob.mx/' AS url, 1 AS orden, 1 AS activo
    UNION ALL SELECT 'footer_sistemas', 'Correo institucional', 'https://correo.fiscaliatabasco.gob.mx/', 2, 1
    UNION ALL SELECT 'footer_sistemas', 'Transparencia', 'https://transparencia.fiscaliatabasco.gob.mx/account', 3, 1
    UNION ALL SELECT 'footer_sistemas', 'SIGAACEJ', 'https://sigaacej.tsj-tabasco.gob.mx/', 4, 1
) semillas_footer_sistemas
WHERE NOT EXISTS (
    SELECT 1 FROM sitio_enlaces WHERE grupo = 'footer_sistemas'
);

INSERT IGNORE INTO schema_migrations (version, name)
VALUES ('008', 'configuracion_publica_enlaces');
