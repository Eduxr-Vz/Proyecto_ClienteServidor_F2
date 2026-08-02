// Notificaciones en tiempo real con SignalR.
// Abre una conexión permanente con el servidor y muestra un aviso cada vez
// que alguien crea, actualiza o elimina un ticket, sin recargar la página.

(function () {
    "use strict";

    if (typeof signalR === "undefined") {
        console.warn("No se cargó la librería de SignalR; las notificaciones quedan desactivadas.");
        return;
    }

    var contenedor = document.getElementById("contenedor-notificaciones");
    var indicador = document.getElementById("estado-conexion");
    var contador = document.getElementById("contador-pendientes");

    // withAutomaticReconnect vuelve a conectar solo si se cae la red o se
    // reinicia el servidor, sin que el usuario tenga que recargar.
    var conexion = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/notificaciones")
        .withAutomaticReconnect()
        .build();

    var ESTILOS = {
        creado: { clase: "text-bg-primary", icono: "+" },
        actualizado: { clase: "text-bg-secondary", icono: "~" },
        estado: { clase: "text-bg-warning", icono: "!" },
        eliminado: { clase: "text-bg-danger", icono: "x" }
    };

    function mostrarAviso(n) {
        if (!contenedor) return;

        var estilo = ESTILOS[n.tipo] || ESTILOS.actualizado;
        var hora = new Date(n.fecha).toLocaleTimeString("es-MX", {
            hour: "2-digit",
            minute: "2-digit"
        });

        var toast = document.createElement("div");
        toast.className = "toast";
        toast.setAttribute("role", "alert");
        toast.setAttribute("aria-live", "polite");
        toast.innerHTML =
            '<div class="toast-header ' + estilo.clase + '">' +
                '<strong class="me-auto">' + estilo.icono + " " + escapar(n.titulo) + "</strong>" +
                '<small class="ms-2">' + hora + "</small>" +
                '<button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast" aria-label="Cerrar"></button>' +
            "</div>" +
            '<div class="toast-body">' + escapar(n.mensaje) +
                (n.tipo !== "eliminado"
                    ? '<div class="mt-2"><a href="/Tickets/Details/' + n.idTicket +
                      '" class="btn btn-sm btn-outline-dark">Ver ticket</a></div>'
                    : "") +
            "</div>";

        contenedor.appendChild(toast);
        new bootstrap.Toast(toast, { delay: 8000 }).show();
        toast.addEventListener("hidden.bs.toast", function () { toast.remove(); });
    }

    function actualizarContador(pendientes) {
        if (!contador || typeof pendientes !== "number") return;
        contador.textContent = pendientes;
        contador.classList.toggle("d-none", pendientes === 0);
    }

    function marcarConexion(conectado) {
        if (!indicador) return;
        indicador.className = conectado
            ? "badge text-bg-success"
            : "badge text-bg-secondary";
        indicador.textContent = conectado ? "En vivo" : "Sin conexión";
        indicador.title = conectado
            ? "Recibiendo notificaciones en tiempo real"
            : "Se perdió la conexión con el servidor; reintentando...";
    }

    // Evita que un texto con < o > rompa el HTML del aviso.
    function escapar(texto) {
        var div = document.createElement("div");
        div.textContent = texto || "";
        return div.innerHTML;
    }

    conexion.on("RecibirNotificacion", function (n) {
        mostrarAviso(n);
        actualizarContador(n.ticketsPendientes);
    });

    conexion.onreconnecting(function () { marcarConexion(false); });
    conexion.onreconnected(function () { marcarConexion(true); });
    conexion.onclose(function () { marcarConexion(false); });

    conexion.start()
        .then(function () { marcarConexion(true); })
        .catch(function (err) {
            marcarConexion(false);
            console.error("No se pudo conectar al hub de notificaciones:", err);
        });
})();
