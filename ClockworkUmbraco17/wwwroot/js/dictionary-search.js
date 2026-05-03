/**
 * Ana sayfa sözlük formu — GET /api/dictionary/search (SearchController)
 */
(function () {
    var form = document.getElementById('dict-search-form');
    if (!form) return;

    var apiUrl = form.getAttribute('data-dictionary-search-url') || '/api/dictionary/search';
    var input = document.getElementById('query');
    var list = document.getElementById('suggestions');
    var tpl = document.getElementById('dictionary-suggestion-item-template');
    var directionInput = document.getElementById('direction');

    if (!input || !list || !tpl) return;

    var debounceMs = 280;
    var minChars = 1;
    var timer = null;
    var lastController = null;

    function hideSuggestions() {
        list.hidden = true;
        list.innerHTML = '';
    }

    function renderItems(results) {
        list.innerHTML = '';
        for (var i = 0; i < results.length; i++) {
            var item = results[i];
            var lemma = item.lemma || item.Lemma || '';
            var href = item.url || item.Url || '';
            if (!href || href === '#') continue;
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

    input.addEventListener('input', scheduleFetch);
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
                debugger;
                return res.json();
            })
            .then(function (data) {
                var results = (data && (data.results || data.Results)) || [];
                var first = results[0];
                var u = first && (first.url || first.Url);
                console.log("data", data)
                if (u) {
                    window.location.href = u;
                    return;
                }
                hideSuggestions();
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
                input.placeholder = 'Türkçe ara';
            } else {
                input.placeholder = 'Search English';
            }
        });
    });
})();
