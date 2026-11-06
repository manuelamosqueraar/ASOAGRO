const fs = require('fs');

// 1. Update Home.razor with summarized, complete sentences
const razorPath = 'Components/Pages/Home.razor';
let razor = fs.readFileSync(razorPath, 'utf8');

const t1Old = 'Seguimiento automatizado y oportuno de las fechas límite para la renovación de juntas directivas, actas estatutarias y verificación de quórum legal.';
const t1New = 'Control de fechas límite para la renovación de juntas directivas, actas y quórum estatutario.';

const t2Old = 'Control preventivo de requisitos fiscales, declaraciones y presentación de informes para evitar sanciones y pérdidas en convocatorias.';
const t2New = 'Monitoreo oportuno de obligaciones fiscales y declaraciones tributarias para evitar sanciones.';

const t3Old = 'Articulación directa y fluida entre la directiva central de la FAAUA y las juntas locales que administran las 37 asociaciones de base de la región.';
const t3New = 'Articulación continua entre la directiva central de FAAUA y las 37 asociaciones de base.';

const t4Old = 'Registro detallado de predios, maquinaria, infraestructura productiva e insumos adjudicados a cada asociación para garantizar transparencia y control de recursos.';
const t4New = 'Inventario de predios, maquinaria e infraestructura productiva adjudicada a cada asociación.';

const t5Old = 'Generación automática de informes de gestión transparentes y tableros de control listos para ser presentados ante entidades de control y cooperantes.';
const t5New = 'Informes de gestión y tableros de control listos para entidades aliadas y cooperantes.';

razor = razor.replace(t1Old, t1New);
razor = razor.replace(t2Old, t2New);
razor = razor.replace(t3Old, t3New);
razor = razor.replace(t4Old, t4New);
razor = razor.replace(t5Old, t5New);

fs.writeFileSync(razorPath, razor, 'utf8');
console.log('Home.razor texts summarized successfully');

// 2. Update Home.razor.css to give the section a slightly larger and well-proportioned size
const cssPath = 'Components/Pages/Home.razor.css';
let css = fs.readFileSync(cssPath, 'utf8');

const startMarker = '.eco-servicios {';
const endMarker = '/* ==========================================================================\r\n   4. SECCIÓN VERDE BOSQUE OSCURO';
const endMarkerUnix = '/* ==========================================================================\n   4. SECCIÓN VERDE BOSQUE OSCURO';

const startIndex = css.indexOf(startMarker);
let endIndex = css.indexOf(endMarker);
if (endIndex === -1) endIndex = css.indexOf(endMarkerUnix);

if (startIndex === -1 || endIndex === -1) {
    console.error('Markers not found in Home.razor.css');
    process.exit(1);
}

const newCss = `.eco-servicios {
    position: relative;
    width: 100%;
    padding: clamp(2.2rem, 4.5vh, 3.25rem) 0;
    background: #ffffff;
}

.eco-servicios-cab {
    display: flex;
    flex-wrap: wrap;
    align-items: flex-end;
    justify-content: space-between;
    gap: 1rem 1.75rem;
    margin-bottom: clamp(1.2rem, 2.4vh, 1.8rem);
}

.eco-servicios-titulos {
    max-width: 800px;
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    gap: 0.55rem;
}

.eco-h2-oscuro {
    margin: 0;
    font-family: 'Plus Jakarta Sans', 'Inter', system-ui, -apple-system, sans-serif !important;
    font-size: clamp(1.8rem, 2.8vw, 2.35rem);
    font-weight: 800 !important;
    line-height: 1.15;
    letter-spacing: -0.025em;
    color: #112414;
}

.eco-carrusel-botones {
    display: flex;
    align-items: center;
    gap: 0.75rem;
}

.eco-nav-circulo {
    width: 40px;
    height: 40px;
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
    width: 16px;
    height: 16px;
    fill: none;
    stroke: currentColor;
    stroke-width: 2.4;
    stroke-linecap: round;
    stroke-linejoin: round;
}

/* Carrusel de 3 tarjetas verde bosque */
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
    padding: 1.05rem 1.05rem 1.55rem;
    border-radius: 24px;
    background: #15541e;
    color: #ffffff;
    box-shadow: 0 14px 32px -10px rgba(16, 60, 24, 0.35);
    transition: transform 0.25s ease, box-shadow 0.25s ease, background-color 0.25s ease;
}

.eco-card-verde:hover {
    transform: translateY(-5px);
    background: #124719;
    box-shadow: 0 20px 38px -10px rgba(16, 60, 24, 0.48);
}

.eco-card-foto {
    position: relative;
    width: 100%;
    aspect-ratio: 16 / 10;
    max-height: 195px;
    border-radius: 16px;
    overflow: hidden;
    margin-bottom: 1rem;
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
    padding: 0 0.25rem;
}

.eco-card-pastilla {
    align-self: flex-start;
    margin-bottom: 0.6rem;
    padding: 0.22rem 0.8rem;
    border: 1px solid rgba(255, 255, 255, 0.28);
    border-radius: 999px;
    background: rgba(255, 255, 255, 0.08);
    color: #d4f796;
    font-size: 0.74rem;
    font-weight: 600;
    letter-spacing: 0.02em;
}

.eco-card-titulo {
    margin: 0 0 0.55rem;
    font-family: 'Plus Jakarta Sans', 'Inter', system-ui, sans-serif !important;
    font-size: 1.25rem;
    font-weight: 800 !important;
    line-height: 1.22;
    color: #ffffff;
}

.eco-card-texto {
    margin: 0;
    font-size: 0.85rem;
    line-height: 1.5;
    color: rgba(255, 255, 255, 0.88);
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

