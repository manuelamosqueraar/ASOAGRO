const fs = require('fs');

const razorPath = 'Components/Pages/Home.razor';
let razor = fs.readFileSync(razorPath, 'utf8');

const startMarker = '<section id="gobernanza" class="eco-servicios">';
const endMarker = '<!-- ===== 4. ARTICULACIÓN INTERASOCIATIVA';

const startIndex = razor.indexOf(startMarker);
const endIndex = razor.indexOf(endMarker);

if (startIndex === -1 || endIndex === -1) {
    console.error('Markers not found in Home.razor');
    process.exit(1);
}

const newSection = `<section id="gobernanza" class="eco-servicios">
        <div class="eco-contenedor">
            <div class="eco-servicios-cab">
                <div class="eco-servicios-titulos">
                    <span class="eco-tag-claro">Nuestros Servicios</span>
                    <h2 class="eco-h2-oscuro">Control Oportuno de Obligaciones y Estatutos</h2>
                </div>

                <div class="eco-carrusel-botones" aria-label="Controles del carrusel">
                    <button type="button"
                            class="eco-nav-circulo"
                            aria-label="Pilar anterior"
                            onclick="const c = document.getElementById('carruselGobernanza'); if(c) c.scrollBy({ left: -380, behavior: 'smooth' });">
                        <svg viewBox="0 0 24 24"><use href="#gob-izq" /></svg>
                    </button>
                    <button type="button"
                            class="eco-nav-circulo"
                            aria-label="Pilar siguiente"
                            onclick="const c = document.getElementById('carruselGobernanza'); if(c) c.scrollBy({ left: 380, behavior: 'smooth' });">
                        <svg viewBox="0 0 24 24"><use href="#gob-der" /></svg>
                    </button>
                </div>
            </div>

            <div id="carruselGobernanza" class="eco-tarjetas-carrusel">
                <!-- Tarjeta 1 -->
                <article class="eco-card-verde">
                    <div class="eco-card-foto">
                        <img src="https://images.unsplash.com/photo-1592982537447-7440770cbfc9?auto=format&fit=crop&w=900&q=80" alt="Asambleas al Día" />
                    </div>
                    <div class="eco-card-cuerpo">
                        <span class="eco-card-pastilla">Componente Estatutario</span>
                        <h3 class="eco-card-titulo">Asambleas al Día</h3>
                        <p class="eco-card-texto">
                            Seguimiento automatizado y oportuno de las fechas límite para la renovación de juntas directivas, actas estatutarias y verificación de quórum legal.
                        </p>
                    </div>
                </article>

                <!-- Tarjeta 2 -->
                <article class="eco-card-verde">
                    <div class="eco-card-foto">
                        <img src="https://images.unsplash.com/photo-1574943320219-553eb213f72d?auto=format&fit=crop&w=900&q=80" alt="Obligaciones Tributarias" />
                    </div>
                    <div class="eco-card-cuerpo">
                        <span class="eco-card-pastilla">Componente Fiscal</span>
                        <h3 class="eco-card-titulo">Obligaciones Tributarias</h3>
                        <p class="eco-card-texto">
                            Control preventivo de requisitos fiscales, declaraciones y presentación de informes para evitar sanciones y pérdidas en convocatorias.
                        </p>
                    </div>
                </article>

                <!-- Tarjeta 3 -->
                <article class="eco-card-verde">
                    <div class="eco-card-foto">
                        <img src="https://images.unsplash.com/photo-1500595046743-cd271d694d30?auto=format&fit=crop&w=900&q=80" alt="Gobernanza Interna" />
                    </div>
                    <div class="eco-card-cuerpo">
                        <span class="eco-card-pastilla">Red de Asociaciones</span>
                        <h3 class="eco-card-titulo">Gobernanza Interna</h3>
                        <p class="eco-card-texto">
                            Articulación directa y fluida entre la directiva central de la FAAUA y las juntas locales que administran las 37 asociaciones de base de la región.
                        </p>
                    </div>
                </article>

                <!-- Tarjeta 4 -->
                <article class="eco-card-verde">
                    <div class="eco-card-foto">
                        <img src="https://images.unsplash.com/photo-1500937386664-56d1dfef3854?auto=format&fit=crop&w=900&q=80" alt="Gestión de Activos y Fincas" />
                    </div>
                    <div class="eco-card-cuerpo">
                        <span class="eco-card-pastilla">Inventario Regional</span>
                        <h3 class="eco-card-titulo">Gestión de Activos y Fincas</h3>
                        <p class="eco-card-texto">
                            Registro detallado de predios, maquinaria, infraestructura productiva e insumos adjudicados a cada asociación para garantizar transparencia y control de recursos.
                        </p>
                    </div>
                </article>

                <!-- Tarjeta 5 -->
                <article class="eco-card-verde">
                    <div class="eco-card-foto">
                        <img src="https://images.unsplash.com/photo-1586771107445-d3ca888129ff?auto=format&fit=crop&w=900&q=80" alt="Auditoría y Reportes" />
                    </div>
                    <div class="eco-card-cuerpo">
                        <span class="eco-card-pastilla">Transparencia</span>
                        <h3 class="eco-card-titulo">Auditoría y Reportes</h3>
                        <p class="eco-card-texto">
                            Generación automática de informes de gestión transparentes y tableros de control listos para ser presentados ante entidades de control y cooperantes.
                        </p>
                    </div>
                </article>
            </div>
        </div>
    </section>

    `;

razor = razor.substring(0, startIndex) + newSection + razor.substring(endIndex);
fs.writeFileSync(razorPath, razor, 'utf8');
console.log('Home.razor updated successfully');

