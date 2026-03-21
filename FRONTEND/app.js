const express = require('express');
const path = require('path');
const expressLayouts = require('express-ejs-layouts');
const app = express();

// EJS view engine ayarları
app.set('view engine', 'ejs');
app.set('views', path.join(__dirname, 'views'));
app.set('layout', 'layouts/main');
app.use(expressLayouts);

// Statik dosyalar için public klasörü
app.use(express.static(path.join(__dirname, 'public')));
app.use('/swiper', express.static(path.join(__dirname, 'node_modules/swiper')));

// Ana sayfa route'u
app.get('/', (req, res) => {
    res.render('pages/home', {
        title: 'Buket.com.tr',
        description: 'Hoşgeldiniz',
        style: '',
        script: ''
    });
});

// Hakkımızda sayfası
app.get('/hakkimizda', (req, res) => {
    res.render('pages/about', {
        title: 'Hakkımızda',
        description: 'Biz Kimiz?',
        style: '',
        script: ''
    });
});

// Hizmetler sayfası
app.get('/hizmetler', (req, res) => {
    res.render('pages/services', {
        title: 'Hizmetler',
        description: 'Hizmetler',
        style: '',
        script: ''
    });
});


// İletişim sayfası
app.get('/iletisim', (req, res) => {
    res.render('pages/contact', {
        title: 'İletişim',
        description: 'Bize Ulaşın',
        style: '',
        script: ''
    });
});

// Sunucuyu başlat
const PORT = process.env.PORT || 3000;
app.listen(PORT, () => {
    console.log(`Sunucu http://localhost:${PORT} adresinde çalışıyor`);
}); 