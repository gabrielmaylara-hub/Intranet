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
  // Mostrar/ocultar contraseñas sin enviar formularios.
  // ---------------------------------------------------------------------------
  document.querySelectorAll("[data-password-toggle]").forEach((boton) => {
    boton.addEventListener("click", () => {
      const grupo = boton.closest(".campo-password");
      const input = grupo?.querySelector("input");
      if (!input) return;

      const visible = input.type === "text";
      input.type = visible ? "password" : "text";
      boton.setAttribute("aria-pressed", visible ? "false" : "true");
      boton.setAttribute(
        "aria-label",
        visible ? "Mostrar contraseña" : "Ocultar contraseña"
      );
      boton.classList.toggle("password-toggle--activo", !visible);
    });
  });

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
          : pagina.includes("directorio")
            ? "extension"
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
      asignarValor("formArea", datos.area);
      asignarValor("formExtension", datos.extension);
      asignarValor("formOrden", datos.orden);
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

  const panelAreaFormulario = document.getElementById("panelAreaFormulario");
  const btnNuevaArea = document.getElementById("btnNuevaArea");
  const btnCancelarArea = document.getElementById("btnCancelarArea");

  if (panelAreaFormulario) {
    const formArea = panelAreaFormulario.querySelector("form");
    const tituloArea = document.getElementById("areaFormTitulo");
    const asignarValor = (id, valor) => {
      const campo = document.getElementById(id);
      if (campo) campo.value = valor ?? "";
    };
    const asignarCheck = (id, valor) => {
      const campo = document.getElementById(id);
      if (campo) campo.checked = valor === true || valor === "true";
    };

    const prepararNuevaArea = () => {
      if (formArea) formArea.reset();
      asignarValor("areaFormId", "0");
      asignarValor("areaFormOrden", "0");
      asignarCheck("areaFormActivo", true);

      const nombre = document.getElementById("areaFormNombre");
      if (nombre) nombre.readOnly = false;
      if (tituloArea) tituloArea.textContent = "Nueva area";

      panelAreaFormulario.hidden = false;
      panelAreaFormulario.scrollIntoView({ behavior: "smooth", block: "start" });
      nombre?.focus();
    };

    btnNuevaArea?.addEventListener("click", prepararNuevaArea);

    document.querySelectorAll(".btn-editar-area").forEach((boton) => {
      boton.addEventListener("click", () => {
        const datos = boton.dataset;
        asignarValor("areaFormId", datos.areaId);
        asignarValor("areaFormNombre", datos.areaNombre);
        asignarValor("areaFormTitular", datos.areaTitular);
        asignarValor("areaFormUbicacion", datos.areaUbicacion);
        asignarValor("areaFormCorreo", datos.areaCorreo);
        asignarValor("areaFormOrden", datos.areaOrden);
        asignarCheck("areaFormActivo", datos.areaActivo);

        const nombre = document.getElementById("areaFormNombre");
        if (nombre) nombre.readOnly = true;
        if (tituloArea) tituloArea.textContent = "Editar datos del area";

        panelAreaFormulario.hidden = false;
        panelAreaFormulario.scrollIntoView({ behavior: "smooth", block: "start" });
      });
    });

    btnCancelarArea?.addEventListener("click", () => {
      panelAreaFormulario.hidden = true;
      panelAreaFormulario.querySelector("form")?.reset();
    });
  }

  // ---------------------------------------------------------------------------
  // Paneles colapsables en pantallas administrativas densas.
  // ---------------------------------------------------------------------------
  document.querySelectorAll("[data-colapsable]").forEach((panel) => {
    const boton = panel.querySelector("[data-colapsar]");
    const cuerpo = panel.querySelector("[data-colapsable-contenido]");
    if (!boton || !cuerpo) return;

    boton.addEventListener("click", () => {
      const colapsado = !cuerpo.hidden;
      cuerpo.hidden = colapsado;
      boton.setAttribute("aria-expanded", colapsado ? "false" : "true");
      boton.textContent = colapsado ? "Expandir" : "Contraer";
    });
  });

  // ---------------------------------------------------------------------------
  // Directorio: reordenamiento por arrastrar/soltar dentro de la misma area.
  // ---------------------------------------------------------------------------
  const bloquesDirectorio = document.querySelectorAll(".directorio-area-bloque");

  if (bloquesDirectorio.length > 0) {
    const token = document.querySelector("input[name='__RequestVerificationToken']")?.value || "";
    let filaArrastrada = null;
    let bloqueOrigen = null;

    const filasDeBloque = (bloque) =>
      Array.from(bloque.querySelectorAll(".directorio-reordenable"));

    const actualizarOrdenVisual = (bloque) => {
      filasDeBloque(bloque).forEach((fila, indice) => {
        const orden = String(indice + 1);
        const celdaOrden = fila.querySelector("[data-orden-celda]");
        const botonEditar = fila.querySelector(".btn-editar");
        if (celdaOrden) celdaOrden.textContent = orden;
        if (botonEditar) botonEditar.dataset.orden = orden;
      });
    };

    const guardarOrden = async (bloque) => {
      const ids = filasDeBloque(bloque)
        .map((fila) => Number(fila.dataset.directorioId))
        .filter((id) => Number.isInteger(id) && id > 0);

      if (ids.length === 0) return;

      bloque.classList.add("directorio-guardando");

      try {
        const respuesta = await fetch(`${window.location.pathname}?handler=ReordenarExtensiones`, {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            "X-XSRF-TOKEN": token
          },
          body: JSON.stringify({
            area: bloque.dataset.directorioArea || "",
            ids
          })
        });

        if (!respuesta.ok) {
          const datos = await respuesta.json().catch(() => null);
          throw new Error(datos?.mensaje || "No se pudo guardar el orden.");
        }

        actualizarOrdenVisual(bloque);
      } catch (error) {
        window.alert(error.message || "No se pudo guardar el orden.");
        window.location.reload();
      } finally {
        bloque.classList.remove("directorio-guardando");
      }
    };

    bloquesDirectorio.forEach((bloque) => {
      bloque.addEventListener("dragstart", (evento) => {
        const fila = evento.target.closest(".directorio-reordenable");
        if (!fila || !bloque.contains(fila)) return;

        filaArrastrada = fila;
        bloqueOrigen = bloque;
        fila.classList.add("directorio-dragging");
        evento.dataTransfer.effectAllowed = "move";
      });

      bloque.addEventListener("dragover", (evento) => {
        if (!filaArrastrada || bloqueOrigen !== bloque) return;

        const objetivo = evento.target.closest(".directorio-reordenable");
        if (!objetivo || objetivo === filaArrastrada || !bloque.contains(objetivo)) return;

        evento.preventDefault();
        const caja = objetivo.getBoundingClientRect();
        const insertarDespues = evento.clientY > caja.top + caja.height / 2;
        bloque.insertBefore(filaArrastrada, insertarDespues ? objetivo.nextSibling : objetivo);
      });

      bloque.addEventListener("drop", (evento) => {
        if (!filaArrastrada || bloqueOrigen !== bloque) return;

        evento.preventDefault();
        guardarOrden(bloque);
      });
    });

    document.addEventListener("dragend", () => {
      filaArrastrada?.classList.remove("directorio-dragging");
      filaArrastrada = null;
      bloqueOrigen = null;
    });
  }
})();
