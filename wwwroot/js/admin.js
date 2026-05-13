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
  // Configuracion: pestañas internas sin recargar ni perder valores escritos.
  // ---------------------------------------------------------------------------
  document.querySelectorAll("[data-config-tabs]").forEach((contenedor) => {
    const tabs = Array.from(contenedor.querySelectorAll("[data-config-tab]"));
    const panels = Array.from(contenedor.querySelectorAll("[data-config-panel]"));
    const inputTab = contenedor.querySelector("#configTabActiva");

    if (tabs.length === 0 || panels.length === 0) return;

    const activar = (nombre, actualizarHash = false) => {
      const existe = tabs.some((tab) => tab.dataset.configTab === nombre);
      const tabActiva = existe ? nombre : tabs[0].dataset.configTab;

      tabs.forEach((tab) => {
        const activo = tab.dataset.configTab === tabActiva;
        tab.classList.toggle("admin-tab--activo", activo);
        tab.setAttribute("aria-selected", activo ? "true" : "false");
      });

      panels.forEach((panel) => {
        panel.hidden = panel.dataset.configPanel !== tabActiva;
      });

      if (inputTab) inputTab.value = tabActiva;

      if (actualizarHash && tabActiva) {
        const url = `${window.location.pathname}${window.location.search}#config-${tabActiva}`;
        window.history.replaceState(null, "", url);
      }
    };

    tabs.forEach((tab) => {
      tab.addEventListener("click", () => activar(tab.dataset.configTab, true));
    });

    contenedor.querySelectorAll("[data-config-submit-tab]").forEach((boton) => {
      boton.addEventListener("click", () => {
        if (inputTab && boton.dataset.configSubmitTab) {
          inputTab.value = boton.dataset.configSubmitTab;
        }
      });
    });

    const hash = window.location.hash.replace("#config-", "");
    const inicial = tabs.some((tab) => tab.dataset.configTab === hash)
      ? hash
      : inputTab?.value;

    activar(inicial || "identidad");
  });

  // ---------------------------------------------------------------------------
  // Usuarios: el area solo aplica cuando el rol es usuario_area.
  // ---------------------------------------------------------------------------
  const rolUsuario = document.querySelector("[data-usuario-rol]");
  const areaUsuario = document.querySelector("[data-usuario-area-select]");
  const ayudaAreaUsuario = document.querySelector("[data-usuario-area-ayuda]");
  const requeridoAreaUsuario = document.querySelector("[data-usuario-area-requerida]");

  if (rolUsuario && areaUsuario) {
    const sincronizarAreaUsuario = () => {
      const requiereArea = rolUsuario.value === "usuario_area";

      areaUsuario.disabled = !requiereArea;
      areaUsuario.required = requiereArea;
      if (!requiereArea) areaUsuario.value = "";

      if (requeridoAreaUsuario) {
        requeridoAreaUsuario.hidden = !requiereArea;
      }

      if (ayudaAreaUsuario) {
        ayudaAreaUsuario.textContent = requiereArea
          ? "Obligatoria para usuario_area."
          : "No aplica para admin_general.";
      }
    };

    rolUsuario.addEventListener("change", sincronizarAreaUsuario);
    sincronizarAreaUsuario();
  }

  // ---------------------------------------------------------------------------
  // Usuarios: el alta inicia cerrada y solo se abre cuando el admin la solicita.
  // ---------------------------------------------------------------------------
  const panelUsuariosForm = document.querySelector("[data-usuarios-form]");
  const btnNuevoUsuario = document.querySelector("[data-usuarios-nuevo]");
  const btnCancelarUsuario = document.querySelector("[data-usuarios-cancelar]");

  if (panelUsuariosForm) {
    const formUsuarios = panelUsuariosForm.querySelector("form");
    const primerCampoUsuario = panelUsuariosForm.querySelector("#Usuario");

    const sincronizarRolUsuario = () => {
      if (rolUsuario) {
        rolUsuario.dispatchEvent(new Event("change"));
      }
    };

    btnNuevoUsuario?.addEventListener("click", () => {
      if (formUsuarios) formUsuarios.reset();
      panelUsuariosForm.hidden = false;
      sincronizarRolUsuario();
      panelUsuariosForm.scrollIntoView({ behavior: "smooth", block: "start" });
      primerCampoUsuario?.focus();
    });

    btnCancelarUsuario?.addEventListener("click", () => {
      if (formUsuarios) formUsuarios.reset();
      panelUsuariosForm.hidden = true;
      sincronizarRolUsuario();
    });
  }

  // ---------------------------------------------------------------------------
  // Confirmaciones simples antes de acciones administrativas sensibles.
  // ---------------------------------------------------------------------------
  document.querySelectorAll("[data-confirm-submit]").forEach((formulario) => {
    formulario.addEventListener("submit", (evento) => {
      const mensaje = formulario.dataset.confirmSubmit || "Confirma la accion.";
      if (!window.confirm(mensaje)) {
        evento.preventDefault();
      }
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
      asignarValor("formAreaPublicacionId", datos.areaPublicacionId);
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
    const estadoOrden = document.querySelector("[data-directorio-orden-estado]");
    let filaArrastrada = null;
    let bloqueOrigen = null;
    let firmaOrdenInicial = "";

    const filasDeBloque = (bloque) =>
      Array.from(bloque.querySelectorAll(".directorio-reordenable"));

    const obtenerFirmaOrden = (bloque) =>
      filasDeBloque(bloque)
        .map((fila) => fila.dataset.directorioId || "")
        .join(",");

    const mostrarEstadoOrden = (mensaje, esError = false) => {
      if (!estadoOrden) return;
      estadoOrden.textContent = mensaje;
      estadoOrden.classList.toggle("directorio-orden-estado--error", esError);
      estadoOrden.classList.toggle("directorio-orden-estado--ok", !esError && mensaje.length > 0);

      if (mensaje && !esError) {
        window.setTimeout(() => {
          if (estadoOrden.textContent === mensaje) {
            estadoOrden.textContent = "";
            estadoOrden.classList.remove("directorio-orden-estado--ok");
          }
        }, 2400);
      }
    };

    const actualizarOrdenVisual = (bloque) => {
      filasDeBloque(bloque).forEach((fila, indice) => {
        const orden = String(indice + 1);
        const celdaOrden = fila.querySelector("[data-orden-celda]");
        const botonEditar = fila.querySelector(".btn-editar");
        if (celdaOrden) celdaOrden.textContent = orden;
        if (botonEditar) botonEditar.dataset.orden = orden;
      });
    };

    const guardarOrden = async (bloque, firmaAnterior) => {
      const ids = filasDeBloque(bloque)
        .map((fila) => Number(fila.dataset.directorioId))
        .filter((id) => Number.isInteger(id) && id > 0);
      const areaId = Number(bloque.dataset.directorioAreaId);

      if (ids.length === 0 || !Number.isInteger(areaId) || areaId <= 0) return;
      if (obtenerFirmaOrden(bloque) === firmaAnterior) return;

      bloque.classList.add("directorio-guardando");
      mostrarEstadoOrden("Guardando orden...");

      try {
        const respuesta = await fetch(`${window.location.pathname}?handler=ReordenarExtensiones`, {
          method: "POST",
          credentials: "same-origin",
          headers: {
            "Content-Type": "application/json",
            "X-XSRF-TOKEN": token
          },
          body: JSON.stringify({
            areaId,
            ids
          })
        });

        if (!respuesta.ok) {
          const datos = await respuesta.json().catch(() => null);
          throw new Error(datos?.mensaje || "No se pudo guardar el orden.");
        }

        actualizarOrdenVisual(bloque);
        bloque.dataset.directorioOrdenOriginal = obtenerFirmaOrden(bloque);
        mostrarEstadoOrden("Orden guardado.");
      } catch (error) {
        mostrarEstadoOrden(error.message || "No se pudo guardar el orden.", true);
        window.alert(error.message || "No se pudo guardar el orden.");
        window.location.reload();
      } finally {
        bloque.classList.remove("directorio-guardando");
      }
    };

    bloquesDirectorio.forEach((bloque) => {
      bloque.dataset.directorioOrdenOriginal = obtenerFirmaOrden(bloque);

      bloque.addEventListener("dragstart", (evento) => {
        const fila = evento.target.closest(".directorio-reordenable");
        if (!fila || !bloque.contains(fila)) return;

        filaArrastrada = fila;
        bloqueOrigen = bloque;
        firmaOrdenInicial = obtenerFirmaOrden(bloque);
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
      });
    });

    document.addEventListener("dragend", () => {
      const bloqueParaGuardar = bloqueOrigen;
      const firmaAnterior = firmaOrdenInicial;

      filaArrastrada?.classList.remove("directorio-dragging");
      filaArrastrada = null;
      bloqueOrigen = null;
      firmaOrdenInicial = "";

      if (bloqueParaGuardar) {
        guardarOrden(bloqueParaGuardar, firmaAnterior);
      }
    });
  }
})();
