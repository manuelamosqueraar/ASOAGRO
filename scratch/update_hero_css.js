const fs = require('fs');

const cssPath = 'Components/Pages/Home.razor.css';
let css = fs.readFileSync(cssPath, 'utf8');

// Replace lines from /* 1. HERO ... to .ag-entra
const startMarker = '/* ==========================================================================\r\n   1. HERO (COMPOSICIÓN IZQUIERDA CON SOL DORADO Y VIDEO DE FONDO)\r\n   ========================================================================== */';
const startMarkerUnix = '/* ==========================================================================\n   1. HERO (COMPOSICIÓN IZQUIERDA CON SOL DORADO Y VIDEO DE FONDO)\n   ========================================================================== */';

const endMarker = '.ag-entra {';

let startIndex = css.indexOf(startMarker);
if (startIndex === -1) {
    startIndex = css.indexOf(startMarkerUnix);
}

if (startIndex === -1) {
    console.error('Could not find start marker');
    process.exit(1);
}

const endIndex = css.indexOf(endMarker, startIndex);
if (endIndex === -1) {
    console.error('Could not find end marker');
    process.exit(1);
}

const newHeroCss = `/* ==========================================================================
   1. HERO — ESTILO LAWN BUSTER (100% PANTALLA, ALINEADO A LA IZQUIERDA, TIPOGRAFÍA DUAL)
   ========================================================================== */
.eco-hero {
    position: relative;
    display: flex;
    align-items: center;
    width: 100%;
    height: 100vh;
    height: 100svh;
    min-height: 640px;
    padding: clamp(6.5rem, 12vh, 8.5rem) 0 clamp(2rem, 5vh, 3.5rem);
    background-color: var(--eco-bosque-osc);
    overflow: hidden;
    box-sizing: border-box;
}

.eco-hero .ag-capa {
    position: absolute;
    inset: 0;
    z-index: 0;
}

.ag-hero-video {
    position: absolute;
    inset: 0;
    width: 100%;
    height: 100%;
    object-fit: cover;
    object-position: center;
}

/* Degradado cinematográfico que realza la legibilidad del texto blanco a la izquierda */
.eco-hero-velo {
    position: absolute;
    inset: 0;
    z-index: 1;
    background:
        linear-gradient(90deg, rgba(8, 26, 12, 0.94) 0%, rgba(10, 32, 15, 0.8) 42%, rgba(14, 38, 17, 0.38) 75%, rgba(10, 28, 13, 0.55) 100%),
        linear-gradient(180deg, rgba(6, 20, 9, 0.6) 0%, transparent 35%, rgba(6, 20, 9, 0.75) 100%);
    pointer-events: none;
}

.eco-hero-inner {
    display: flex;
    align-items: flex-end;
    justify-content: space-between;
    width: 100%;
    height: 100%;
    gap: 2.5rem;
}

/* Columna Izquierda: Todo estrictamente alineado a la izquierda como la imagen Lawn Buster */
.eco-hero-caja {
    flex: 1 1 660px;
    max-width: 740px;
    text-align: left;
    color: #ffffff;
    align-self: center;
    margin: 0;
}

/* Rating Badge estilo Facebook / Google Reviews */
.eco-hero-rating {
    display: inline-flex;
    align-items: center;
    gap: 0.75rem;
    margin-bottom: 1.15rem;
}

.eco-rating-icono {
    display: grid;
    place-items: center;
    width: 36px;
    height: 36px;
    border-radius: 50%;
    background: #1877f2;
    color: #ffffff;
    font-size: 0.62rem;
    font-weight: 800;
    letter-spacing: -0.02em;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
}

.eco-rating-texto {
    display: flex;
    flex-direction: column;
    gap: 0.15rem;
    text-align: left;
}

.eco-rating-estrellas {
    display: flex;
    align-items: center;
    gap: 0.2rem;
    color: #f59e0b;
    font-size: 0.95rem;
    line-height: 1;
}

.eco-estrella {
    color: #f59e0b;
}

.eco-rating-estrellas strong {
    margin-left: 0.35rem;
    color: #ffffff;
    font-size: 0.95rem;
    font-weight: 800;
}

.eco-rating-texto small {
    color: rgba(255, 255, 255, 0.82);
    font-size: 0.76rem;
    font-weight: 500;
}

/* Chips de categorías */
.eco-hero-chips {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 0.55rem;
    margin-bottom: 0.9rem;
}

.eco-chip-hero {
    display: inline-flex;
    align-items: center;
    padding: 0.32rem 0.9rem;
    border-radius: 999px;
    background: rgba(0, 0, 0, 0.38);
    border: 1px solid rgba(255, 255, 255, 0.22);
    backdrop-filter: blur(8px);
    color: #ffffff;
    font-size: 0.78rem;
    font-weight: 600;
    letter-spacing: 0.02em;
}

/* Titular monumental con tipografía dual exactamente como la referencia */
.eco-hero-titulo {
    margin: 0.75rem 0 1.25rem;
    font-family: 'Plus Jakarta Sans', 'Outfit', sans-serif !important;
    font-size: clamp(2.8rem, 5.2vw, 4.8rem);
    font-weight: 800 !important;
    line-height: 1.05;
    letter-spacing: -0.03em;
    color: #ffffff;
    text-shadow: 0 4px 26px rgba(0, 0, 0, 0.45);
    text-align: left;
}

/* Serif Itálica fluida idéntica al diseño Lawn Buster */
.eco-serif-italica {
    font-family: 'Instrument Serif', 'Playfair Display', Georgia, serif !important;
    font-style: italic !important;
    font-weight: 400 !important;
    letter-spacing: normal;
}

.eco-hero-desc {
    margin: 0 0 2rem;
    max-width: 580px;
    font-size: clamp(0.96rem, 1.12vw, 1.1rem);
    line-height: 1.65;
    color: rgba(255, 255, 255, 0.88);
    text-align: left;
}

.eco-hero-acciones {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 1.1rem;
}

/* Botón primario estilo Lawn Buster: Cápsula lima con círculo negro y flecha diagonal */
.eco-btn-lb {
    display: inline-flex;
    align-items: center;
    gap: 0.9rem;
    padding: 0.45rem 0.55rem 0.45rem 1.45rem;
    border-radius: 999px;
    background: #cbf264;
    color: #0c2210;
    font-family: 'Plus Jakarta Sans', system-ui, sans-serif;
    font-size: 0.96rem;
    font-weight: 700;
    text-decoration: none;
    box-shadow: 0 12px 24px -8px rgba(184, 235, 88, 0.45);
    transition: transform 0.25s ease, background-color 0.25s ease, box-shadow 0.25s ease;
}

.eco-btn-lb:hover {
    background: #bbf048;
    color: #0c2210;
    transform: translateY(-2px);
    box-shadow: 0 16px 30px -8px rgba(184, 235, 88, 0.6);
}

.eco-btn-lb-flecha {
    width: 34px;
    height: 34px;
    flex: 0 0 34px;
    border-radius: 50%;
    background: #0c2210;
    color: #cbf264;
    display: grid;
    place-items: center;
    transition: transform 0.25s ease;
}

.eco-btn-lb:hover .eco-btn-lb-flecha {
    transform: translate(2px, -2px);
}

.eco-btn-lb-flecha svg {
    width: 16px;
    height: 16px;
    fill: none;
    stroke: currentColor;
    stroke-width: 2.5;
    stroke-linecap: round;
    stroke-linejoin: round;
}

/* Enlace secundario con flecha */
.eco-link-secundario {
    display: inline-flex;
    align-items: center;
    gap: 0.45rem;
    padding: 0.5rem 0.9rem;
    color: #ffffff;
    font-size: 0.95rem;
    font-weight: 600;
    text-decoration: underline;
    text-underline-offset: 4px;
    text-decoration-color: rgba(255, 255, 255, 0.45);
    transition: color 0.2s ease, text-decoration-color 0.2s ease;
}

.eco-link-secundario:hover {
    color: #cbf264;
    text-decoration-color: #cbf264;
}

.eco-ico-diag {
    width: 15px;
    height: 15px;
    fill: none;
    stroke: currentColor;
    stroke-width: 2.5;
    stroke-linecap: round;
    stroke-linejoin: round;
}

/* Bloque inferior derecho (Garantía + Tarjeta de clientes/asociaciones) */
.eco-hero-der {
    display: flex;
    flex-direction: column;
    align-items: flex-end;
    gap: 0.85rem;
    align-self: flex-end;
    margin-bottom: 1.5rem;
    flex: 0 0 auto;
}

.eco-garantia-badge {
    display: inline-flex;
    align-items: center;
    gap: 0.65rem;
    color: rgba(255, 255, 255, 0.88);
    text-align: right;
    font-size: 0.75rem;
}

.eco-garantia-badge strong {
    display: block;
    color: #ffffff;
    font-size: 0.82rem;
    font-weight: 700;
}

.eco-garantia-badge small {
    display: block;
    color: rgba(255, 255, 255, 0.72);
}

.eco-garantia-ico {
    width: 22px;
    height: 22px;
    flex: 0 0 22px;
    color: #cbf264;
}

.eco-card-flotante {
    display: flex;
    align-items: center;
    gap: 1.15rem;
    padding: 0.9rem 1.1rem;
    border-radius: 1.25rem;
    background: rgba(12, 34, 16, 0.75);
    border: 1px solid rgba(255, 255, 255, 0.16);
    backdrop-filter: blur(14px);
    box-shadow: 0 20px 40px -10px rgba(0, 0, 0, 0.45);
}

.eco-card-flotante-info {
    display: flex;
    flex-direction: column;
    gap: 0.2rem;
    text-align: left;
}

.eco-card-flotante-info strong {
    font-size: 1.65rem;
    font-weight: 800;
    line-height: 1;
    color: #ffffff;
    letter-spacing: -0.02em;
}

.eco-card-flotante-info span {
    font-size: 0.76rem;
    color: rgba(255, 255, 255, 0.78);
    font-weight: 600;
}

.eco-card-avatares {
    display: flex;
    align-items: center;
    margin-top: 0.35rem;
}

.eco-card-avatares img {
    width: 30px;
    height: 30px;
    border-radius: 50%;
    border: 2px solid #0c2210;
    margin-left: -8px;
    object-fit: cover;
}

.eco-card-avatares img:first-child {
    margin-left: 0;
}

.eco-card-miniatura {
    position: relative;
    width: 135px;
    height: 82px;
    border-radius: 0.85rem;
    overflow: hidden;
    padding: 0;
    border: 0;
    cursor: pointer;
    background: #000;
}

.eco-card-miniatura img {
    width: 100%;
    height: 100%;
    object-fit: cover;
    transition: transform 0.3s ease;
}

.eco-card-miniatura:hover img {
    transform: scale(1.08);
}

.eco-mini-play {
    position: absolute;
    inset: 0;
    margin: auto;
    width: 34px;
    height: 34px;
    border-radius: 50%;
    background: #cbf264;
    color: #0c2210;
    display: grid;
    place-items: center;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.35);
}

.eco-mini-play svg {
    width: 15px;
    height: 15px;
    margin-left: 2px;
}

@media (max-width: 991px) {
    .eco-hero {
        height: auto;
        min-height: 100vh;
        min-height: 100svh;
        padding-top: 6.5rem;
        padding-bottom: 3rem;
    }

    .eco-hero-inner {
        flex-direction: column;
        align-items: flex-start;
        gap: 2rem;
    }

    .eco-hero-der {
        align-self: flex-start;
        align-items: flex-start;
    }

    .eco-garantia-badge {
        text-align: left;
    }
}

`;

const updatedCss = css.substring(0, startIndex) + newHeroCss + css.substring(endIndex);
fs.writeFileSync(cssPath, updatedCss, 'utf8');
console.log('Home.razor.css hero section updated successfully');

