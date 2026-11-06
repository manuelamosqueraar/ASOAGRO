// Mapa del predio (Leaflet + OpenStreetMap / imagen satelital Esri).
// Se carga Leaflet bajo demanda para no afectar al resto de la aplicación.
let mapa = null;
let marcador = null;
let referencia = null;
let cargaLeaflet = null;

function cargarLeaflet() {
    if (window.L) return Promise.resolve();
    if (cargaLeaflet) return cargaLeaflet;

    cargaLeaflet = new Promise((resolve, reject) => {
        const css = document.createElement("link");
        css.rel = "stylesheet";
        css.href = "https://unpkg.com/leaflet@1.9.4/dist/leaflet.css";
        document.head.appendChild(css);

        const js = document.createElement("script");
        js.src = "https://unpkg.com/leaflet@1.9.4/dist/leaflet.js";
        js.onload = () => resolve();
        js.onerror = () => {
            cargaLeaflet = null;
            reject(new Error("No se pudo cargar Leaflet"));
        };
        document.head.appendChild(js);
    });

    return cargaLeaflet;
}

function icono() {
    const html =
        '<div style="width:38px;height:46px;position:relative;filter:drop-shadow(0 4px 4px rgba(0,0,0,.35))">' +
        '<svg viewBox="0 0 38 46" width="38" height="46" xmlns="http://www.w3.org/2000/svg">' +
        '<path d="M19 45C19 45 3 28.5 3 17.5C3 8.4 10.2 1.5 19 1.5C27.8 1.5 35 8.4 35 17.5C35 28.5 19 45 19 45Z" fill="#17372a" stroke="#fff" stroke-width="2"/>' +
        '<circle cx="19" cy="17.5" r="6.5" fill="#c8e86a"/></svg></div>';
    return L.divIcon({ html, className: "", iconSize: [38, 46], iconAnchor: [19, 44] });
}

function notificar(lat, lng) {
    if (referencia) {
        referencia.invokeMethodAsync("AlPuntoSeleccionado", lat, lng);
    }
}

export async function iniciar(idContenedor, ref, lat, lng, zoom, conMarcador) {
    destruir();
    referencia = ref;

    const contenedor = document.getElementById(idContenedor);
    if (!contenedor) return;

    try {
        await cargarLeaflet();
    } catch (e) {
        contenedor.innerHTML =
            '<div style="padding:2rem;text-align:center;color:#55675c;font-size:.9rem">' +
            "No se pudo cargar el mapa. Verifique la conexión a internet.</div>";
        return;
    }

    if (!document.getElementById(idContenedor)) return;

    mapa = L.map(contenedor, { zoomControl: true, scrollWheelZoom: true }).setView([lat, lng], zoom);

    const calles = L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
        maxZoom: 19,
        attribution: "&copy; OpenStreetMap"
    });

    const satelite = L.tileLayer(
        "https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}",
        { maxZoom: 19, attribution: "Imagen &copy; Esri, Maxar, Earthstar Geographics" }
    );

    calles.addTo(mapa);
    L.control.layers({ "Mapa": calles, "Satélite": satelite }, null, { position: "topright" }).addTo(mapa);

    if (conMarcador) {
        ponerMarcador(lat, lng);
    }

    mapa.on("click", (e) => {
        ponerMarcador(e.latlng.lat, e.latlng.lng);
        notificar(e.latlng.lat, e.latlng.lng);
    });

    setTimeout(() => mapa && mapa.invalidateSize(), 150);
}

export function ponerMarcador(lat, lng) {
    if (!mapa) return;
    if (marcador) {
        marcador.setLatLng([lat, lng]);
        return;
    }

    marcador = L.marker([lat, lng], { draggable: true, icon: icono() }).addTo(mapa);
    marcador.on("dragend", () => {
        const p = marcador.getLatLng();
        notificar(p.lat, p.lng);
    });
}

export function centrar(lat, lng, zoom, mover) {
    if (!mapa) return;
    mapa.flyTo([lat, lng], zoom, { duration: 0.9 });
    if (mover) {
        ponerMarcador(lat, lng);
    }
}

export function miUbicacion() {
    if (!mapa || !navigator.geolocation) return false;
    navigator.geolocation.getCurrentPosition(
        (pos) => {
            const { latitude, longitude } = pos.coords;
            mapa.flyTo([latitude, longitude], 16, { duration: 0.9 });
            ponerMarcador(latitude, longitude);
            notificar(latitude, longitude);
        },
        () => { },
        { enableHighAccuracy: true, timeout: 10000 }
    );
    return true;
}

export function destruir() {
    if (mapa) {
        mapa.remove();
        mapa = null;
        marcador = null;
    }
}

