(function () {
    'use strict';

    const defaultPosition = { lat: 10.762622, lng: 106.660172 };
    const openFreeMapStyle = 'https://tiles.openfreemap.org/styles/liberty';

    function showMessage(mapElement, message) {
        mapElement.classList.add('ap-map-message');
        mapElement.textContent = message;
    }

    function createPopupContent(title, address) {
        const wrapper = document.createElement('div');

        if (title) {
            const heading = document.createElement('strong');
            heading.textContent = title;
            wrapper.appendChild(heading);
        }

        if (address) {
            const addressLine = document.createElement('div');
            addressLine.textContent = address;
            wrapper.appendChild(addressLine);
        }

        return wrapper;
    }

    function resolvePosition(lat, lng) {
        if (Number.isFinite(lat) && Number.isFinite(lng) && lat !== 0 && lng !== 0) {
            return { position: [lng, lat], showMarker: true };
        }

        return { position: [defaultPosition.lng, defaultPosition.lat], showMarker: false };
    }

    async function initDetailMap() {
        const mapElement = document.getElementById('detail-map');
        if (!mapElement) return;
        if (mapElement.dataset.mapLibreMapInitialized === 'true') return;
        mapElement.dataset.mapLibreMapInitialized = 'true';

        try {
            const maplibregl = await window.loadMapLibre();
            const lat = Number.parseFloat(mapElement.dataset.latitude);
            const lng = Number.parseFloat(mapElement.dataset.longitude);
            const address = mapElement.dataset.address || '';
            const title = mapElement.dataset.title || '';
            const resolved = resolvePosition(lat, lng);
            const map = new maplibregl.Map({
                container: mapElement,
                style: openFreeMapStyle,
                center: resolved.position,
                zoom: 15,
                scrollZoom: false
            });

            map.addControl(new maplibregl.NavigationControl(), 'top-right');
            if (!resolved.showMarker) return;

            const popup = new maplibregl.Popup({ offset: 22 })
                .setDOMContent(createPopupContent(title, address));

            new maplibregl.Marker()
                .setLngLat(resolved.position)
                .setPopup(popup)
                .addTo(map);
        } catch (error) {
            console.error('MapLibre initialization error:', error);
            showMessage(mapElement, 'Không thể tải bản đồ OpenFreeMap. Hãy kiểm tra kết nối mạng.');
        }
    }

    document.addEventListener('DOMContentLoaded', initDetailMap);
    document.addEventListener('luxe:page-loaded', initDetailMap);
})();
