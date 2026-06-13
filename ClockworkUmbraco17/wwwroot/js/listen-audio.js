(function () {
    var audioCache = new Map();
    var currentAudio = null;
    var audioContext = null;
    var pollIntervalMs = 1000;
    var pollAttempts = 10;

    function buildAudioUrl(baseUrl, btn) {
        var url = new URL(baseUrl || '/api/dictionary/audio', window.location.href);
        url.searchParams.set('text', btn.getAttribute('data-text') || '');
        url.searchParams.set('language', btn.getAttribute('data-language') || btn.getAttribute('data-lang') || 'en-US');
        url.searchParams.set('sourceType', btn.getAttribute('data-source-type') || 'word');
        return url.pathname + url.search;
    }

    function setLoading(btn, loading) {
        btn.disabled = loading;
        btn.classList.toggle('is-loading', loading);
        btn.setAttribute('aria-busy', loading ? 'true' : 'false');
    }

    function getAudioContext() {
        var AudioContextCtor = window.AudioContext || window.webkitAudioContext;
        if (!AudioContextCtor) return null;
        if (!audioContext) {
            audioContext = new AudioContextCtor();
        }
        return audioContext;
    }

    function unlockAudioPlayback() {
        var ctx = getAudioContext();
        if (!ctx) return;

        if (ctx.state === 'suspended' && typeof ctx.resume === 'function') {
            ctx.resume().catch(function () {});
        }
    }

    function playWithAudioContext(url) {
        var ctx = getAudioContext();
        if (!ctx) {
            return Promise.reject(new Error('audio context unavailable'));
        }

        return fetch(url)
            .then(function (res) {
                if (!res.ok) throw new Error('audio fetch failed');
                return res.arrayBuffer();
            })
            .then(function (buffer) {
                return ctx.decodeAudioData(buffer);
            })
            .then(function (decoded) {
                var source = ctx.createBufferSource();
                source.buffer = decoded;
                source.connect(ctx.destination);
                source.start(0);
            });
    }

    function playUrl(url) {
        if (currentAudio) {
            currentAudio.pause();
        }

        if (!currentAudio) {
            currentAudio = new Audio();
            currentAudio.preload = 'auto';
            currentAudio.playsInline = true;
        }

        currentAudio.src = url;
        currentAudio.load();

        return currentAudio.play().catch(function () {
            return playWithAudioContext(url);
        });
    }

    function pollStatus(baseUrl, hash, attemptsLeft) {
        if (!hash || attemptsLeft <= 0) {
            return Promise.resolve(null);
        }

        var statusUrl = (baseUrl || '/api/dictionary/audio').replace(/\/$/, '') + '/status/' + encodeURIComponent(hash);
        return new Promise(function (resolve) {
            window.setTimeout(resolve, pollIntervalMs);
        })
            .then(function () {
                return fetch(statusUrl, { headers: { Accept: 'application/json' } });
            })
            .then(function (res) {
                if (!res.ok) return null;
                return res.json();
            })
            .then(function (data) {
                var status = data && (data.status || data.Status);
                var url = data && (data.url || data.Url);
                if (status === 'ready' && url) {
                    return url;
                }

                return pollStatus(baseUrl, hash, attemptsLeft - 1);
            })
            .catch(function () {
                return null;
            });
    }

    function requestAndPlay(btn, baseUrl) {
        var text = (btn.getAttribute('data-text') || '').trim();
        if (!text) return;

        var cacheKey = [
            text,
            btn.getAttribute('data-language') || btn.getAttribute('data-lang') || 'en-US',
            btn.getAttribute('data-source-type') || 'word',
        ].join('|');

        if (audioCache.has(cacheKey)) {
            playUrl(audioCache.get(cacheKey));
            return;
        }

        setLoading(btn, true);
        unlockAudioPlayback();
        fetch(buildAudioUrl(baseUrl, btn), { headers: { Accept: 'application/json' } })
            .then(function (res) {
                if (res.status !== 200 && res.status !== 202) {
                    throw new Error('audio request failed');
                }

                return res.json();
            })
            .then(function (data) {
                var status = data && (data.status || data.Status);
                var hash = data && (data.hash || data.Hash);
                var url = data && (data.url || data.Url);

                if (status === 'ready' && url) {
                    audioCache.set(cacheKey, url);
                    return playUrl(url);
                }

                return pollStatus(baseUrl, hash, pollAttempts).then(function (readyUrl) {
                    if (readyUrl) {
                        audioCache.set(cacheKey, readyUrl);
                        return playUrl(readyUrl);
                    }
                });
            })
            .catch(function () {
                btn.classList.add('has-error');
                window.setTimeout(function () {
                    btn.classList.remove('has-error');
                }, 1500);
            })
            .finally(function () {
                setLoading(btn, false);
            });
    }

    document.querySelectorAll('.dictionary-page').forEach(function (page) {
        var baseUrl = page.getAttribute('data-dictionary-audio-url') || '/api/dictionary/audio';
        page.querySelectorAll('.listen-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                unlockAudioPlayback();
                requestAndPlay(btn, baseUrl);
            });
        });
    });
})();
