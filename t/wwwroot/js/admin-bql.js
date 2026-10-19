// Ban quản lý: ngăn trượt chi tiết căn + lọc/tìm trên sơ đồ toà.
(function () {
    const body = document.body;
    const drawer = document.getElementById('admin-drawer');
    const content = drawer && drawer.querySelector('[data-drawer-content]');
    let lastTrigger = null;

    async function openDrawer(url, trigger) {
        if (!drawer) { window.location.href = url; return; }
        document.querySelectorAll('.unit.selected').forEach(u => u.classList.remove('selected'));
        if (trigger) trigger.classList.add('selected');
        lastTrigger = trigger;
        content.innerHTML = '<div class="drawer-loading">Đang tải…</div>';
        body.classList.add('drawer-open');
        drawer.setAttribute('aria-hidden', 'false');
        try {
            const res = await fetch(url, { headers: { 'X-Requested-With': 'fetch' } });
            if (!res.ok) throw new Error(res.status);
            content.innerHTML = await res.text();
            const focusable = content.querySelector('[data-drawer-close], a, button');
            if (focusable) focusable.focus();
        } catch (e) {
            content.innerHTML = '<div class="drawer-loading">Không tải được thông tin căn hộ.</div>';
        }
    }

    function closeDrawer() {
        body.classList.remove('drawer-open');
        if (drawer) drawer.setAttribute('aria-hidden', 'true');
        document.querySelectorAll('.unit.selected').forEach(u => u.classList.remove('selected'));
        if (lastTrigger) lastTrigger.focus();
    }

    document.addEventListener('click', function (e) {
        const trigger = e.target.closest('[data-drawer-url]');
        if (trigger) {
            // Ctrl/Cmd-click mở trang chi tiết đầy đủ ở tab mới.
            if ((e.ctrlKey || e.metaKey) && trigger.dataset.href) { window.open(trigger.dataset.href, '_blank'); return; }
            e.preventDefault();
            openDrawer(trigger.dataset.drawerUrl, trigger);
            return;
        }
        if (e.target.closest('[data-drawer-close]')) closeDrawer();
    });
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && body.classList.contains('drawer-open')) closeDrawer();
    });

    // Sơ đồ toà: chip lọc theo tình trạng + ô tìm mã căn / tên cư dân.
    document.querySelectorAll('[data-plan]').forEach(function (plan) {
        const root = plan.closest('[data-plan-root]') || document;
        const chips = root.querySelectorAll('[data-plan-filter]');
        const search = root.querySelector('[data-plan-search]');
        const units = plan.querySelectorAll('.unit');
        let filter = '';

        function apply() {
            const q = (search && search.value || '').trim().toLowerCase();
            const active = !!filter || !!q;
            plan.classList.toggle('filtering', active);
            units.forEach(function (u) {
                const tags = (u.dataset.tags || '').split(' ');
                const okFilter = !filter || tags.includes(filter);
                const okSearch = !q || (u.dataset.search || '').includes(q);
                u.classList.toggle('match', okFilter && okSearch);
            });
        }

        chips.forEach(function (chip) {
            chip.addEventListener('click', function () {
                filter = filter === chip.dataset.planFilter ? '' : chip.dataset.planFilter;
                chips.forEach(c => c.classList.toggle('active', c.dataset.planFilter === filter && !!filter));
                apply();
            });
        });
        if (search) search.addEventListener('input', apply);
    });

    // Bảng ghi chỉ số: hiện tiêu thụ ngay khi nhập.
    document.querySelectorAll('[data-bulk-row]').forEach(function (row) {
        const input = row.querySelector('input[type="number"]');
        const out = row.querySelector('.consumption');
        const prev = parseFloat(row.dataset.prev || '0');
        if (!input || !out) return;
        function update() {
            if (input.value === '') { out.textContent = '—'; out.classList.remove('bad'); return; }
            const used = parseFloat(input.value) - prev;
            out.textContent = isNaN(used) ? '—' : used.toLocaleString('vi-VN');
            out.classList.toggle('bad', used < 0);
        }
        input.addEventListener('input', update);
        update();
    });
})();
