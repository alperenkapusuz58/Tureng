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
            // #region agent log
            fetch('http://127.0.0.1:7396/ingest/c6b999e4-3319-4540-bcac-5c9789ccfc20',{method:'POST',headers:{'Content-Type':'application/json','X-Debug-Session-Id':'8ad337'},body:JSON.stringify({sessionId:'8ad337',runId:'pre-fix',hypothesisId:'H5',location:'phrase-focus.js:run',message:'Phrase focus skipped because hash is not phrase hash',data:{hash:hash,path:window.location.pathname},timestamp:Date.now()})}).catch(()=>{});
            // #endregion
            return;
        }

        var target = findPhraseElement(hash);
        // #region agent log
        fetch('http://127.0.0.1:7396/ingest/c6b999e4-3319-4540-bcac-5c9789ccfc20',{method:'POST',headers:{'Content-Type':'application/json','X-Debug-Session-Id':'8ad337'},body:JSON.stringify({sessionId:'8ad337',runId:'pre-fix',hypothesisId:'H4,H5',location:'phrase-focus.js:run',message:'Phrase focus hash lookup result',data:{hash:hash,path:window.location.pathname,targetFound:!!target,targetId:target&&target.id,targetPhrase:target&&target.getAttribute('data-phrase')},timestamp:Date.now()})}).catch(()=>{});
        // #endregion
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
