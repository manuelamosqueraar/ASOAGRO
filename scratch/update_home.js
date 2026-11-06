const fs = require('fs');
let c = fs.readFileSync('Components/Pages/Home.razor', 'utf8');

// Update Google Fonts import
const oldFont = '<link href="https://fonts.googleapis.com/css2?family=Playfair+Display:ital,wght@1,600;1,700&family=Plus+Jakarta+Sans:wght@500;600;700;800&display=swap" rel="stylesheet" />';
const newFont = '<link href="https://fonts.googleapis.com/css2?family=Instrument+Serif:ital@1&family=Playfair+Display:ital,wght@1,400;1,600;1,700&family=Plus+Jakarta+Sans:wght@500;600;700;800&display=swap" rel="stylesheet" />';
if (c.includes(oldFont)) {
    c = c.replace(oldFont, newFont);
}

// Remove parallax/desvanece from hero inner
c = c.replace('<div class="ag-capa" data-parallax="0.35">', '<div class="ag-capa">');
c = c.replace('<div class="eco-contenedor eco-hero-inner" data-parallax="-0.06" data-desvanece>', '<div class="eco-contenedor eco-hero-inner">');

fs.writeFileSync('Components/Pages/Home.razor', c, 'utf8');
console.log('Home.razor updated');

