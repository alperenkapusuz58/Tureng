/**
 * Kelime türü etiketlerini İngilizce sözlük diline çevirir (slug: noun, verb, …).
 * API Türkçe veya İngilizce döndüğünde arayüzde her zaman İngilizce gösterim için kullanılır.
 */

const TR_TO_EN = {
  isim: 'noun',
  fiil: 'verb',
  sıfat: 'adjective',
  zarf: 'adverb',
  zamir: 'pronoun',
  edat: 'preposition',
  bağlaç: 'conjunction',
  ünlem: 'interjection',
};

const EN_ALIASES = {
  n: 'noun',
  v: 'verb',
  adj: 'adjective',
  adv: 'adverb',
  pron: 'pronoun',
  prep: 'preposition',
  conj: 'conjunction',
  interj: 'interjection',
};

function normTr(s) {
  return String(s)
    .trim()
    .toLocaleLowerCase('tr-TR');
}

/**
 * @param {string} raw — CMS veya API'den gelen kelime türü (ör. "İsim", "noun", "adj")
 * @returns {string} İngilizce slug (ör. "noun"); bilinmiyorsa küçük harfe indirgenmiş ham değer
 */
function partOfSpeechEnglish(raw) {
  if (raw == null) return '';
  const trimmed = String(raw).trim();
  if (!trimmed) return '';

  const trKey = normTr(trimmed);
  if (TR_TO_EN[trKey]) return TR_TO_EN[trKey];

  const lower = trimmed.toLowerCase();
  if (TR_TO_EN[lower]) return TR_TO_EN[lower];
  if (EN_ALIASES[lower]) return EN_ALIASES[lower];

  const noSpace = lower.replace(/\s+/g, '');
  if (EN_ALIASES[noSpace]) return EN_ALIASES[noSpace];

  return lower;
}

module.exports = { partOfSpeechEnglish, TR_TO_EN, EN_ALIASES };
