(function () {
    var audioCache = new Map();
    var blobCache = new Map();
    var currentAudio = null;
    var currentSource = null;
    var audioContext = null;
    var audioUnlocked = false;
    var pollIntervalMs = 1500;
    var pollAttempts = 120;
    var loadTimeoutMs = 8000;
    // Minimal silent MP3 for Safari/iOS user-gesture unlock.
    var silentMp3 = 'data:audio/mp3;base64,SUQzBAAAAAAAI1RTU0UAAAAPAAADTGF2ZjU4Ljc2LjEwMAAAAAAAAAAAAAAA/+M4wAAAAAAAAAAAAEluZm8AAAAPAAAAAwAAAbAAqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqq1dXV1dXV1dXV1dXV1dXV1dXV1dXV1dXV1dXV1dXV1dXV1dXV////////////////////////////////////////////AAAAAExhdmM1OC4xMwAAAAAAAAAAAAAAACQDkAAAAAAAAAGw9wrNaQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA/+MYxAAAAANIAAAAAExBTUUzLjEwMFVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVV/+MYxDsAAANIAAAAAFVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVV/+MYxHYAAANIAAAAAFVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVV';
    var webAudioGainBoost = 3.0;

    function isBlobOrDataUrl(url) {
        return /^blob:|^data:audio\//i.test(url);
    }

    function buildAudioUrl(baseUrl, btn) {
        var url = new URL(baseUrl || '/api/dictionary/audio', window.location.href);
        url.searchParams.set('text', btn.getAttribute('data-text') || '');
        url.searchParams.set('language', btn.getAttribute('data-language') || btn.getAttribute('data-lang') || 'en-US');
        url.searchParams.set('sourceType', btn.getAttribute('data-source-type') || 'word');
        return url.pathname + url.search;
    }

    function resolveAudioUrl(url) {
        try {
            return new URL(url, window.location.href).href;
        } catch (e) {
            return url;
        }
    }

    function setLoading(btn, loading) {
        btn.disabled = loading;
        btn.classList.toggle('is-loading', loading);
        btn.setAttribute('aria-busy', loading ? 'true' : 'false');
    }

    function setPreparing(btn, preparing) {
        btn.classList.toggle('is-preparing', preparing);
        if (preparing) {
            btn.setAttribute('aria-label', 'Ses hazırlanıyor');
            btn.setAttribute('title', 'Ses hazırlanıyor…');
        } else {
            btn.removeAttribute('title');
        }
    }

    function getAudioContext() {
        var AudioContextCtor = window.AudioContext || window.webkitAudioContext;
        if (!AudioContextCtor) return null;
        if (!audioContext) {
            audioContext = new AudioContextCtor();
        }
        return audioContext;
    }

    function ensureAudioElement() {
        if (!currentAudio) {
            currentAudio = new Audio();
            currentAudio.preload = 'auto';
            currentAudio.setAttribute('playsinline', '');
            currentAudio.setAttribute('webkit-playsinline', '');
        }

        currentAudio.volume = 1;
        currentAudio.muted = false;
        currentAudio.playsInline = true;

        return currentAudio;
    }

    function unlockAudioPlayback() {
        var ctx = getAudioContext();
        var resumePromise = Promise.resolve();
        if (ctx && ctx.state === 'suspended' && typeof ctx.resume === 'function') {
            resumePromise = ctx.resume().catch(function () {});
        }

        if (audioUnlocked) {
            return resumePromise;
        }

        return resumePromise.then(function () {
            var audio = ensureAudioElement();
            var previousSrc = audio.src;
            audio.src = silentMp3;

            var playPromise = audio.play();
            if (!playPromise || typeof playPromise.then !== 'function') {
                audioUnlocked = true;
                return;
            }

            return playPromise
                .then(function () {
                    audioUnlocked = true;
                    audio.pause();
                    audio.currentTime = 0;
                    if (previousSrc) {
                        audio.src = previousSrc;
                    } else {
                        audio.removeAttribute('src');
                    }
                    audio.load();
                })
                .catch(function () {});
        });
    }

    function isSameOrigin(url) {
        try {
            return new URL(url, window.location.href).origin === window.location.origin;
        } catch (e) {
            return false;
        }
    }

    function playWithMediaElement(url) {
        var resolvedUrl = resolveAudioUrl(url);
        var audio = ensureAudioElement();
        stopCurrentPlayback();

        return new Promise(function (resolve, reject) {
            var settled = false;
            var timeoutId;

            function cleanup() {
                clearTimeout(timeoutId);
                audio.removeEventListener('canplaythrough', onReady);
                audio.removeEventListener('canplay', onReady);
                audio.removeEventListener('loadeddata', onReady);
                audio.removeEventListener('error', onError);
            }

            function settle(err) {
                if (settled) return;
                settled = true;
                cleanup();
                if (err) {
                    reject(err);
                    return;
                }

                audio.volume = 1;
                audio.muted = false;

                var playPromise = audio.play();
                if (playPromise && typeof playPromise.then === 'function') {
                    playPromise.then(resolve).catch(reject);
                } else {
                    resolve();
                }
            }

            function onReady() {
                settle();
            }

            function onError() {
                settle(new Error('audio load failed'));
            }

            timeoutId = window.setTimeout(function () {
                if (audio.readyState >= HTMLMediaElement.HAVE_CURRENT_DATA) {
                    settle();
                } else {
                    settle(new Error('audio load timeout'));
                }
            }, loadTimeoutMs);

            audio.addEventListener('canplaythrough', onReady, { once: true });
            audio.addEventListener('canplay', onReady, { once: true });
            audio.addEventListener('loadeddata', onReady, { once: true });
            audio.addEventListener('error', onError, { once: true });
            audio.src = resolvedUrl;
            audio.load();
        });
    }

    function fetchBlobUrl(url) {
        var resolvedUrl = resolveAudioUrl(url);

        if (blobCache.has(resolvedUrl)) {
            return Promise.resolve(blobCache.get(resolvedUrl));
        }

        return fetch(resolvedUrl)
            .then(function (res) {
                if (!res.ok) throw new Error('audio fetch failed');
                return res.blob();
            })
            .then(function (blob) {
                var blobUrl = URL.createObjectURL(blob);
                blobCache.set(resolvedUrl, blobUrl);
                return blobUrl;
            });
    }

    function playWithBlobUrl(url) {
        return fetchBlobUrl(url).then(function (blobUrl) {
            return playWithMediaElement(blobUrl);
        });
    }

    function stopCurrentPlayback() {
        if (currentSource) {
            try {
                currentSource.stop();
            } catch (e) {}
            currentSource = null;
        }

        if (currentAudio) {
            currentAudio.pause();
        }
    }

    function playWithAudioContext(url) {
        var ctx = getAudioContext();
        if (!ctx) {
            return Promise.reject(new Error('audio context unavailable'));
        }

        var resumePromise = ctx.state === 'suspended' && typeof ctx.resume === 'function'
            ? ctx.resume().catch(function () {})
            : Promise.resolve();

        return resumePromise
            .then(function () {
                return fetchBlobUrl(url);
            })
            .then(function (blobUrl) {
                return fetch(blobUrl);
            })
            .then(function (res) {
                if (!res.ok) throw new Error('audio fetch failed');
                return res.arrayBuffer();
            })
            .then(function (buffer) {
                return ctx.decodeAudioData(buffer);
            })
            .then(function (decoded) {
                stopCurrentPlayback();

                var source = ctx.createBufferSource();
                var gainNode = ctx.createGain();
                source.buffer = decoded;
                gainNode.gain.value = webAudioGainBoost;
                source.connect(gainNode);
                gainNode.connect(ctx.destination);
                currentSource = source;
                source.onended = function () {
                    if (currentSource === source) {
                        currentSource = null;
                    }
                };
                source.start(0);
            });
    }

    function playUrl(url) {
        var resolvedUrl = resolveAudioUrl(url);

        return playWithAudioContext(resolvedUrl).catch(function () {
            return playWithMediaElement(resolvedUrl).catch(function () {
                if (isBlobOrDataUrl(resolvedUrl) || !isSameOrigin(resolvedUrl)) {
                    return Promise.reject(new Error('audio playback failed'));
                }

                return playWithBlobUrl(resolvedUrl).then(function (blobUrl) {
                    return playWithMediaElement(blobUrl);
                });
            });
        });
    }

    var TERMINAL_STATUSES = ['failed', 'not_found', 'invalid', 'rate_limited'];

    function isTerminalStatus(status) {
        return TERMINAL_STATUSES.indexOf((status || '').toLowerCase()) !== -1;
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
                if (!data) {
                    return pollStatus(baseUrl, hash, attemptsLeft - 1);
                }

                var status = (data.status || data.Status || '').toLowerCase();
                var url = data.url || data.Url;
                var error = data.error || data.Error;

                if (status === 'ready' && url) {
                    return { url: url, error: null };
                }

                if (isTerminalStatus(status)) {
                    return { url: null, error: error || (status === 'rate_limited' ? 'Çok fazla istek. Lütfen biraz bekleyip tekrar deneyin.' : 'Ses üretilemedi') };
                }

                return pollStatus(baseUrl, hash, attemptsLeft - 1);
            })
            .catch(function () {
                return pollStatus(baseUrl, hash, attemptsLeft - 1);
            });
    }

    function prefetchAudio(url) {
        var resolvedUrl = resolveAudioUrl(url);
        if (!isSameOrigin(resolvedUrl) || blobCache.has(resolvedUrl)) {
            return Promise.resolve();
        }

        return fetchBlobUrl(resolvedUrl).catch(function () {});
    }

    function showError(btn, message) {
        btn.classList.add('has-error');
        if (message) {
            btn.setAttribute('title', message);
        }

        window.setTimeout(function () {
            btn.classList.remove('has-error');
            btn.removeAttribute('title');
        }, 2500);
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
            playUrl(audioCache.get(cacheKey)).catch(function () {
                showError(btn, 'Ses çalınamadı');
            });
            return;
        }

        var defaultAriaLabel = btn.getAttribute('aria-label') || 'Dinle';
        setLoading(btn, true);
        fetch(buildAudioUrl(baseUrl, btn), { headers: { Accept: 'application/json' } })
            .then(function (res) {
                if (res.status === 429) {
                    return res.json().catch(function () {
                        return { status: 'rate_limited', error: 'Çok fazla istek. Lütfen biraz bekleyip tekrar deneyin.' };
                    }).then(function (data) {
                        throw new Error((data && (data.error || data.Error)) || 'Çok fazla istek. Lütfen biraz bekleyip tekrar deneyin.');
                    });
                }

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
                    return prefetchAudio(url).then(function () {
                        return playUrl(url);
                    });
                }

                setPreparing(btn, true);
                return pollStatus(baseUrl, hash, pollAttempts).then(function (result) {
                    if (result && result.url) {
                        audioCache.set(cacheKey, result.url);
                        return prefetchAudio(result.url).then(function () {
                            return playUrl(result.url);
                        });
                    }

                    if (result && result.error) {
                        throw new Error(result.error);
                    }

                    throw new Error('Ses hazırlanamadı. Lütfen biraz sonra tekrar deneyin.');
                });
            })
            .catch(function (err) {
                showError(btn, err && err.message ? err.message : 'Ses çalınamadı');
            })
            .finally(function () {
                setPreparing(btn, false);
                setLoading(btn, false);
                btn.setAttribute('aria-label', defaultAriaLabel);
            });
    }

    document.querySelectorAll('.dictionary-page, .home-page').forEach(function (page) {
        var baseUrl = page.getAttribute('data-dictionary-audio-url') || '/api/dictionary/audio';
        page.querySelectorAll('.listen-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                unlockAudioPlayback().then(function () {
                    requestAndPlay(btn, baseUrl);
                });
            });
        });
    });
})();
