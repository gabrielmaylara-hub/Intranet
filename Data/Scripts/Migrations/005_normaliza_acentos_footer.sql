UPDATE configuracion_sitio
SET valor = 'Fiscalía General del Estado de Tabasco'
WHERE clave = 'footer_texto'
  AND valor IN (
      'Fiscal??a General del Estado de Tabasco',
      'Fiscalia General del Estado de Tabasco'
  );

UPDATE configuracion_sitio
SET valor = 'Dirección General de Informática y Estadística'
WHERE clave = 'footer_subtexto'
  AND valor IN (
      'Direcci??n General de Inform?tica y Estad?stica',
      'Direccion General de Informatica y Estadistica'
  );

UPDATE configuracion_sitio
SET valor = 'Villahermosa, Tabasco, México'
WHERE clave = 'footer_direccion'
  AND valor IN (
      'Villahermosa, Tabasco, M?xico',
      'Villahermosa, Tabasco, Mexico'
  );

INSERT IGNORE INTO schema_migrations (version, name)
VALUES ('005', 'normaliza_acentos_footer');
