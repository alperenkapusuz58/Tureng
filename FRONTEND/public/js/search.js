(function () {
    const section = document.querySelector('[data-search-section]');
    if (!section) return;

    const form = document.getElementById('home-search-form');
    const input = document.getElementById('search-input');
    const resultsBlock = document.getElementById('search-results-block');
    const resultsHeading = document.getElementById('search-results-heading');
    const resultsList = document.getElementById('search-results-list');
    const dirButtons = section.querySelectorAll('[data-search-dir]');

    if (!form || !input || !resultsBlock || !resultsHeading || !resultsList) return;

    let selectedDir = '';
    dirButtons.forEach(function (btn) {
        btn.addEventListener('click', function () {
            const dir = btn.getAttribute('data-search-dir') || '';
            selectedDir = selectedDir === dir ? '' : dir;
            dirButtons.forEach(function (b) {
                const on = b.getAttribute('data-search-dir') === selectedDir && selectedDir !== '';
                b.setAttribute('aria-pressed', on ? 'true' : 'false');
                b.classList.toggle('bg-[#F4F2EE]', on);
                b.classList.toggle('font-600', on);
            });
            if (input.value.trim()) runSearch();
        });
    });

    function setLoading(isLoading) {
        input.setAttribute('aria-busy', isLoading ? 'true' : 'false');
        if (isLoading) {
            resultsHeading.textContent = 'Aranıyor…';
            resultsBlock.hidden = false;
            resultsList.innerHTML = '';
        }
    }

    function renderResults(data) {
        const q = data.query || '';
        const items = Array.isArray(data.items) ? data.items : [];

        resultsBlock.hidden = false;

        if (!String(q).trim()) {
            resultsHeading.textContent = '';
            resultsList.innerHTML = '';
            resultsBlock.hidden = true;
            return;
        }

        if (items.length === 0) {
            resultsHeading.textContent = 'Sonuç bulunamadı';
            resultsList.innerHTML =
                '<p class="px-[12px] py-[16px] text-14 text-gray border-b border-[#EBEBEB]">' +
                '“' +
                escapeHtml(String(q)) +
                '” için eşleşen kayıt yok.</p>';
            return;
        }

        resultsHeading.textContent = 'Arama Sonuçları (' + items.length + ')';

        const linkClass =
            'flex items-center px-[12px] py-[12px] md:py-[16px] border-b border-[#EBEBEB]';
        const spanClass = 'text-14 md:text-16 font-normal leading-[1.5em] text-gray';

        resultsList.innerHTML = items
            .map(function (item) {
                const title = item.title != null ? String(item.title) : '';
                const href = item.href != null ? String(item.href) : '#';
                return (
                    '<a href="' +
                    escapeAttr(href) +
                    '" class="' +
                    linkClass +
                    '" role="listitem">' +
                    '<span class="' +
                    spanClass +
                    '">' +
                    escapeHtml(title) +
                    '</span></a>'
                );
            })
            .join('');
    }

    function escapeHtml(s) {
        return s
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    function escapeAttr(s) {
        return escapeHtml(s).replace(/'/g, '&#39;');
    }

    async function runSearch() {
        const q = input.value.trim();
        if (!q) {
            resultsHeading.textContent = '';
            resultsList.innerHTML = '';
            resultsBlock.hidden = true;
            return;
        }

        setLoading(true);

        try {
            const params = new URLSearchParams({ q: q });
            if (selectedDir) params.set('dir', selectedDir);
            const url = '/api/search?' + params.toString();
            const res = await fetch(url, { headers: { Accept: 'application/json' } });
            if (!res.ok) throw new Error('Arama isteği başarısız');
            const data = await res.json();
            renderResults(data);
        } catch (e) {
            resultsHeading.textContent = 'Arama yapılamadı';
            resultsList.innerHTML =
                '<p class="px-[12px] py-[16px] text-14 text-gray border-b border-[#EBEBEB]">Bir hata oluştu. Lütfen tekrar deneyin.</p>';
            resultsBlock.hidden = false;
        } finally {
            input.removeAttribute('aria-busy');
        }
    }

    form.addEventListener('submit', function (e) {
        e.preventDefault();
        runSearch();
    });
})();
