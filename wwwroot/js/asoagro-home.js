// Animaciones de la página de inicio de Asoagro.
// Usa IntersectionObserver y scroll normal, así que funciona en todos los navegadores modernos.

const limpiezas = [];
const observadores = [];
const reducido = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
const CX = 280, CY = 230, RADIO = 160;

let pendiente = false;
let barraPagina = null;
let carrusel = null;
let barraCarrusel = null;
let grafico = null;
let nodos = [];
let lineas = [];

const limitar = (v, min, max) => Math.min(max, Math.max(min, v));

// Paralaje de fondos y desvanecido del texto (Excluyendo la Sección 1 / Hero)
function paralaje() {
    if (reducido) return;
    const vh = window.innerHeight;

    document.querySelectorAll('[data-parallax]').forEach(el => {
        const seccion = el.closest('section') || el.parentElement;
        
        // Excluir la sección 1 (Hero) para que su imagen de fondo quede estática
        if (seccion.classList.contains('ag-hero') || seccion.querySelector('.img-hero-1')) return;

        const r = seccion.getBoundingClientRect();
        const desplazamiento = (r.top + r.height / 2) - vh / 2;
        const velocidad = parseFloat(el.dataset.parallax) || 0;
        el.style.transform = `translate3d(0, ${limitar(-desplazamiento * velocidad, -120, 120).toFixed(1)}px, 0)`;
    });

    document.querySelectorAll('[data-desvanece]').forEach(el => {
        const seccion = el.closest('section') || el.parentElement;
        
        // Excluir la sección 1 (Hero) para que su texto no se desvanezca al hacer scroll
        if (seccion.classList.contains('ag-hero') || seccion.querySelector('.ag-hero-txt')) return;

        const r = seccion.getBoundingClientRect();
        const avance = limitar(-r.top / (r.height * 0.7), 0, 1);
        el.style.opacity = (1 - avance * 0.85).toFixed(3);
    });
}

// Gráfico: el directorio gira según el scroll y los nodos se mantienen sobre la órbita
function orbitar() {
    if (reducido || !grafico || !nodos.length) return;
    const r = grafico.getBoundingClientRect();
    const avance = limitar((window.innerHeight - r.top) / (window.innerHeight + r.height), 0, 1);
    const giro = avance * 80;

    nodos.forEach((nodo, i) => {
        const angulo = (parseFloat(nodo.dataset.nodo) + giro) * Math.PI / 180;
        const x = (CX + RADIO * Math.cos(angulo)).toFixed(1);
        const y = (CY + RADIO * Math.sin(angulo)).toFixed(1);
        nodo.setAttribute('transform', `translate(${x} ${y})`);
        if (lineas[i]) lineas[i].setAttribute('d', `M${CX} ${CY} L${x} ${y}`);
    });
}

// Barra de progreso del carrusel
function progresoCarrusel() {
    if (!carrusel || !barraCarrusel) return;
    const max = carrusel.scrollWidth - carrusel.clientWidth;
    const avance = max > 0 ? carrusel.scrollLeft / max : 0;
    barraCarrusel.style.transform = `scaleX(${avance.toFixed(4)})`;
}

// Barra de progreso de la página
function progresoPagina() {
    if (!barraPagina) return;
    const max = document.documentElement.scrollHeight - window.innerHeight;
    const avance = max > 0 ? window.scrollY / max : 0;
    barraPagina.style.transform = `scaleX(${limitar(avance, 0, 1).toFixed(4)})`;
}

function actualizar() {
    pendiente = false;
    paralaje();
    orbitar();
    progresoPagina();
}

function alMoverse() {
    if (!pendiente) {
        pendiente = true;
        requestAnimationFrame(actualizar);
    }
}

export function iniciar() {
    document.documentElement.classList.add('ag-js');

    barraPagina = document.querySelector('[data-progreso]');
    carrusel = document.getElementById('carruselGobernanza');
    barraCarrusel = document.querySelector('[data-barra]');
    grafico = document.querySelector('[data-orbita]');
    nodos = grafico ? [...grafico.querySelectorAll('[data-nodo]')] : [];
    lineas = grafico ? [...grafico.querySelectorAll('[data-linea]')] : [];

    // Revelado de elementos al entrar en pantalla
    const observador = new IntersectionObserver(entradas => {
        entradas.forEach(e => {
            if (e.isIntersecting) {
                e.target.classList.add('ag-visible');
                observador.unobserve(e.target);
            }
        });
    }, { threshold: 0.15, rootMargin: '0px 0px -8% 0px' });
    document.querySelectorAll('[data-reveal]').forEach(el => observador.observe(el));
    observadores.push(observador);

    // Carrusel: barra de progreso y botones de navegación
    if (carrusel) {
        carrusel.addEventListener('scroll', progresoCarrusel, { passive: true });
        limpiezas.push(() => carrusel.removeEventListener('scroll', progresoCarrusel));
    }
    document.querySelectorAll('[data-carrusel]').forEach(boton => {
        const mover = () => carrusel?.scrollBy({ left: Number(boton.dataset.carrusel) * carrusel.clientWidth * 0.9, behavior: 'smooth' });
        boton.addEventListener('click', mover);
        limpiezas.push(() => boton.removeEventListener('click', mover));
    });

    window.addEventListener('scroll', alMoverse, { passive: true });
    window.addEventListener('resize', alMoverse, { passive: true });
    limpiezas.push(() => {
        window.removeEventListener('scroll', alMoverse);
        window.removeEventListener('resize', alMoverse);
    });

    progresoCarrusel();
    alMoverse();
}

export function destruir() {
    observadores.splice(0).forEach(o => o.disconnect());
    limpiezas.splice(0).forEach(fn => fn());
    document.documentElement.classList.remove('ag-js');
}