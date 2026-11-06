const fs = require('fs');

const cssPath = 'Components/Pages/Home.razor.css';
let css = fs.readFileSync(cssPath, 'utf8');

const startMarker = '.eco-servicios {';
const endMarker = '/* ==========================================================================\r\n   4. SECCIÓN VERDE BOSQUE OSCURO';
const endMarkerUnix = '/* ==========================================================================\n   4. SECCIÓN VERDE BOSQUE OSCURO';

const startIndex = css.indexOf(startMarker);
let endIndex = css.indexOf(endMarker);
if (endIndex === -1) endIndex = css.indexOf(endMarkerUnix);

if (startIndex === -1 || endIndex === -1) {
    console.error('Markers not found');
    process.exit(1);
}

const compactCss = `.eco-servicios {
    position: relative;
    width: 100%;
    padding: clamp(1.5rem, 3.2vh, 2.25rem) 0;
    background: #ffffff;
}

.eco-servicios-cab {
    display: flex;
    flex-wrap: wrap;
    align-items: flex-end;
    justify-content: space-between;
    gap: 0.85rem 1.5rem;
    margin-bottom: clamp(0.9rem, 1.8vh, 1.35rem);
}

.eco-servicios-titulos {
    max-width: 800px;
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    gap: 0.45rem;
}

.eco-h2-oscuro {
    margin: 0;
    font-family: 'Plus Jakarta Sans', 'Inter', system-ui, -apple-system, sans-serif !important;
    font-size: clamp(1.55rem, 2.4vw, 2.05rem);
    font-weight: 800 !important;
    line-height: 1.15;
    letter-spacing: -0.025em;
    color: #112414;
}

.eco-carrusel-botones {
    display: flex;
    align-items: center;
    gap: 0.6rem;
}

.eco-nav-circulo {
    width: 36px;
    height: 36px;
    display: inline-grid;
    place-items: center;
    border: 0;
    border-radius: 50%;
    background: #15541e;
    color: #ffffff;
    cursor: pointer;
    box-shadow: 0 6px 14px -5px rgba(21, 84, 30, 0.45);
    transition: background-color 0.25s ease, transform 0.25s ease;
}

.eco-nav-circulo:hover {
    background: #0e3b14;
    transform: translateY(-2px);
}

.eco-nav-circulo svg {
    width: 15px;
    height: 15px;
    fill: none;
    stroke: currentColor;
    stroke-width: 2.4;
    stroke-linecap: round;
    stroke-linejoin: round;
}

/* Carrusel de 3 tarjetas verde bosque proporcionado para caber completo en pantalla */
.eco-tarjetas-carrusel {
    display: grid;
    grid-auto-flow: column;
    grid-auto-columns: calc((100% - 2rem) / 3);
    gap: 1rem;
    overflow-x: auto;
    scroll-snap-type: x mandatory;
    scroll-behavior: smooth;
    padding: 0.2rem 0.1rem 0.6rem;
    scrollbar-width: none;
    -ms-overflow-style: none;
}

.eco-tarjetas-carrusel::-webkit-scrollbar {
    display: none;
}

.eco-card-verde {
    scroll-snap-align: start;
    display: flex;
    flex-direction: column;
    padding: 0.85rem 0.85rem 1.25rem;
    border-radius: 20px;
    background: #15541e;
    color: #ffffff;
    box-shadow: 0 12px 28px -10px rgba(16, 60, 24, 0.32);
    transition: transform 0.25s ease, box-shadow 0.25s ease, background-color 0.25s ease;
}

.eco-card-verde:hover {
    transform: translateY(-4px);
    background: #124719;
    box-shadow: 0 18px 34px -10px rgba(16, 60, 24, 0.45);
}

.eco-card-foto {
    position: relative;
    width: 100%;
    aspect-ratio: 16 / 9.5;
    max-height: 165px;
    border-radius: 14px;
    overflow: hidden;
    margin-bottom: 0.85rem;
    background: #0f3512;
}

.eco-card-foto img {
    width: 100%;
    height: 100%;
    object-fit: cover;
    display: block;
    transition: transform 0.5s ease;
}

.eco-card-verde:hover .eco-card-foto img {
    transform: scale(1.05);
}

.eco-card-cuerpo {
    display: flex;
    flex-direction: column;
    flex: 1;
    padding: 0 0.15rem;
}

.eco-card-pastilla {
    align-self: flex-start;
    margin-bottom: 0.45rem;
    padding: 0.18rem 0.7rem;
    border: 1px solid rgba(255, 255, 255, 0.28);
    border-radius: 999px;
    background: rgba(255, 255, 255, 0.08);
    color: #d4f796;
    font-size: 0.71rem;
    font-weight: 600;
    letter-spacing: 0.02em;
}

.eco-card-titulo {
    margin: 0 0 0.45rem;
    font-family: 'Plus Jakarta Sans', 'Inter', system-ui, sans-serif !important;
    font-size: 1.15rem;
    font-weight: 800 !important;
    line-height: 1.2;
    color: #ffffff;
}

.eco-card-texto {
    margin: 0;
    font-size: 0.81rem;
    line-height: 1.45;
    color: rgba(255, 255, 255, 0.84);
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
}

@media (max-width: 991px) {
    .eco-tarjetas-carrusel {
        grid-auto-columns: calc((100% - 1rem) / 2);
    }
}

@media (max-width: 640px) {
    .eco-tarjetas-carrusel {
        grid-auto-columns: 88%;
    }
}

`;

css = css.substring(0, startIndex) + compactCss + css.substring(endIndex);
fs.writeFileSync(cssPath, css, 'utf8');
console.log('Home.razor.css made compact successfully');

