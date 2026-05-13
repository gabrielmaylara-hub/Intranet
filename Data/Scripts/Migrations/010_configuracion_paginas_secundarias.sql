INSERT IGNORE INTO configuracion_sitio (clave, valor, tipo, descripcion) VALUES
('pagina_formatos_titulo', 'Formatos de Contraloría', 'texto', 'Titulo de la pagina publica de Formatos'),
('pagina_formatos_descripcion', 'Formatos oficiales de la Contraloría Interna de la Fiscalía General del Estado de Tabasco. Descarga el formato que necesitas en formato PDF.', 'texto', 'Descripcion de la pagina publica de Formatos'),
('pagina_manuales_titulo', 'Manuales de Capacitación Justicia.NET', 'texto', 'Titulo de la pagina publica de Manuales'),
('pagina_manuales_descripcion', 'Manuales de usuario y capacitación del sistema Justicia.NET. Consulta o descarga el manual que necesitas.', 'texto', 'Descripcion de la pagina publica de Manuales'),
('pagina_dgie_titulo', 'Solicitud de Anuencia Técnica DGIE', 'texto', 'Titulo de la pagina publica de DGIE'),
('pagina_dgie_descripcion', 'Formatos y documentos para tramitar una solicitud de anuencia técnica ante la Dirección General de Informática y Estadística.', 'texto', 'Descripcion de la pagina publica de DGIE'),
('pagina_identidad_titulo', 'Kit de Identidad Gráfica FGET', 'texto', 'Titulo de la pagina publica de Identidad'),
('pagina_identidad_descripcion', 'Recursos gráficos oficiales de la Fiscalía General del Estado de Tabasco: logotipos, paleta de colores, tipografías y lineamientos de uso.', 'texto', 'Descripcion de la pagina publica de Identidad'),
('pagina_capacitacion_titulo', 'Oferta Académica', 'texto', 'Titulo de la pagina publica de Capacitacion'),
('pagina_capacitacion_descripcion', 'Cursos de capacitación y formación profesional disponibles para el personal de la Fiscalía General del Estado de Tabasco.', 'texto', 'Descripcion de la pagina publica de Capacitacion'),
('pagina_capacitacion_internos_titulo', 'Cursos y Materiales Internos', 'texto', 'Titulo de materiales internos en Capacitacion'),
('pagina_capacitacion_externo_activo', '1', 'booleano', 'Indica si se muestra el enlace academico externo'),
('pagina_capacitacion_externo_titulo', 'Sistema Integral de Gestión Académica', 'texto', 'Titulo del enlace academico externo'),
('pagina_capacitacion_externo_descripcion', 'Accede al sistema SIGAACEJ del Tribunal Superior de Justicia del Estado de Tabasco para consultar la oferta académica institucional compartida.', 'texto', 'Descripcion del enlace academico externo'),
('pagina_capacitacion_externo_boton_texto', 'Acceder a SIGAACEJ ↗', 'texto', 'Texto del boton academico externo'),
('pagina_capacitacion_externo_boton_url', 'https://sigaacej.tsj-tabasco.gob.mx/', 'url', 'URL del boton academico externo'),
('pagina_tutoriales_titulo', 'Tutoriales Institucionales', 'texto', 'Titulo de la pagina publica de Tutoriales'),
('pagina_tutoriales_descripcion', 'Videos de capacitación y guías de uso de los sistemas institucionales de la Fiscalía General del Estado de Tabasco.', 'texto', 'Descripcion de la pagina publica de Tutoriales'),
('pagina_tutoriales_vacio', 'No hay tutoriales publicados en este momento.', 'texto', 'Mensaje cuando no hay tutoriales publicados'),
('pagina_tutoriales_video_no_soportado', 'Tu navegador no soporta reproducción de video.', 'texto', 'Mensaje de video no soportado'),
('pagina_tutoriales_video_proximamente', 'Video próximamente', 'texto', 'Mensaje cuando un tutorial no tiene video');

INSERT IGNORE INTO schema_migrations (version, name)
VALUES ('010', 'configuracion_paginas_secundarias');
