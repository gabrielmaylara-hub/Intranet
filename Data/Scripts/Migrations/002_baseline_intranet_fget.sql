-- Esquema base idempotente para permitir que la app arranque contra una BD vacia.
-- No crea ni selecciona una base por nombre; usa la base configurada en la cadena de conexion.

CREATE TABLE IF NOT EXISTS configuracion_sitio (
    clave       VARCHAR(100)  NOT NULL,
    valor       TEXT,
    tipo        VARCHAR(50)   NOT NULL DEFAULT 'texto'
                    COMMENT 'texto | imagen | url | email | telefono | color',
    descripcion VARCHAR(255),
    PRIMARY KEY (clave)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS accesos_rapidos (
    id                 INT          NOT NULL AUTO_INCREMENT,
    nombre             VARCHAR(150) NOT NULL,
    url                VARCHAR(500) NOT NULL,
    icono_path         VARCHAR(500) NULL COMMENT 'Ruta relativa desde Storage/',
    banner_path        VARCHAR(500) NULL COMMENT 'Imagen panoramica desde Storage/',
    orden              INT          NOT NULL DEFAULT 0,
    abre_nueva_ventana TINYINT(1)   NOT NULL DEFAULT 1,
    activo             TINYINT(1)   NOT NULL DEFAULT 1,
    PRIMARY KEY (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS avisos (
    id                 INT          NOT NULL AUTO_INCREMENT,
    titulo             VARCHAR(300) NOT NULL,
    contenido          TEXT,
    fecha_publicacion  DATE         NOT NULL,
    activo             TINYINT(1)   NOT NULL DEFAULT 1,
    orden              INT          NOT NULL DEFAULT 0,
    PRIMARY KEY (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS tutoriales (
    id              INT          NOT NULL AUTO_INCREMENT,
    titulo          VARCHAR(300) NOT NULL,
    descripcion     TEXT,
    archivo_path    VARCHAR(500) NULL COMMENT 'Ruta relativa del .mp4 desde Storage/',
    miniatura_path  VARCHAR(500) NULL COMMENT 'Ruta relativa de la miniatura desde Storage/',
    orden           INT          NOT NULL DEFAULT 0,
    activo          TINYINT(1)   NOT NULL DEFAULT 1,
    fecha_creacion  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS archivos_seccion (
    id           INT          NOT NULL AUTO_INCREMENT,
    seccion      VARCHAR(50)  NOT NULL COMMENT 'formatos|manuales|dgie|identidad|capacitacion',
    nombre       VARCHAR(300) NOT NULL,
    descripcion  TEXT,
    archivo_path VARCHAR(500) NOT NULL COMMENT 'Ruta relativa desde Storage/',
    orden        INT          NOT NULL DEFAULT 0,
    activo       TINYINT(1)   NOT NULL DEFAULT 1,
    PRIMARY KEY (id),
    INDEX idx_seccion (seccion),
    INDEX idx_seccion_orden (seccion, orden)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS usuarios_admin (
    id              INT          NOT NULL AUTO_INCREMENT,
    usuario         VARCHAR(100) NOT NULL,
    password_hash   VARCHAR(255) NOT NULL,
    nombre_completo VARCHAR(200) NOT NULL,
    activo          TINYINT(1)   NOT NULL DEFAULT 1,
    PRIMARY KEY (id),
    UNIQUE KEY uq_usuario (usuario)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT IGNORE INTO configuracion_sitio (clave, valor, tipo, descripcion) VALUES
('nombre_sitio',          'Intranet FGET',                                 'texto',    'Nombre del sitio mostrado en el titulo'),
('logo_path',             '',                                               'imagen',   'Ruta del logo principal'),
('footer_texto',          'Fiscalía General del Estado de Tabasco',         'texto',    'Texto principal del footer'),
('footer_subtexto',       'Dirección General de Informática y Estadística',  'texto',    'Subtexto del footer'),
('footer_contacto_email', 'dgie@fiscaliatabasco.gob.mx',                    'email',    'Correo de contacto DGIE'),
('footer_contacto_tel',   '',                                               'telefono', 'Telefono de contacto'),
('footer_direccion',      'Villahermosa, Tabasco, México',                  'texto',    'Direccion fisica'),
('color_dorado',          '#c9922a',                                         'color',    'Color institucional principal'),
('color_dorado_bri',      '#e8a820',                                         'color',    'Color institucional de realce');

INSERT IGNORE INTO accesos_rapidos
    (id, nombre, url, icono_path, banner_path, orden, abre_nueva_ventana, activo)
VALUES
    (1,  'Página web de la FGE',   'https://www.fiscaliatabasco.gob.mx/',                    NULL, NULL, 1,  1, 1),
    (2,  'Correo FGE',             'https://correo.fiscaliatabasco.gob.mx/',                 NULL, NULL, 2,  1, 1),
    (3,  'Transparencia',          'https://transparencia.fiscaliatabasco.gob.mx/account',   NULL, NULL, 3,  1, 1),
    (4,  'Declaración patrimonial','https://declaracionpatrimonial.fiscaliatabasco.gob.mx/', NULL, NULL, 4,  1, 1),
    (5,  'Formatos de No Adeudo y Entrega-Recepción', '/formatos',                           NULL, NULL, 5,  0, 1),
    (6,  'Entrega recepción',      'https://entregarecepcion.fiscaliatabasco.gob.mx/',       NULL, NULL, 6,  1, 1),
    (7,  'Manuales Justicia NET',  '/manuales',                                              NULL, NULL, 7,  0, 1),
    (8,  'Solicitudes DGIE',       '/dgie',                                                  NULL, NULL, 8,  0, 1),
    (9,  'SGC',                    'https://manualespericiales.fiscaliatabasco.gob.mx/',     NULL, NULL, 9,  1, 1),
    (10, 'Oferta Académica',       '/capacitacion',                                          NULL, NULL, 10, 0, 1),
    (11, 'Identidad Gráfica FGET', '/identidad',                                             NULL, NULL, 11, 0, 1),
    (12, 'Tutoriales y Videos',    '/tutoriales',                                            NULL, NULL, 12, 0, 1),
    (13, 'NUBE PRIVADA',           'https://nube.fiscaliatabasco.gob.mx',                    NULL, NULL, 13, 1, 1),
    (14, 'DIRECTORIO',             '/directorio',                                            NULL, NULL, 14, 0, 1);

INSERT IGNORE INTO schema_migrations (version, name)
VALUES ('002', 'baseline_intranet_fget_existing_schema');
