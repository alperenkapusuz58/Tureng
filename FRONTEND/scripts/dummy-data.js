/**
 * Örnek içerik — Django view'larındaki yapıya yakın tutuldu.
 * Kendi projenizde bu nesneyi API/JSON ile değiştirebilirsiniz.
 */

const site = {
  site_name: 'Tureng Sözlük (önizleme)',
  brand_tagline: 'İngilizce-Türkçe sözlük — statik dummy şablon.',
  logo_url: '',
  footer_meta_text: 'Bu sayfalar FRONTEND/build çıktısıdır; üretimde backend bağlanır.',
  announcement_is_active: true,
  announcement_text: 'Örnek duyuru: Bu bir statik önizlemedir.',
  announcement_link_url: '',
};

const headwordEn = {
  lemma: 'example',
  pronunciation_text: 'ɪɡˈzɑːmpəl',
  pronunciation_audio_url: '',
};

const headwordTr = {
  lemma: 'örnek',
};

module.exports = {
  site,

  home() {
    return { site };
  },

  enTrDetail() {
    return {
      site,
      headword: headwordEn,
      senseGroups: [
        {
          pos_key: 'noun',
          irregular_forms: '',
          senses: [
            {
              grammar_code: '[C]',
              definition_html:
                'Something that is typical of its kind or illustrates a general rule.',
              translation: 'örnek, misal',
              examples: [
                {
                  sentence_source_html: 'This is a good <strong>example</strong> of cooperation.',
                  sentence_target_html: 'Bu, iş birliğinin iyi bir örneğidir.',
                  sentence_source_plain: 'This is a good example of cooperation.',
                },
              ],
            },
            {
              grammar_code: '[U]',
              definition_html: 'The act of illustrating or serving as a pattern.',
              translation: 'örnek teşkil etme',
              examples: [],
            },
          ],
        },
        {
          pos_key: 'verb',
          irregular_forms: '',
          senses: [
            {
              grammar_code: '[T]',
              definition_html: 'To show or illustrate by example.',
              translation: 'örneklemek',
              examples: [
                {
                  sentence_source_html: 'She <em>exampled</em> the rule with a short story.',
                  sentence_target_html: 'Kuralı kısa bir hikâyle örnekledi.',
                  sentence_source_plain: 'She exampled the rule with a short story.',
                },
              ],
            },
          ],
        },
      ],
      phrases: [
        {
          phrase_text: 'for example',
          definition_html: 'Used when giving an example.',
          translation: 'örneğin',
          example_source_html: 'Many fruits are rich in vitamin C; <strong>for example</strong>, oranges.',
          example_target_html: 'Birçok meyve C vitamini açısından zengindir; örneğin portakal.',
        },
      ],
    };
  },

  trEnDetail() {
    return {
      site,
      headword: headwordTr,
      links: [
        { en_lemma: 'example', slug: 'example' },
        { en_lemma: 'instance', slug: 'instance' },
        { en_lemma: 'sample', slug: 'sample' },
      ],
    };
  },

  trEnResults() {
    return {
      site,
      query: 'örnek',
      total: 3,
      resultGroups: [
        {
          title: 'İsim',
          rows: [
            {
              num: 1,
              pos_display: 'İsim',
              tr_link_text: 'örnek',
              en_lemma: 'example',
              en_slug: 'example',
            },
            {
              num: 2,
              pos_display: 'İsim',
              tr_link_text: 'numune',
              en_lemma: 'sample',
              en_slug: 'sample',
            },
          ],
        },
        {
          title: 'Fiil',
          rows: [
            {
              num: 1,
              pos_display: 'Fiil',
              tr_link_text: 'örnek göstermek',
              en_lemma: 'exemplify',
              en_slug: 'exemplify',
            },
          ],
        },
      ],
    };
  },

  notFound() {
    return {
      site,
      query: 'xyzdummy',
      direction: 'en-tr',
      suggestions: [
        { lemma: 'example', url: 'en-tr-detail.html' },
        { lemma: 'examine', url: 'en-tr-detail.html' },
      ],
      did_you_mean: [
        { lemma: 'example', term: '', url: 'en-tr-detail.html' },
      ],
      sample_words: [
        { lemma: 'water', url: 'en-tr-detail.html' },
        { lemma: 'light', url: 'en-tr-detail.html' },
        { lemma: 'book', url: 'en-tr-detail.html' },
      ],
    };
  },

  notFoundTr() {
    return {
      site,
      query: 'nosuchwordtr',
      direction: 'tr-en',
      suggestions: [
        { lemma: 'örnek', translation: 'example', url: 'en-tr-detail.html' },
      ],
      did_you_mean: [],
      sample_words: [
        { lemma: 'su', url: 'en-tr-detail.html' },
        { lemma: 'ışık', url: 'en-tr-detail.html' },
      ],
    };
  },

  page404() {
    return {};
  },
};
