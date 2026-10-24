// Traducción de la página con el widget de Google Translate.
// Al elegir EN se guarda una cookie y la página se recarga: Google traduce al cargar.

const IDIOMA_ORIGEN = 'es';

function leerIdioma() {
    const coincidencia = document.cookie.match(/(?:^|;\s*)googtrans=\/es\/([a-z]{2})/);
    return coincidencia ? coincidencia[1] : IDIOMA_ORIGEN;
}

function borrarCookie() {
    const caducar = 'expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/';
    document.cookie = `googtrans=; ${caducar}`;

    // En un dominio real, Google puede haber guardado la cookie para el dominio
    const partes = location.hostname.split('.');
    if (partes.length > 1) {
        document.cookie = `googtrans=; ${caducar}; domain=${location.hostname}`;
        document.cookie = `googtrans=; ${caducar}; domain=.${location.hostname}`;
    }
}

function cargarGoogle() {
    if (document.getElementById('gt-cargador')) return;

    const contenedor = document.createElement('div');
    contenedor.id = 'gt-oculto';
    contenedor.style.display = 'none';
    document.body.appendChild(contenedor);

    window.googleTranslateElementInit = () => {
        new google.translate.TranslateElement({
            pageLanguage: IDIOMA_ORIGEN,
            includedLanguages: 'en,es',
            autoDisplay: false
        }, 'gt-oculto');
    };

    const script = document.createElement('script');
    script.id = 'gt-cargador';
    script.src = 'https://translate.google.com/translate_a/element.js?cb=googleTranslateElementInit';
    script.async = true;
    document.body.appendChild(script);
}

export function obtenerIdioma() {
    return leerIdioma();
}

export function iniciar() {
    if (leerIdioma() === 'en') cargarGoogle();
}

export function cambiar(idioma) {
    if (idioma === 'en') {
        document.cookie = `googtrans=/${IDIOMA_ORIGEN}/en; path=/`;
    } else {
        borrarCookie();
    }
    location.reload();
}