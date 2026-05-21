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

    document.addEventListener('DOMContentLoaded', () => {
        initImagePicker();
        initAutoSuggest();
    });
})();
