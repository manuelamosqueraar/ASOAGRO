// Vista previa, descarga y copia para el módulo de actas.
// Los bytes llegan desde .NET con DotNetStreamReference y se leen con arrayBuffer().
let overflowDocumento = null;

export function bloquearScroll(bloquear) {
    if (bloquear) {
        if (overflowDocumento === null) {
            overflowDocumento = {
                body: document.body.style.overflow,
                html: document.documentElement.style.overflow
            };
        }

        document.body.style.overflow = "hidden";
        document.documentElement.style.overflow = "hidden";
        return;
    }

    if (overflowDocumento !== null) {
        document.body.style.overflow = overflowDocumento.body;
        document.documentElement.style.overflow = overflowDocumento.html;
        overflowDocumento = null;
    }
}

// Crea una URL temporal para mostrar un PDF o una imagen dentro de la página.
export async function crearVista(referencia, tipoMime) {
    const bytes = await referencia.arrayBuffer();
    return URL.createObjectURL(new Blob([bytes], { type: tipoMime }));
}

// Libera la memoria de una vista previa que ya no se usa.
export function liberarVista(url) {
    if (url) {
        URL.revokeObjectURL(url);
    }
}

// Descarga el archivo con su nombre original.
export async function descargar(nombre, referencia, tipoMime) {
    const bytes = await referencia.arrayBuffer();
    const url = URL.createObjectURL(new Blob([bytes], { type: tipoMime }));

    const enlace = document.createElement("a");
    enlace.href = url;
    enlace.download = nombre;
    document.body.appendChild(enlace);
    enlace.click();
    enlace.remove();

    setTimeout(() => URL.revokeObjectURL(url), 4000);
}

// Descarga una tabla CSV con BOM para conservar caracteres acentuados en hojas de cálculo.
export function descargarCsv(nombre, contenido) {
    const blob = new Blob(["\uFEFF", contenido], { type: "text/csv;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const enlace = document.createElement("a");
    enlace.href = url;
    enlace.download = nombre;
    document.body.appendChild(enlace);
    enlace.click();
    enlace.remove();
    setTimeout(() => URL.revokeObjectURL(url), 4000);
}

// Copia un texto, por ejemplo la huella SHA-256, al portapapeles.
export async function copiarTexto(texto) {
    await navigator.clipboard.writeText(texto);
}