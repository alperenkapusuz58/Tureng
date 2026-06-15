/**
 * Headword sayfasında #phrase-... hash'ine göre ilgili öbeğe kaydırır ve kısa süre vurgular.
 */
(function () {
    function normalizePhrase(value) {
        return (value || '').replace(/\s+/g, ' ').trim().toLowerCase();
    }

    function findPhraseElement(hash) {
        if (hash) {
            var byId = document.getElementById(hash.replace(/^#/, ''));
            if (byId) {
                return byId;
            }
        }

        return null;
    }

    function focusPhraseElement(element) {
        if (!element) {
            return;
        }

        element.scrollIntoView({ behavior: 'smooth', block: 'center' });
        element.classList.add('is-focused');

        window.setTimeout(function () {
            element.classList.remove('is-focused');
        }, 2600);
    }

    function run() {
        var hash = window.location.hash || '';
        if (!hash || hash.indexOf('phrase-') !== 1) {
            return;
        }

        var target = findPhraseElement(hash);
        if (!target) {
            return;
        }

        window.requestAnimationFrame(function () {
            focusPhraseElement(target);
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', run);
    } else {
        run();
    }

    window.addEventListener('hashchange', run);
})();
