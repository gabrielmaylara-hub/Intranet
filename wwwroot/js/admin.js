// =============================================================================
// INTRANET FGET - Interacciones del panel administrativo
// =============================================================================

(() => {
  "use strict";

  // ---------------------------------------------------------------------------
  // Sidebar responsive del panel.
  // ---------------------------------------------------------------------------
  const sidebar = document.getElementById("adminSidebar");
  const sidebarToggle = document.getElementById("sidebarToggle");

  if (sidebar && sidebarToggle) {
    const cerrarSidebar = () => {
      sidebar.classList.remove("sidebar--abierto");
      sidebarToggle.setAttribute("aria-expanded", "false");
    };

    sidebarToggle.addEventListener("click", () => {
      const abierto = sidebar.classList.toggle("sidebar--abierto");
      sidebarToggle.setAttribute("aria-expanded", abierto ? "true" : "false");
    });

    document.addEventListener("keydown", (evento) => {
      if (evento.key === "Escape") cerrarSidebar();
    });

    document.addEventListener("click", (evento) => {
      const objetivo = evento.target;
      if (!(objetivo instanceof Node)) return;
      if (sidebar.contains(objetivo) || sidebarToggle.contains(objetivo)) return;
      cerrarSidebar();
    });
  }

  // ---------------------------------------------------------------------------
  // Formularios reutilizados de alta y edicion.
  // Las paginas de Accesos, Avisos y Tutoriales comparten los mismos ids base.
  // ---------------------------------------------------------------------------
  const panelFormulario = document.getElementById("panelFormulario");
  const btnNuevo = document.getElementById("btnNuevo");
  const btnCancelar = document.getElementById("btnCancelar");

  if (panelFormulario) {
    const form = panelFormulario.querySelector("form");
    const tituloFormulario = document.getElementById("formTitulo");

    const pagina = window.location.pathname.toLowerCase();
    const entidad = pagina.includes("accesosrapidos")
      ? "acceso rapido"
      : pagina.includes("avisos")
        ? "aviso"
        : pagina.includes("tutoriales")
          ? "tutorial"
          : "registro";

    const obtener = (id) => document.getElementById(id);
    const asignarValor = (id, valor) => {
      const campo = obtener(id);
      if (campo) campo.value = valor ?? "";
    };
    const asignarCheck = (id, valor) => {
      const campo = obtener(id);
      if (campo) campo.checked = valor === true || valor === "true";
    };

    const mostrarFormulario = (modo) => {
      if (tituloFormulario) {
        tituloFormulario.textContent =
          modo === "editar" ? `Editar ${entidad}` : `Nuevo ${entidad}`;
      }

      panelFormulario.hidden = false;
      panelFormulario.scrollIntoView({ behavior: "smooth", block: "start" });
    };

    const prepararNuevo = () => {
      if (form) form.reset();

      asignarValor("formId", "0");
      asignarCheck("formActivo", true);
      asignarCheck("formNuevaVentana", true);

      mostrarFormulario("nuevo");

      const primerCampo = panelFormulario.querySelector(
        "input:not([type=hidden]):not([type=file]), textarea"
      );
      if (primerCampo) primerCampo.focus();
    };

    const prepararEdicion = (boton) => {
      const datos = boton.dataset;

      asignarValor("formId", datos.id);
      asignarValor("formNombre", datos.nombre);
      asignarValor("formUrl", datos.url);
      asignarValor("formTituloInput", datos.titulo);
      asignarValor("formContenido", datos.contenido);
      asignarValor("formFecha", datos.fecha);
      asignarValor("formDescripcion", datos.descripcion);
      asignarCheck("formNuevaVentana", datos.nuevaVentana);
      asignarCheck("formActivo", datos.activo);

      mostrarFormulario("editar");
    };

    btnNuevo?.addEventListener("click", prepararNuevo);

    btnCancelar?.addEventListener("click", () => {
      panelFormulario.hidden = true;
      if (form) form.reset();
    });

    document.querySelectorAll(".btn-editar").forEach((boton) => {
      boton.addEventListener("click", () => prepararEdicion(boton));
    });
  }
})();
