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

const newCss = `.eco-servicios {
    position: relative;
    width: 100%;
    padding: clamp(3rem, 6vh, 4.5rem) 0;
    background: #ffffff;
}

.eco-servicios-cab {
    display: flex;
    flex-wrap: wrap;
    align-items: flex-end;
    justify-content: space-between;
    gap: 1.25rem 2rem;
    margin-bottom: clamp(1.75rem, 3.5vh, 2.5rem);
}

.eco-servicios-titulos {
    max-width: 800px;
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    gap: 0.65rem;
}

.eco-h2-oscuro {
    margin: 0;
    font-family: 'Plus Jakarta Sans', 'Inter', system-ui, -apple-system, sans-serif !important;
    font-size: clamp(2rem, 3.4vw, 2.75rem);
    font-weight: 800 !important;
    line-height: 1.15;
    letter-spacing: -0.03em;
    color: #112414;
}

.eco-carrusel-botones {
    display: flex;
    align-items: center;
    gap: 0.75rem;
}

.eco-nav-circulo {
    width: 44px;
    height: 44px;
    display: inline-grid;
    place-items: center;
    border: 0;
    border-radius: 50%;
    background: #15541e;
    color: #ffffff;
    cursor: pointer;
    box-shadow: 0 8px 18px -6px rgba(21, 84, 30, 0.45);
    transition: background-color 0.25s ease, transform 0.25s ease;
}

.eco-nav-circulo:hover {
    background: #0e3b14;
    transform: translateY(-2px);
}

.eco-nav-circulo svg {
    width: 18px;
    height: 18px;
    fill: none;
    stroke: currentColor;
    stroke-width: 2.4;
    stroke-linecap: round;
    stroke-linejoin: round;
}

/* Carrusel de 3 tarjetas verde bosque idéntico a la imagen de referencia */
.eco-tarjetas-carrusel {
    display: grid;
    grid-auto-flow: column;
    grid-auto-columns: calc((100% - 2.5rem) / 3);
    gap: 1.25rem;
    overflow-x: auto;
    scroll-snap-type: x mandatory;
    scroll-behavior: smooth;
    padding: 0.25rem 0.15rem 0.85rem;
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
    padding: 1.15rem 1.15rem 1.75rem;
    border-radius: 26px;
    background: #15541e;
    color: #ffffff;
    box-shadow: 0 16px 36px -12px rgba(16, 60, 24, 0.35);
    transition: transform 0.3s ease, box-shadow 0.3s ease, background-color 0.3s ease;
}

.eco-card-verde:hover {
    transform: translateY(-5px);
    background: #124719;
    box-shadow: 0 22px 42px -12px rgba(16, 60, 24, 0.48);
}

.eco-card-foto {
    position: relative;
    width: 100%;
    aspect-ratio: 16 / 10.5;
    border-radius: 18px;
    overflow: hidden;
    margin-bottom: 1.25rem;
    background: #0f3512;
}

.eco-card-foto img {
    width: 100%;
    height: 100%;
    object-fit: cover;
    display: block;
    transition: transform 0.6s ease;
}

.eco-card-verde:hover .eco-card-foto img {
    transform: scale(1.05);
}

.eco-card-cuerpo {
    display: flex;
    flex-direction: column;
    flex: 1;
    padding: 0 0.25rem;
}

.eco-card-pastilla {
    align-self: flex-start;
    margin-bottom: 0.75rem;
    padding: 0.24rem 0.85rem;
    border: 1px solid rgba(255, 255, 255, 0.3);
    border-radius: 999px;
    background: rgba(255, 255, 255, 0.08);
    color: #d4f796;
    font-size: 0.75rem;
    font-weight: 600;
    letter-spacing: 0.02em;
}

.eco-card-titulo {
    margin: 0 0 0.65rem;
    font-family: 'Plus Jakarta Sans', 'Inter', system-ui, sans-serif !important;
    font-size: 1.32rem;
    font-weight: 800 !important;
    line-height: 1.2;
    color: #ffffff;
}

.eco-card-texto {
    margin: 0;
    font-size: 0.86rem;
    line-height: 1.55;
    color: rgba(255, 255, 255, 0.84);
}

@media (max-width: 991px) {
    .eco-tarjetas-carrusel {
        grid-auto-columns: calc((100% - 1.25rem) / 2);
    }
}

@media (max-width: 640px) {
    .eco-tarjetas-carrusel {
        grid-auto-columns: 88%;
    }
}

`;

css = css.substring(0, startIndex) + newCss + css.substring(endIndex);
fs.writeFileSync(cssPath, css, 'utf8');
console.log('Home.razor.css updated successfully');

