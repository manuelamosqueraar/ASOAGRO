const fs = require('fs');
let c = fs.readFileSync('wwwroot/js/asoagro-home.js', 'utf8');
c = c.replaceAll("seccion.classList.contains('ag-hero')", "seccion.classList.contains('ag-hero') || seccion.classList.contains('eco-hero')");
fs.writeFileSync('wwwroot/js/asoagro-home.js', c, 'utf8');
console.log('Updated asoagro-home.js successfully');

