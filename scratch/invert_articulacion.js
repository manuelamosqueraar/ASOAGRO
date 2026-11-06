const fs = require('fs');

// 1. Update Home.razor
const razorPath = 'Components/Pages/Home.razor';
let razor = fs.readFileSync(razorPath, 'utf8');

const startMarker = '<!-- ===== 4. ARTICULACIÓN INTERASOCIATIVA';
const endMarker = '<!-- ===== 5. TRAZABILIDAD COMERCIAL';

const startIndex = razor.indexOf(startMarker);
const endIndex = razor.indexOf(endMarker);

if (startIndex === -1 || endIndex === -1) {
    console.error('Markers not found in Home.razor');
    process.exit(1);
}

const newSection = `<!-- ===== 4. ARTICULACIÓN INTERASOCIATIVA (INVERTIDO: TEXTO IZQUIERDA, FOTOS DERECHA, BOTÓN CENTRADO) ===== -->
    <section class="art eco-articulacion">
        <div class="eco-contenedor">
            <div class="art-grid">

                <!-- Columna Izquierda: Contenido, tabla de dos columnas y botón amarillo centrado -->
                <div class="art-texto-col">
                    <h2 class="art-titulo">
                        Gestión Unificada de las <br />
                        <span class="art-titulo-acento">Asociaciones de Base</span>
                    </h2>

                    <p class="art-descripcion">
                        Resolvemos la desarticulación administrativa conectando los registros de productores, fincas y juntas directivas en un único directorio federado y confiable para toda la subregión.
                    </p>

                    <!-- Tabla / Caja de características de 2 columnas estilo referencia -->
                    <div class="art-tabla-caracteristicas">
                        <div class="art-caracteristica-item">
                            <div class="art-caract-icono">
                                <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="5" fill="currentColor" /><path d="M12 1v2M12 21v2M4.22 4.22l1.42 1.42M18.36 18.36l1.42 1.42M1 12h2M21 12h2M4.22 19.78l1.42-1.42M18.36 5.64l1.42-1.42" stroke="currentColor" stroke-width="2" stroke-linecap="round" fill="none" /></svg>
                            </div>
                            <div class="art-caract-info">
                                <strong>Directorio Centralizado</strong>
                                <p>Unificamos productores, predios y juntas locales en un registro verificado.</p>
                            </div>
                        </div>

                        <div class="art-caracteristica-item">
                            <div class="art-caract-icono">
                                <svg viewBox="0 0 24 24"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" fill="none" /><path d="M9 12l2 2 4-4" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" fill="none" /></svg>
                            </div>
                            <div class="art-caract-info">
                                <strong>Gobernanza y Control</strong>
                                <p>Seguimiento continuo a estatutos para garantizar elegibilidad en convocatorias.</p>
                            </div>
                        </div>
                    </div>

                    <!-- Botón CTA amarillo centrado -->
                    <div class="art-acciones">
                        <button type="button" class="art-btn-amarillo" @onclick='() => NavegarA("historia/origenes")'>
                            <span>Conocer Orígenes</span>
                            <svg viewBox="0 0 24 24" class="art-btn-flecha"><path d="M7 17L17 7M9 7h8v8" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" /></svg>
                        </button>
                    </div>
                </div>

                <!-- Columna Derecha: Composición fotográfica con badge flotante amarillo -->
                <div class="art-fotos-col">
                    <div class="art-marco-decorativo" aria-hidden="true"></div>

                    <!-- Foto principal -->
                    <div class="art-foto-principal">
                        <img src="https://images.unsplash.com/photo-1500937386664-56d1dfef3854?auto=format&fit=crop&w=900&q=80" alt="Agricultores en campo de siembra" />
                    </div>

                    <!-- Foto secundaria superpuesta -->
                    <div class="art-foto-secundaria">
                        <img src="https://images.unsplash.com/photo-1530836369250-ef72a3f5cda8?auto=format&fit=crop&w=600&q=80" alt="Plántulas y brotes en macetas" />
                    </div>

                    <!-- Badge flotante amarillo -->
                    <div class="art-badge-experiencia">
                        <strong class="art-badge-cifra">37+</strong>
                        <span class="art-badge-rotulo">Asociaciones de Base Federadas</span>
                    </div>
                </div>

            </div>
        </div>
    </section>

    `;

razor = razor.substring(0, startIndex) + newSection + razor.substring(endIndex);
fs.writeFileSync(razorPath, razor, 'utf8');
console.log('Home.razor updated');

// 2. Update Home.razor.css
const cssPath = 'Components/Pages/Home.razor.css';
let css = fs.readFileSync(cssPath, 'utf8');

const cssStartMarker = '.eco-articulacion {';
const cssEndMarker = '.eco-traza {';

const cssStartIndex = css.indexOf(cssStartMarker);
const cssEndIndex = css.indexOf(cssEndMarker);

if (cssStartIndex === -1 || cssEndIndex === -1) {
    console.error('Markers not found in Home.razor.css');
    process.exit(1);
}

