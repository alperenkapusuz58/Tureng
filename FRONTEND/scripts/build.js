/**
 * EJS şablonlarını FRONTEND/views altından okuyup FRONTEND/dist içine statik HTML üretir.
 * public/ klasörünü dist/ içine kopyalar.
 */

const fs = require('fs');
const path = require('path');
const ejs = require('ejs');

const root = path.join(__dirname, '..');
const viewsDir = path.join(root, 'views');
const publicDir = path.join(root, 'public');
const distDir = path.join(root, 'dist');
const dummy = require('./dummy-data');
const { partOfSpeechEnglish } = require('./pos-labels');

function ensureDir(p) {
  fs.mkdirSync(p, { recursive: true });
}

function copyPublic() {
  if (!fs.existsSync(publicDir)) return;
  const walk = (src, dest) => {
    ensureDir(dest);
    for (const name of fs.readdirSync(src)) {
      const s = path.join(src, name);
      const d = path.join(dest, name);
      const st = fs.statSync(s);
      if (st.isDirectory()) walk(s, d);
      else fs.copyFileSync(s, d);
    }
  };
  walk(publicDir, distDir);
}

function renderTo(name, outName, data) {
  const tpl = path.join(viewsDir, name);
  const html = ejs.render(fs.readFileSync(tpl, 'utf8'), data, {
    filename: tpl,
    root: viewsDir,
    views: [viewsDir],
  });
  const outPath = path.join(distDir, outName);
  ensureDir(path.dirname(outPath));
  fs.writeFileSync(outPath, html, 'utf8');
}

function main() {
  ensureDir(distDir);
  copyPublic();

  renderTo('index.ejs', 'index.html', dummy.home());
  renderTo('en-tr-detail.ejs', 'en-tr-detail.html', {
    ...dummy.enTrDetail(),
    partOfSpeechEnglish,
  });
  renderTo('tr-en-detail.ejs', 'tr-en-detail.html', dummy.trEnDetail());
  renderTo('tr-en-results.ejs', 'tr-en-results.html', {
    ...dummy.trEnResults(),
    partOfSpeechEnglish,
  });
  renderTo('not-found.ejs', 'not-found.html', dummy.notFound());
  renderTo('not-found.ejs', 'not-found-tr.html', dummy.notFoundTr());
  renderTo('404.ejs', '404.html', dummy.page404());

  console.log('OK: çıktı ->', distDir);
}

main();
