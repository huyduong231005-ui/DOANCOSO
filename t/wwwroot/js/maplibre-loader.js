(function () {
    'use strict';

    const version = '5.12.0';
    const scriptUrl = `https://unpkg.com/maplibre-gl@${version}/dist/maplibre-gl.js`;
    const stylesheetUrl = `https://unpkg.com/maplibre-gl@${version}/dist/maplibre-gl.css`;
    let loadPromise;

    function ensureStylesheet() {
        if (document.querySelector(`link[href="${stylesheetUrl}"]`)) return;

        const stylesheet = document.createElement('link');
        stylesheet.rel = 'stylesheet';
        stylesheet.href = stylesheetUrl;
        document.head.appendChild(stylesheet);
    }

    window.loadMapLibre = function () {
        ensureStylesheet();

        if (window.maplibregl) {
            return Promise.resolve(window.maplibregl);
        }

        if (!loadPromise) {
            loadPromise = new Promise(function (resolve, reject) {
                const script = document.createElement('script');
                script.src = scriptUrl;
                script.async = true;
                script.onload = function () {
                    resolve(window.maplibregl);
                };
                script.onerror = function () {
                    loadPromise = undefined;
                    reject(new Error('MapLibre GL JS could not be loaded.'));
                };
                document.head.appendChild(script);
            });
        }

        return loadPromise;
    };
})();