const newArtCss = `.eco-articulacion {
    position: relative;
    width: 100%;
    padding: clamp(3.5rem, 7vh, 5.5rem) 0;
    background-color: #ffffff;
    color: #112414;
    overflow: hidden;
}

/* Grilla invertida: Columna de texto a la izquierda, Fotos a la derecha */
.art-grid {
    display: grid;
    grid-template-columns: minmax(0, 1.15fr) minmax(0, 1.05fr);
    gap: clamp(2.5rem, 5vw, 4.5rem);
    align-items: center;
}

/* Columna de Texto (Izquierda) */
.art-texto-col {
    display: flex;
    flex-direction: column;
    align-items: flex-start;
}

.art-titulo {
    margin: 0 0 1.15rem;
    font-family: 'Plus Jakarta Sans', 'Inter', system-ui, sans-serif !important;
    font-size: clamp(2.1rem, 3.4vw, 2.85rem);
    font-weight: 800 !important;
    line-height: 1.16;
    letter-spacing: -0.03em;
    color: #112414;
}

.art-titulo-acento {
    color: #17601f;
}

.art-descripcion {
    margin: 0 0 1.85rem;
    font-size: 0.98rem;
    line-height: 1.68;
    color: #435446;
    max-width: 580px;
}

/* Tabla de características con acento superior amarillo */
.art-tabla-caracteristicas {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 1.4rem;
    width: 100%;
    padding: 1.45rem 1.65rem;
    border-radius: 20px;
    background: #fcfbf5;
    border: 1px solid rgba(246, 201, 39, 0.45);
    border-top: 3.5px solid #f6c927;
    margin-bottom: 2rem;
    box-shadow: 0 12px 28px -14px rgba(0, 0, 0, 0.07);
}

.art-caracteristica-item {
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    gap: 0.75rem;
}

.art-caract-icono {
    width: 44px;
    height: 44px;
    border-radius: 50%;
    background: #f6c927;
    color: #112414;
    display: grid;
    place-items: center;
    box-shadow: 0 6px 14px -5px rgba(246, 201, 39, 0.55);
}

.art-caract-icono svg {
    width: 22px;
    height: 22px;
}

.art-caract-info strong {
    display: block;
    font-size: 1.05rem;
    font-weight: 800;
    color: #112414;
    margin-bottom: 0.35rem;
}

.art-caract-info p {
    margin: 0;
    font-size: 0.82rem;
    line-height: 1.5;
    color: #556858;
}

/* Botón CTA amarillo centrado */
.art-acciones {
    display: flex;
    justify-content: center;
    width: 100%;
}

.art-btn-amarillo {
    display: inline-flex;
    align-items: center;
    gap: 0.65rem;
    padding: 0.85rem 1.85rem;
    border-radius: 999px;
    background: #f6c927;
    color: #112414;
    font-family: inherit;
    font-size: 0.96rem;
    font-weight: 800;
    border: 0;
    cursor: pointer;
    box-shadow: 0 10px 24px -8px rgba(246, 201, 39, 0.6);
    transition: transform 0.25s ease, background-color 0.25s ease, box-shadow 0.25s ease;
}

.art-btn-amarillo:hover {
    transform: translateY(-2px);
    background: #ffd646;
    box-shadow: 0 14px 28px -6px rgba(246, 201, 39, 0.75);
}

.art-btn-flecha {
    width: 17px;
    height: 17px;
}

/* Columna Derecha: Composición Fotográfica */
.art-fotos-col {
    position: relative;
    width: 100%;
    max-width: 520px;
    margin: 0 auto;
    padding: 1.5rem 1rem 2rem 0;
}

.art-marco-decorativo {
    position: absolute;
    top: 25px;
    right: -10px;
    width: 72%;
    height: 82%;
    border: 1.5px solid rgba(246, 201, 39, 0.7);
    border-radius: 28px;
    z-index: 0;
    pointer-events: none;
}

.art-foto-principal {
    position: relative;
    z-index: 1;
    width: 76%;
    aspect-ratio: 4 / 4.8;
    max-height: 420px;
    border-radius: 24px;
    overflow: hidden;
    box-shadow: 0 20px 42px -16px rgba(0, 0, 0, 0.22);
}

.art-foto-principal img {
    width: 100%;
    height: 100%;
    object-fit: cover;
    display: block;
}

.art-foto-secundaria {
    position: absolute;
    top: 14%;
    right: 0;
    width: 56%;
    aspect-ratio: 4 / 4.6;
    max-height: 310px;
    border-radius: 22px;
    overflow: hidden;
    border: 5px solid #ffffff;
    box-shadow: 0 22px 46px -14px rgba(0, 0, 0, 0.26);
    z-index: 2;
}

.art-foto-secundaria img {
    width: 100%;
    height: 100%;
    object-fit: cover;
    display: block;
}

.art-badge-experiencia {
    position: absolute;
    bottom: 0;
    right: 18px;
    z-index: 3;
    display: flex;
    align-items: center;
    gap: 0.85rem;
    padding: 0.95rem 1.45rem;
    border-radius: 18px;
    background: #f6c927;
    color: #112414;
    box-shadow: 0 14px 28px -8px rgba(246, 201, 39, 0.6);
}

.art-badge-cifra {
    font-family: 'Plus Jakarta Sans', system-ui, sans-serif;
    font-size: 2.2rem;
    font-weight: 800;
    line-height: 1;
    letter-spacing: -0.02em;
}

.art-badge-rotulo {
    font-size: 0.84rem;
    font-weight: 700;
    line-height: 1.25;
    max-width: 130px;
}

@media (max-width: 991px) {
    .art-grid {
        grid-template-columns: 1fr;
        gap: 3rem;
    }

    .art-fotos-col {
        max-width: 440px;
    }

    .art-tabla-caracteristicas {
        grid-template-columns: 1fr;
        gap: 1.25rem;
    }
}

`;

css = css.substring(0, cssStartIndex) + newArtCss + css.substring(cssEndIndex);
fs.writeFileSync(cssPath, css, 'utf8');
console.log('Home.razor.css updated');

