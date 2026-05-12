UPDATE accesos_rapidos
SET nombre = 'Página web de la FGE'
WHERE url = 'https://www.fiscaliatabasco.gob.mx/'
  AND nombre IN ('P??gina web de la FGE', 'Pagina web de la FGE', 'Página web de la FGE');

UPDATE accesos_rapidos
SET nombre = 'Declaración patrimonial'
WHERE url = 'https://declaracionpatrimonial.fiscaliatabasco.gob.mx/'
  AND nombre IN ('Declaraci??n patrimonial', 'Declaracion patrimonial', 'Declaración patrimonial');

UPDATE accesos_rapidos
SET nombre = 'Formatos de No Adeudo y Entrega-Recepción'
WHERE url = '/formatos'
  AND nombre IN (
      'Formatos de No Adeudo y Entrega-Recepci??n',
      'Formatos de No Adeudo y Entrega-Recepcion',
      'Formatos de No Adeudo y Entrega-Recepción'
  );

UPDATE accesos_rapidos
SET nombre = 'Entrega recepción'
WHERE url = 'https://entregarecepcion.fiscaliatabasco.gob.mx/'
  AND nombre IN ('Entrega recepci??n', 'Entrega recepcion', 'Entrega recepción');

UPDATE accesos_rapidos
SET nombre = 'Oferta Académica'
WHERE url = '/capacitacion'
  AND nombre IN ('Oferta Acad??mica', 'Oferta Academica', 'Oferta Académica');

UPDATE accesos_rapidos
SET nombre = 'Identidad Gráfica FGET'
WHERE url = '/identidad'
  AND nombre IN ('Identidad Gr??fica FGET', 'Identidad Grafica FGET', 'Identidad Gráfica FGET');

INSERT IGNORE INTO schema_migrations (version, name)
VALUES ('004', 'normaliza_acentos_accesos_rapidos');
