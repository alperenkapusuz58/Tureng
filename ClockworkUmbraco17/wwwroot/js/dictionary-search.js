/**
 * Ana sayfa sözlük formu — GET /api/dictionary/search (SearchController)
 */
(function () {
    var form = document.getElementById('dict-search-form');
    if (!form) return;

    var apiUrl = form.getAttribute('data-dictionary-search-url') || '/api/dictionary/search';
    var rawNoResults = form.getAttribute('data-dictionary-no-results-url');
    var noResultsBaseUrl = rawNoResults && rawNoResults.trim();
    var input = document.getElementById('query');
    var list = document.getElementById('suggestions');
    var tpl = document.getElementById('dictionary-suggestion-item-template');
    var directionInput = document.getElementById('direction');
    var clearBtn = document.getElementById('dict-search-clear');

    if (!input || !list || !tpl) return;

    function updateClearBtn() {
        if (!clearBtn) return;
        var hasText = (input.value || '').length > 0;
        if (hasText) {
            clearBtn.removeAttribute('hidden');
        } else {
            clearBtn.setAttribute('hidden', '');
        }
    }

    if (clearBtn) {
        clearBtn.addEventListener('click', function () {
            input.value = '';
            updateClearBtn();
            input.focus();
            hideSuggestions();
        });
    }

    updateClearBtn();

    var debounceMs = 280;
    var minChars = 1;
    var timer = null;
    var lastController = null;

    function hideSuggestions() {
        list.hidden = true;
        list.innerHTML = '';
    }

    /** API bazen url dolu iken lemma/word göndermeyebilir — gösterim için yedekler */
    function pickDisplayLemma(item, href) {
        var lemma =
            item.lemma ||
            item.Lemma ||
            item.word ||
            item.Word ||
            item.name ||
            item.Name ||
            '';
        if (lemma) return lemma;

        if (href && href !== '#') {
            try {
                var path = href.replace(/\/+$/, '').split('/').filter(Boolean);
                var last = path[path.length - 1];
                if (last) {
                    return decodeURIComponent(last).replace(/\+/g, ' ').replace(/-/g, ' ');
                }
            } catch (e1) {
                /* ignore */
            }
        }

        var translation = item.translation != null ? item.translation : item.Translation;
        if (translation) return translation;

        return '';
    }

    function renderItems(results) {
        list.innerHTML = '';
        for (var i = 0; i < results.length; i++) {
            var item = results[i];
            var href = item.url || item.Url || '';
            if (!href || href === '#') continue;

            var lemma = pickDisplayLemma(item, href);
            if (!lemma) lemma = href;

            var translation = item.translation != null ? item.translation : item.Translation;
            var node = tpl.content.cloneNode(true);
            var li = node.querySelector('li');
            var a = node.querySelector('a');
            if (!a || !li) continue;
            a.href = href;
            a.setAttribute('aria-label', lemma + (translation ? ' — ' + translation : ''));

            var main = document.createElement('span');
            main.className = 'suggestion-main';
            main.textContent = lemma;
            a.appendChild(main);

            if (translation && translation !== lemma) {
                var meta = document.createElement('span');
                meta.className = 'suggestion-meta';
                meta.textContent = translation;
                a.appendChild(meta);
            }

            list.appendChild(node);
        }
        list.hidden = results.length === 0;
    }

    function fetchSuggestions(q) {
        if (lastController) {
            lastController.abort();
        }
        lastController = typeof AbortController !== 'undefined' ? new AbortController() : null;
        var direction = directionInput ? (directionInput.value || 'en-tr') : 'en-tr';
        var url = apiUrl + (apiUrl.indexOf('?') >= 0 ? '&' : '?') + 'q=' + encodeURIComponent(q) + '&direction=' + encodeURIComponent(direction);

        fetch(url, {
            method: 'GET',
            headers: { Accept: 'application/json' },
            signal: lastController ? lastController.signal : undefined,
        })
            .then(function (res) {
                if (!res.ok) throw new Error('search failed');
                return res.json();
            })
            .then(function (data) {
                var results = (data && (data.results || data.Results)) || [];
                renderItems(results);
            })
            .catch(function (err) {
                if (err && err.name === 'AbortError') return;
                hideSuggestions();
            });
    }

    function scheduleFetch() {
        if (timer) clearTimeout(timer);
        var q = (input.value || '').trim();
        if (q.length < minChars) {
            hideSuggestions();
            return;
        }
        timer = setTimeout(function () {
            fetchSuggestions(q);
        }, debounceMs);
    }

    input.addEventListener('input', function () {
        updateClearBtn();
        scheduleFetch();
    });
    input.addEventListener('focus', function () {
        if ((input.value || '').trim().length >= minChars && list.children.length) {
            list.hidden = false;
        } else {
            scheduleFetch();
        }
    });

    document.addEventListener('click', function (e) {
        if (!form.contains(e.target)) {
            hideSuggestions();
        }
    });

    list.addEventListener('mousedown', function (e) {
        e.preventDefault();
    });

    form.addEventListener('submit', function (e) {
        e.preventDefault();
        var q = (input.value || '').trim();
        if (!q) return;

        var direction = directionInput ? (directionInput.value || 'en-tr') : 'en-tr';
        var url = apiUrl + (apiUrl.indexOf('?') >= 0 ? '&' : '?') + 'q=' + encodeURIComponent(q) + '&direction=' + encodeURIComponent(direction);
        fetch(url, {
            method: 'GET',
            headers: { Accept: 'application/json' },
        })
            .then(function (res) {
                if (!res.ok) throw new Error('search failed');
                return res.json();
            })
            .then(function (data) {
                var results = (data && (data.results || data.Results)) || [];
                var first = results[0];
                var u = first && (first.url || first.Url);
                if (u) {
                    window.location.href = u;
                    return;
                }
                try {
                    var target = new URL(noResultsBaseUrl, window.location.href);
                    target.searchParams.set('q', q);
                    target.searchParams.set('direction', direction);
                    window.location.href = target.pathname + target.search;
                } catch (e2) {
                    var sep = noResultsBaseUrl.indexOf('?') >= 0 ? '&' : '?';
                    window.location.href =
                        noResultsBaseUrl +
                        sep +
                        'q=' +
                        encodeURIComponent(q) +
                        '&direction=' +
                        encodeURIComponent(direction);
                }
            })
            .catch(function () {
                hideSuggestions();
            });
    });

    var tabBtns = form.querySelectorAll('.tab-btn');
    tabBtns.forEach(function (btn) {
        btn.addEventListener('click', function () {
            var dir = btn.getAttribute('data-direction') || 'en-tr';
            if (directionInput) directionInput.value = dir;
            tabBtns.forEach(function (b) {
                b.classList.toggle('active', b === btn);
            });
            if (dir === 'tr-en') {
                input.placeholder = 'Search Turkish - English';
            } else {
                input.placeholder = 'Search English - Turkish';
            }
        });
    });
})();
