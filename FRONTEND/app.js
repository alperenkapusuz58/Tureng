const express = require('express');
const path = require('path');
const expressLayouts = require('express-ejs-layouts');
const app = express();

app.set('view engine', 'ejs');
app.set('views', path.join(__dirname, 'views'));
app.set('layout', 'layouts/main');
app.use(expressLayouts);

app.use(express.static(path.join(__dirname, 'public')));
app.use('/swiper', express.static(path.join(__dirname, 'node_modules/swiper')));

/** Örnek veri — gerçek API bağlanınca değiştirilecek */
const MOCK_SEARCH = [
    { title: 'Politikalar ve Raporlar', href: '#' },
    { title: 'Kurumsal Yönetim', href: '#' },
    { title: 'Bizim Hikayemiz', href: '#' },
    { title: 'Yapı Ürünleri', href: '#' },
    { title: 'İnovasyon ve Girişimcilik', href: '#' },
    { title: 'Yatırımlarımız', href: '#' }
];

app.get('/api/search', (req, res) => {
    const q = String(req.query.q || '').trim();
    const dir = String(req.query.dir || '').trim();

    if (!q) {
        return res.json({ query: '', dir: dir || null, items: [] });
    }

    const lower = q.toLowerCase();
    const items = MOCK_SEARCH.filter(function (row) {
        return row.title.toLowerCase().includes(lower);
    }).map(function (row) {
        return { title: row.title, href: row.href };
    });

    res.json({ query: req.query.q, dir: dir || null, items: items });
});

app.get('/', (req, res) => {
    res.render('pages/home', {
        title: 'Ana Sayfa',
        description: 'İzi İletişim - Stratejik İletişim Çözümleri',
        style: '',
        script: '<script src="/js/search.js" defer></script>'
    });
});

app.use((req, res, next) => {
    res.status(404).render('pages/404', {
        title: '404 - Sayfa Bulunamadı',
        description: 'Aradığınız sayfa bulunamadı',
        style: '',
        script: ''
    });
});

const PORT = process.env.PORT || 3000;
app.listen(PORT, () => {
    console.log(`Sunucu http://localhost:${PORT} adresinde çalışıyor`);
});