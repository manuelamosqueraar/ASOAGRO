const fs = require('fs');

const cssPath = 'Components/Pages/Home.razor.css';
let css = fs.readFileSync(cssPath, 'utf8');

const targetOld = `.eco-traza {
    position: relative;
    display: flex;
    align-items: center;
    width: 100%;
    min-height: 68vh;
    padding: clamp(4.5rem, 8vh, 6rem) 0;
    background-color: var(--eco-bosque-osc);
    overflow: hidden;
}

.eco-traza .ag-capa {
    position: absolute;
    inset: 0;
    z-index: 0;
}`;

const targetNew = `.eco-traza {
    position: relative;
    display: flex;
    align-items: center;
    width: 100%;
    min-height: 76vh;
    padding: clamp(5.5rem, 10vh, 7.5rem) 0;
    background-color: var(--eco-bosque-osc);
    overflow: hidden;
}

/* Margen superior e inferior extendido para evitar cualquier borde o corte al hacer scroll con parallax */
.eco-traza .ag-capa {
    position: absolute;
    top: -160px;
    bottom: -160px;
    left: 0;
    right: 0;
    height: calc(100% + 320px);
    z-index: 0;
}`;

if (!css.includes(targetOld)) {
    console.error('Target old not found in css');
    process.exit(1);
}

css = css.replace(targetOld, targetNew);

// Add bold title rule for eco-traza
const cajaOld = `.eco-traza-caja {
    max-width: 720px;
    color: #ffffff;
}`;

const cajaNew = `.eco-traza-caja {
    max-width: 760px;
    color: #ffffff;
}

.eco-traza .eco-h2-claro {
    margin: 0 0 1.25rem;
    font-family: 'Plus Jakarta Sans', 'Inter', system-ui, sans-serif !important;
    font-size: clamp(2.3rem, 3.8vw, 3.3rem);
    font-weight: 800 !important;
    line-height: 1.15;
    letter-spacing: -0.03em;
    color: #ffffff;
    text-shadow: 0 4px 22px rgba(0, 0, 0, 0.45);
}

.eco-traza .eco-h2-claro strong {
    font-weight: 800 !important;
}`;

css = css.replace(cajaOld, cajaNew);

fs.writeFileSync(cssPath, css, 'utf8');
console.log('Home.razor.css updated for eco-traza successfully');

