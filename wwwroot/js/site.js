// =============================================================================
// INTRANET FGET - Interacciones del sitio publico
// =============================================================================

(() => {
  "use strict";

  // ---------------------------------------------------------------------------
  // Modo PDF: usado desde el panel administrativo para imprimir la vista publica.
  // Abre una barra de accion y prepara la pagina para el dialogo "Guardar como PDF".
  // ---------------------------------------------------------------------------
  const parametros = new URLSearchParams(window.location.search);
  const modoPdf = parametros.get("pdf") === "1";

  if (modoPdf) {
    document.body.classList.add("modo-pdf");

    const barraPdf = document.createElement("div");
    barraPdf.className = "pdf-toolbar";
    barraPdf.innerHTML = `
      <span>Vista publica preparada para PDF</span>
      <button type="button" id="btnImprimirPdf">Guardar PDF</button>
      <button type="button" id="btnCerrarPdf">Cerrar</button>
    `;
    document.body.prepend(barraPdf);

    barraPdf.querySelector("#btnImprimirPdf")?.addEventListener("click", () => window.print());
    barraPdf.querySelector("#btnCerrarPdf")?.addEventListener("click", () => window.close());
  }

  // ---------------------------------------------------------------------------
  // Menu principal responsive.
  // ---------------------------------------------------------------------------
  const navToggle = document.getElementById("navToggle");
  const navLista = document.getElementById("navLista");

  if (navToggle && navLista) {
    const cerrarMenu = () => {
      navLista.classList.remove("nav-lista--abierta");
      navToggle.setAttribute("aria-expanded", "false");
    };

    navToggle.addEventListener("click", () => {
      const abierto = navLista.classList.toggle("nav-lista--abierta");
      navToggle.setAttribute("aria-expanded", abierto ? "true" : "false");
    });

    navLista.querySelectorAll("a").forEach((enlace) => {
      enlace.addEventListener("click", cerrarMenu);
    });

    document.addEventListener("keydown", (evento) => {
      if (evento.key === "Escape") cerrarMenu();
    });
  }

  // ---------------------------------------------------------------------------
  // Buscador local de accesos rapidos.
  // Filtra unicamente los elementos visibles del grid, sin consultar servidor.
  // ---------------------------------------------------------------------------
  const buscador = document.getElementById("buscadorAccesos");
  const btnBuscar = document.getElementById("btnBuscarAccesos");
  const gridAccesos = document.getElementById("gridAccesos");
  const sinResultados = document.getElementById("sinResultados");
  const filtrosBusqueda = document.querySelectorAll("[data-busqueda]");
  const formularioBusquedaGlobal = buscador?.closest("[data-busqueda-global]");

  if (buscador && formularioBusquedaGlobal) {
    formularioBusquedaGlobal.addEventListener("submit", () => {
      buscador.value = (buscador.value || "").trim().slice(0, 100);
    });
  } else if (buscador && gridAccesos) {
    const accesos = Array.from(gridAccesos.querySelectorAll(".acceso-card"));

    const normalizar = (texto) =>
      (texto || "")
        .toString()
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "")
        .toLowerCase()
        .trim();

    const filtrarAccesos = () => {
      const consulta = normalizar(buscador.value);
      let visibles = 0;

      accesos.forEach((acceso) => {
        const nombre = normalizar(acceso.dataset.nombre || acceso.textContent);
        const coincide = consulta.length === 0 || nombre.includes(consulta);
        acceso.hidden = !coincide;
        if (coincide) visibles += 1;
      });

      if (sinResultados) sinResultados.hidden = visibles > 0;
    };

    buscador.addEventListener("input", filtrarAccesos);
    btnBuscar?.addEventListener("click", filtrarAccesos);
    buscador.addEventListener("keydown", (evento) => {
      if (evento.key === "Enter") {
        evento.preventDefault();
        filtrarAccesos();
      }
    });

    filtrosBusqueda.forEach((filtro) => {
      filtro.addEventListener("click", () => {
        buscador.value = filtro.dataset.busqueda || "";
        filtrarAccesos();
        buscador.focus();
      });
    });
  }

  // ---------------------------------------------------------------------------
  // Directorio publico: envia la busqueda GET con una pausa breve al escribir.
  // ---------------------------------------------------------------------------
  const directorioBuscador = document.querySelector("[data-directorio-buscador]");

  if (directorioBuscador) {
    const controles = Array.from(directorioBuscador.querySelectorAll("[data-directorio-auto]"));
    let timerBusqueda;

    const enviarBusqueda = () => {
      const datos = new FormData(directorioBuscador);
      const parametrosBusqueda = new URLSearchParams();

      datos.forEach((valor, clave) => {
        const texto = (valor || "").toString().trim();
        if (texto.length > 0) parametrosBusqueda.set(clave, texto);
      });

      const destino = new URL(directorioBuscador.action || window.location.href, window.location.origin);
      destino.search = parametrosBusqueda.toString();
      window.location.assign(destino.toString());
    };

    const programarBusqueda = (inmediata = false) => {
      window.clearTimeout(timerBusqueda);
      timerBusqueda = window.setTimeout(enviarBusqueda, inmediata ? 0 : 350);
    };

    controles.forEach((control) => {
      const evento = control.tagName === "SELECT" ? "change" : "input";
      control.addEventListener(evento, () => programarBusqueda(evento === "change"));
    });
  }

  if (modoPdf) {
    window.addEventListener("load", () => {
      window.setTimeout(() => window.print(), 700);
    });
  }
})();
