(function () {
    'use strict';

    // ─────────────────────────────────────────────────────────────────
    // Top progress bar (shows when navigating away / submitting forms)
    // ─────────────────────────────────────────────────────────────────
    let bar;
    function ensureBar() {
        if (bar) return bar;
        bar = document.createElement('div');
        bar.className = 'ux-progress';
        document.body.appendChild(bar);
        return bar;
    }
    function startProgress() {
        const b = ensureBar();
        b.style.transition = 'none';
        b.style.width = '0%';
        b.style.opacity = '1';
        requestAnimationFrame(() => {
            b.style.transition = 'width 8s cubic-bezier(.1,.7,.1,1)';
            b.style.width = '85%';
        });
    }
    function stopProgress() {
        if (!bar) return;
        bar.style.transition = 'width .2s ease, opacity .3s ease .2s';
        bar.style.width = '100%';
        setTimeout(() => { bar.style.opacity = '0'; bar.style.width = '0%'; }, 250);
    }
    window.addEventListener('pageshow', stopProgress);
    window.uxProgress = { start: startProgress, stop: stopProgress };

    // ─────────────────────────────────────────────────────────────────
    // Disable submit button + spinner while form posts
    // ─────────────────────────────────────────────────────────────────
    function disableSubmit(form) {
        form.querySelectorAll('button[type="submit"], input[type="submit"]').forEach(b => {
            if (b.disabled) return;
            b.disabled = true;
            b.dataset.uxOriginal = b.innerHTML;
            const hasText = b.textContent.trim().length > 0;
            b.innerHTML = '<span class="material-symbols-outlined ux-spin">progress_activity</span>'
                + (hasText ? '<span>Đang xử lý...</span>' : '');
        });
    }

    // ─────────────────────────────────────────────────────────────────
    // Modal confirm (replaces window.confirm)
    //   <form data-confirm="..." data-confirm-tone="danger|info"
    //         data-confirm-title="..." data-confirm-ok="..."> ... </form>
    // ─────────────────────────────────────────────────────────────────
    function openConfirm(opts) {
        return new Promise(resolve => {
            const tone = opts.tone === 'info' ? 'info' : 'danger';
            const overlay = document.createElement('div');
            overlay.className = 'ux-modal-overlay';
            overlay.innerHTML =
                '<div class="ux-modal" role="dialog" aria-modal="true">' +
                  '<div class="ux-modal-icon ' + tone + '">' +
                    '<span class="material-symbols-outlined">' + (tone === 'danger' ? 'warning' : 'help') + '</span>' +
                  '</div>' +
                  '<h3 class="ux-modal-title">' + escapeHtml(opts.title || 'Xác nhận') + '</h3>' +
                  '<p class="ux-modal-msg">' + escapeHtml(opts.message || '') + '</p>' +
                  '<div class="ux-modal-actions">' +
                    '<button type="button" class="btn btn-ghost" data-ux-cancel>' + escapeHtml(opts.cancelText || 'Huỷ') + '</button>' +
                    '<button type="button" class="btn ' + (tone === 'danger' ? 'btn-danger' : 'btn-primary') + '" data-ux-ok>' + escapeHtml(opts.okText || 'Xác nhận') + '</button>' +
                  '</div>' +
                '</div>';
            document.body.appendChild(overlay);
            requestAnimationFrame(() => overlay.classList.add('ux-modal-show'));

            const close = (val) => {
                overlay.classList.remove('ux-modal-show');
                setTimeout(() => overlay.remove(), 200);
                document.removeEventListener('keydown', onKey);
                resolve(val);
            };
            const onKey = (ev) => {
                if (ev.key === 'Escape') close(false);
                if (ev.key === 'Enter')  close(true);
            };
            overlay.querySelector('[data-ux-ok]').addEventListener('click', () => close(true));
            overlay.querySelector('[data-ux-cancel]').addEventListener('click', () => close(false));
            overlay.addEventListener('click', e => { if (e.target === overlay) close(false); });
            document.addEventListener('keydown', onKey);
            setTimeout(() => overlay.querySelector('[data-ux-ok]').focus(), 50);
        });
    }
    function escapeHtml(s) {
        return String(s).replace(/[&<>"']/g, ch => ({
            '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
        }[ch]));
    }
    window.uxConfirm = openConfirm;

    // Intercept any form submit
    document.addEventListener('submit', async function (e) {
        const form = e.target;
        if (!(form instanceof HTMLFormElement)) return;

        // 1) data-confirm — open modal first
        const msg = form.dataset.confirm;
        if (msg && form.dataset.uxConfirmed !== '1') {
            e.preventDefault();
            const ok = await openConfirm({
                title:   form.dataset.confirmTitle || 'Xác nhận',
                message: msg,
                tone:    form.dataset.confirmTone  || 'danger',
                okText:  form.dataset.confirmOk    || 'Xác nhận'
            });
            if (!ok) return;
            form.dataset.uxConfirmed = '1';
            form.requestSubmit ? form.requestSubmit() : form.submit();
            return;
        }

        // 2) start progress + disable button (skip filter-bar, handled below)
        if (!form.classList.contains('filter-bar') && form.dataset.uxNoDisable === undefined) {
            startProgress();
            setTimeout(() => disableSubmit(form), 0);
        }
    }, true);

    // ─────────────────────────────────────────────────────────────────
    // Filter form auto-submit (debounce on input, instant on change)
    // Inputs with data-suggest-url are SKIPPED (handled by autocomplete).
    // ─────────────────────────────────────────────────────────────────
    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('form.filter-bar').forEach(form => {
            let timer;
            const submit = () => {
                clearTimeout(timer);
                startProgress();
                form.requestSubmit ? form.requestSubmit() : form.submit();
            };
            const debounced = () => {
                clearTimeout(timer);
                timer = setTimeout(submit, 450);
            };
            form.querySelectorAll('input[type="search"], input[type="text"], input[type="number"]').forEach(el => {
                if (el.dataset.suggestUrl) return; // autocomplete owns this input
                el.addEventListener('input', debounced);
            });
            form.querySelectorAll('select').forEach(el => {
                el.addEventListener('change', submit);
            });
            // Keep the manual "Lọc" button working as-is.
        });

        // ─────────────────────────────────────────────────────────────
        // Autocomplete (input[data-suggest-url])
        //   Response shape:
        //   [{ title, subtitle?, url, icon?, thumb? }]
        // ─────────────────────────────────────────────────────────────
        document.querySelectorAll('input[data-suggest-url]').forEach(initSuggest);
    });

    function initSuggest(input) {
        const url = input.dataset.suggestUrl;
        if (!url) return;
        const defaultIcon = input.dataset.suggestIcon || 'search';

        // Wrap input so dropdown can be absolutely positioned
        const wrap = document.createElement('div');
        wrap.className = 'ux-suggest-wrap';
        input.parentNode.insertBefore(wrap, input);
        wrap.appendChild(input);

        const dd = document.createElement('div');
        dd.className = 'ux-suggest-dd';
        dd.setAttribute('role', 'listbox');
        wrap.appendChild(dd);

        let items = [];
        let active = -1;
        let timer, ctrl;

        function close() {
            dd.classList.remove('open');
            dd.innerHTML = '';
            items = []; active = -1;
        }
        function render() {
            if (items.length === 0) {
                dd.innerHTML = '<div class="ux-suggest-empty">Không tìm thấy kết quả</div>';
                dd.classList.add('open');
                return;
            }
            dd.innerHTML = items.map((it, i) =>
                '<a class="ux-suggest-item' + (i === active ? ' active' : '') + '" href="' + escapeAttr(it.url) + '" data-idx="' + i + '">' +
                  (it.thumb
                    ? '<img class="ux-suggest-thumb" src="' + escapeAttr(it.thumb) + '" alt="" />'
                    : '<span class="ux-suggest-icon material-symbols-outlined">' + escapeAttr(it.icon || defaultIcon) + '</span>') +
                  '<span class="ux-suggest-body">' +
                    '<span class="ux-suggest-title">' + escapeHtml(it.title || '') + '</span>' +
                    (it.subtitle ? '<span class="ux-suggest-sub">' + escapeHtml(it.subtitle) + '</span>' : '') +
                  '</span>' +
                '</a>'
            ).join('');
            dd.classList.add('open');
        }
        async function fetchSuggest(q) {
            if (ctrl) ctrl.abort();
            ctrl = new AbortController();
            const sep = url.includes('?') ? '&' : '?';
            try {
                const r = await fetch(url + sep + 'q=' + encodeURIComponent(q), {
                    signal: ctrl.signal,
                    headers: { 'Accept': 'application/json' }
                });
                if (!r.ok) { close(); return; }
                const data = await r.json();
                items = Array.isArray(data) ? data : [];
                active = -1;
                render();
            } catch (e) { /* aborted or net error */ }
        }
        function trigger() {
            clearTimeout(timer);
            const q = input.value.trim();
            if (q.length < 1) { close(); return; }
            timer = setTimeout(() => fetchSuggest(q), 220);
        }

        input.setAttribute('autocomplete', 'off');
        input.addEventListener('input', trigger);
        input.addEventListener('focus', () => { if (input.value.trim().length >= 1) trigger(); });
        input.addEventListener('keydown', (e) => {
            if (!dd.classList.contains('open') || items.length === 0) return;
            if (e.key === 'ArrowDown') { e.preventDefault(); active = (active + 1) % items.length; render(); }
            else if (e.key === 'ArrowUp')   { e.preventDefault(); active = (active - 1 + items.length) % items.length; render(); }
            else if (e.key === 'Enter' && active >= 0) {
                e.preventDefault();
                startProgress();
                window.location.href = items[active].url;
            }
            else if (e.key === 'Escape') { close(); }
        });
        document.addEventListener('click', (e) => {
            if (!wrap.contains(e.target)) close();
        });
        dd.addEventListener('mousedown', (e) => {
            const a = e.target.closest('.ux-suggest-item');
            if (a) startProgress();
        });
    }
    function escapeAttr(s) {
        return String(s).replace(/[&<>"']/g, ch => ({
            '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
        }[ch]));
    }

    // Also show progress on plain <a> clicks (admin navigation)
    document.addEventListener('click', function (e) {
        const a = e.target.closest('a');
        if (!a) return;
        const href = a.getAttribute('href');
        if (!href || href.startsWith('#') || href.startsWith('javascript:')) return;
        if (a.target === '_blank' || a.hasAttribute('download')) return;
        if (e.ctrlKey || e.metaKey || e.shiftKey || e.button !== 0) return;
        startProgress();
    }, true);

    // ─────────────────────────────────────────────────────────────────
    // Clickable rows: <tr data-href="...">  → click anywhere on row
    // Skips clicks on a / button / input / form / .actions-cell.
    // Ctrl/Cmd-click opens in new tab.
    // ─────────────────────────────────────────────────────────────────
    document.addEventListener('click', function (e) {
        const tr = e.target.closest('tr[data-href]');
        if (!tr) return;
        // Don't hijack clicks meant for interactive children
        if (e.target.closest('a, button, input, label, select, textarea, form, .actions-cell, .no-row-click')) return;
        const href = tr.dataset.href;
        if (!href) return;
        if (e.ctrlKey || e.metaKey || e.shiftKey || e.button !== 0) {
            window.open(href, '_blank');
            return;
        }
        startProgress();
        window.location.href = href;
    });
})();
