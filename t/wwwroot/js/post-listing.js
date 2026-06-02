(function () {
    'use strict';

    // ── Defaults theo Category slug ─────────────────────────────────
    const CATEGORY_DEFAULTS = {
        'can-ho-cao-cap': {
            area: 70, beds: 2, baths: 2, price: 18000000, depositMultiplier: 2,
            fee: 'Đã bao gồm phí quản lý',
            amenitySlugs: ['wifi', 'ac', 'pool', 'gym', 'security', 'furniture']
        },
        'chung-cu-mini': {
            area: 25, beds: 1, baths: 1, price: 5500000, depositMultiplier: 2,
            fee: 'Điện 4.000đ/kWh, nước 25.000đ/m³',
            amenitySlugs: ['wifi', 'ac', 'furniture', 'security']
        },
        'nha-nguyen-can': {
            area: 100, beds: 3, baths: 3, price: 16000000, depositMultiplier: 2,
            fee: 'Hợp đồng tối thiểu 1 năm',
            amenitySlugs: ['parking', 'wifi', 'ac', 'furniture', 'security']
        },
        'biet-thu': {
            area: 250, beds: 4, baths: 4, price: 40000000, depositMultiplier: 2,
            fee: 'Phí quản lý KĐT đã trừ riêng',
            amenitySlugs: ['parking', 'pool', 'gym', 'security', 'wifi', 'ac', 'furniture']
        },
        'penthouse': {
            area: 150, beds: 3, baths: 3, price: 40000000, depositMultiplier: 2,
            fee: 'Bao phí gym và bể bơi nội khu',
            amenitySlugs: ['parking', 'pool', 'gym', 'security', 'wifi', 'ac', 'furniture']
        },
        'nha-tro': {
            area: 20, beds: 1, baths: 1, price: 3000000, depositMultiplier: 2,
            fee: 'Điện 3.500đ, nước 25.000đ/m³',
            amenitySlugs: ['wifi', 'furniture']
        }
    };

    const ALLOWED_TYPES = ['image/jpeg', 'image/png', 'image/webp'];
    const MAX_SIZE = 5 * 1024 * 1024;
    const MAX_FILES = 15;
    const PHOTON_API_URL = 'https://photon.komoot.io/api/';
    const AUTOCOMPLETE_DELAY_MS = 500;
    const AUTOCOMPLETE_MIN_CHARS = 3;
    const AUTOCOMPLETE_LIMIT = 5;

    // ── Image picker ────────────────────────────────────────────────
    function initImagePicker() {
        const input = document.getElementById('listing-images-input');
        const dropZone = document.getElementById('listing-drop-zone');
        const previewGrid = document.getElementById('listing-preview-grid');
        const coverIndexInput = document.getElementById('CoverImageIndex');
        const errorBox = document.getElementById('listing-images-error');
        if (!input || !dropZone || !previewGrid || !coverIndexInput) return;

        let files = [];
        let coverIdx = 0;

        function showError(msg) { errorBox.textContent = msg || ''; }

        function syncInput() {
            const dt = new DataTransfer();
            files.forEach(f => dt.items.add(f));
            input.files = dt.files;
            coverIndexInput.value = String(coverIdx);
        }

        function render() {
            previewGrid.innerHTML = '';
            files.forEach((file, idx) => {
                const tile = document.createElement('div');
                tile.className = 'lp-tile' + (idx === coverIdx ? ' is-cover' : '');
                tile.setAttribute('draggable', 'true');
                tile.dataset.index = String(idx);

                const img = document.createElement('img');
                img.alt = file.name;
                const reader = new FileReader();
                reader.onload = e => { img.src = e.target.result; };
                reader.readAsDataURL(file);

                const badge = document.createElement('span');
                badge.className = 'lp-badge';
                badge.textContent = idx === coverIdx ? 'Ảnh bìa' : 'Đặt làm bìa';

                const remove = document.createElement('button');
                remove.type = 'button';
                remove.className = 'lp-remove';
                remove.innerHTML = '<span class="material-symbols-outlined">close</span>';
                remove.addEventListener('click', e => {
                    e.stopPropagation();
                    files.splice(idx, 1);
                    if (coverIdx >= files.length) coverIdx = Math.max(0, files.length - 1);
                    syncInput();
                    render();
                });

                tile.addEventListener('click', () => {
                    coverIdx = idx;
                    syncInput();
                    render();
                });

                tile.addEventListener('dragstart', e => {
                    e.dataTransfer.setData('text/plain', String(idx));
                    tile.classList.add('is-dragging');
                });
                tile.addEventListener('dragend', () => tile.classList.remove('is-dragging'));
                tile.addEventListener('dragover', e => { e.preventDefault(); tile.classList.add('is-drop-target'); });
                tile.addEventListener('dragleave', () => tile.classList.remove('is-drop-target'));
                tile.addEventListener('drop', e => {
                    e.preventDefault();
                    tile.classList.remove('is-drop-target');
                    const from = parseInt(e.dataTransfer.getData('text/plain'), 10);
                    const to = idx;
                    if (Number.isNaN(from) || from === to) return;
                    const moved = files.splice(from, 1)[0];
                    files.splice(to, 0, moved);
                    if (coverIdx === from) coverIdx = to;
                    else if (from < coverIdx && to >= coverIdx) coverIdx -= 1;
                    else if (from > coverIdx && to <= coverIdx) coverIdx += 1;
                    syncInput();
                    render();
                });

                tile.append(img, badge, remove);
                previewGrid.appendChild(tile);
            });

            const counter = document.getElementById('listing-images-counter');
            if (counter) counter.textContent = `${files.length}/${MAX_FILES} ảnh`;
        }

        function addFiles(newFiles) {
            const list = Array.from(newFiles);
            const accepted = [];
            const rejected = [];
            for (const f of list) {
                if (!ALLOWED_TYPES.includes(f.type)) { rejected.push(`${f.name}: định dạng không hợp lệ`); continue; }
                if (f.size > MAX_SIZE) { rejected.push(`${f.name}: vượt 5MB`); continue; }
                if (files.length + accepted.length >= MAX_FILES) { rejected.push(`${f.name}: vượt giới hạn ${MAX_FILES} ảnh`); continue; }
                accepted.push(f);
            }
            files = files.concat(accepted);
            showError(rejected.join(' · '));
            syncInput();
            render();
        }

        input.addEventListener('change', () => addFiles(input.files));

        ['dragenter', 'dragover'].forEach(ev => dropZone.addEventListener(ev, e => {
            e.preventDefault();
            dropZone.classList.add('is-active');
        }));
        ['dragleave', 'drop'].forEach(ev => dropZone.addEventListener(ev, e => {
            e.preventDefault();
            dropZone.classList.remove('is-active');
        }));
        dropZone.addEventListener('drop', e => {
            if (e.dataTransfer && e.dataTransfer.files && e.dataTransfer.files.length > 0) {
                addFiles(e.dataTransfer.files);
            }
        });
    }

    // ── Auto-suggest theo Category ──────────────────────────────────
    function initAutoSuggest() {
        const sel = document.querySelector('[data-category-select]');
        if (!sel) return;

        const titleEl = document.getElementById('Title');
        const areaEl = document.getElementById('Area');
        const bedsEl = document.getElementById('Bedrooms');
        const bathsEl = document.getElementById('Bathrooms');
        const priceEl = document.getElementById('Price');
        const depositEl = document.getElementById('DefaultDeposit');
        const feeEl = document.getElementById('FeeNote');

        function isEmpty(el) {
            if (!el) return false;
            return !el.value || el.value === '0' || el.value.trim() === '';
        }

        sel.addEventListener('change', () => {
            const opt = sel.options[sel.selectedIndex];
            if (!opt) return;
            const slug = opt.dataset.slug;
            const cfg = CATEGORY_DEFAULTS[slug];
            if (!cfg) return;

            // Chỉ điền field còn trống — không ghi đè giá trị user đã gõ
            if (isEmpty(areaEl)) areaEl.value = cfg.area;
            if (isEmpty(bedsEl)) bedsEl.value = cfg.beds;
            if (isEmpty(bathsEl)) bathsEl.value = cfg.baths;
            if (isEmpty(priceEl)) priceEl.value = cfg.price;
            if (isEmpty(depositEl)) depositEl.value = (parseFloat(priceEl.value || cfg.price)) * cfg.depositMultiplier;
            if (isEmpty(feeEl)) feeEl.value = cfg.fee;

            // Tick amenity gợi ý (không bỏ tick nếu user đã chọn thêm)
            cfg.amenitySlugs.forEach(slug => {
                const cb = document.querySelector(`input[type="checkbox"][data-amenity-slug="${slug}"]`);
                if (cb && !cb.checked) cb.checked = true;
            });

            // Hiển thị hint
            const hint = document.getElementById('category-hint');
            if (hint) {
                hint.textContent = `Đã gợi ý mẫu cho loại "${opt.text}" — bạn có thể chỉnh lại thoải mái.`;
                hint.classList.add('is-active');
            }
        });
    }

    // ── MapLibre map integration ───────────────────────────────────────
    function showMapMessage(mapContainer, message) {
        mapContainer.classList.add('posting-map-message');
        mapContainer.textContent = message;
    }

    function initAddressAutocomplete(updatePosition) {
        const addressInput = document.querySelector('[data-address-autocomplete]');
        const suggestions = document.getElementById('address-suggestions');
        const status = document.getElementById('address-search-status');
        if (!addressInput || !suggestions || !status) return;
        if (addressInput.dataset.photonAutocompleteInitialized === 'true') return;
        addressInput.dataset.photonAutocompleteInitialized = 'true';

        let requestTimer;
        let activeRequest;

        function setExpanded(expanded) {
            suggestions.hidden = !expanded;
            addressInput.setAttribute('aria-expanded', String(expanded));
        }

        function clearSuggestions(message = '') {
            suggestions.replaceChildren();
            setExpanded(false);
            status.textContent = message;
        }

        function uniqueParts(parts) {
            const seen = new Set();
            return parts.filter(part => {
                if (!part) return false;
                const normalized = part.trim().toLocaleLowerCase();
                if (!normalized || seen.has(normalized)) return false;
                seen.add(normalized);
                return true;
            });
        }

        function formatSuggestion(feature) {
            const properties = feature.properties || {};
            const street = uniqueParts([properties.street, properties.housenumber]).join(' ');
            const primary = properties.name || street || properties.city || properties.state || properties.country;
            const secondary = uniqueParts([
                street !== primary ? street : '',
                properties.district,
                properties.county,
                properties.city,
                properties.state,
                properties.country
            ]).join(', ');

            return {
                primary: primary || 'Địa điểm chưa có tên',
                secondary,
                address: uniqueParts([primary, secondary]).join(', ')
            };
        }

        function renderSuggestions(features) {
            suggestions.replaceChildren();

            const validFeatures = features.filter(feature => {
                const coordinates = feature.geometry && feature.geometry.coordinates;
                return Array.isArray(coordinates)
                    && Number.isFinite(coordinates[0])
                    && Number.isFinite(coordinates[1]);
            });

            if (validFeatures.length === 0) {
                clearSuggestions('Không tìm thấy địa chỉ phù hợp.');
                return;
            }

            validFeatures.forEach(feature => {
                const coordinates = feature.geometry.coordinates;
                const formatted = formatSuggestion(feature);
                const button = document.createElement('button');
                button.type = 'button';
                button.className = 'posting-address-suggestion';
                button.setAttribute('role', 'option');

                const title = document.createElement('strong');
                title.textContent = formatted.primary;
                button.appendChild(title);

                if (formatted.secondary) {
                    const details = document.createElement('small');
                    details.textContent = formatted.secondary;
                    button.appendChild(details);
                }

                button.addEventListener('click', function () {
                    addressInput.value = formatted.address;
                    updatePosition({ lng: coordinates[0], lat: coordinates[1] });
                    clearSuggestions(`Đã chọn: ${formatted.primary}`);
                    addressInput.focus();
                });

                suggestions.appendChild(button);
            });

            status.textContent = `${validFeatures.length} gợi ý địa chỉ.`;
            setExpanded(true);
        }

        addressInput.addEventListener('input', function () {
            window.clearTimeout(requestTimer);
            if (activeRequest) activeRequest.abort();

            const query = addressInput.value.trim();
            if (query.length < AUTOCOMPLETE_MIN_CHARS) {
                clearSuggestions('');
                return;
            }

            status.textContent = 'Đang tìm địa chỉ...';
            requestTimer = window.setTimeout(async function () {
                activeRequest = new AbortController();

                const url = new URL(PHOTON_API_URL);
                url.searchParams.set('q', query);
                url.searchParams.set('limit', String(AUTOCOMPLETE_LIMIT));
                url.searchParams.set('lat', '10.7769');
                url.searchParams.set('lon', '106.7009');

                try {
                    const response = await fetch(url, { signal: activeRequest.signal });
                    if (!response.ok) throw new Error(`Photon returned ${response.status}.`);

                    const payload = await response.json();
                    renderSuggestions(payload.features || []);
                } catch (error) {
                    if (error.name === 'AbortError') return;
                    console.warn('Photon autocomplete error:', error);
                    clearSuggestions('Không thể tải gợi ý địa chỉ. Bạn vẫn có thể chọn vị trí trên bản đồ.');
                }
            }, AUTOCOMPLETE_DELAY_MS);
        });

        addressInput.addEventListener('keydown', function (event) {
            if (event.key === 'Escape') clearSuggestions('');
        });

        document.addEventListener('click', function (event) {
            if (!event.target.closest('.posting-address-field')) clearSuggestions('');
        });
    }

    async function initMap() {
        const mapContainer = document.getElementById('posting-map');
        if (!mapContainer) return;
        if (mapContainer.dataset.mapLibreMapInitialized === 'true') return;
        mapContainer.dataset.mapLibreMapInitialized = 'true';

        const latInput = document.getElementById('Latitude');
        const lngInput = document.getElementById('Longitude');

        const defaultPosition = { lat: 10.762622, lng: 106.660172 };
        const initialPosition = {
            lat: Number.parseFloat(latInput.value) || defaultPosition.lat,
            lng: Number.parseFloat(lngInput.value) || defaultPosition.lng
        };

        try {
            const maplibregl = await window.loadMapLibre();
            const map = new maplibregl.Map({
                container: mapContainer,
                style: 'https://tiles.openfreemap.org/styles/liberty',
                center: [initialPosition.lng, initialPosition.lat],
                zoom: 14,
                scrollZoom: false
            });
            map.addControl(new maplibregl.NavigationControl(), 'top-right');
            const marker = new maplibregl.Marker({ draggable: true })
                .setLngLat([initialPosition.lng, initialPosition.lat])
                .addTo(map);

            function updateCoordinates(position) {
                if (latInput) latInput.value = position.lat.toFixed(6);
                if (lngInput) lngInput.value = position.lng.toFixed(6);
            }

            function updatePosition(position) {
                marker.setLngLat([position.lng, position.lat]);
                map.easeTo({ center: [position.lng, position.lat] });
                updateCoordinates(position);
            }

            if (latInput && lngInput && (!latInput.value || !lngInput.value)) {
                updateCoordinates(initialPosition);
            }

            marker.on('dragend', function () {
                updateCoordinates(marker.getLngLat());
            });

            map.on('click', function (event) {
                updatePosition(event.lngLat);
            });

            initAddressAutocomplete(updatePosition);
        } catch (error) {
            console.error('MapLibre initialization error:', error);
            showMapMessage(mapContainer, 'Không thể tải bản đồ OpenFreeMap. Hãy kiểm tra kết nối mạng.');
        }
    }

    function initPostListingPage() {
        initImagePicker();
        initAutoSuggest();
        initMap();
    }

    document.addEventListener('DOMContentLoaded', initPostListingPage);
    document.addEventListener('luxe:page-loaded', initPostListingPage);
})();
