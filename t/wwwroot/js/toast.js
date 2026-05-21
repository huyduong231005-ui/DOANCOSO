(function () {
    'use strict';

    const ICONS = {
        success: 'check_circle',
        danger: 'error',
        warning: 'warning',
        info: 'info'
    };

    function ensureContainer() {
        let c = document.querySelector('.toast-stack');
        if (!c) {
            c = document.createElement('div');
            c.className = 'toast-stack';
            document.body.appendChild(c);
        }
        return c;
    }

    function escapeHtml(s) {
        return String(s).replace(/[&<>"']/g, ch => ({
            '&': '&amp;', '<': '&lt;', '>': '&gt;',
            '"': '&quot;', "'": '&#39;'
        }[ch]));
    }

    function showToast(type, message, duration) {
        if (!message) return;
        const container = ensureContainer();
        const toast = document.createElement('div');
        toast.className = 'toast toast-' + type;
        toast.setAttribute('role', 'status');
        toast.innerHTML =
            '<span class="material-symbols-outlined toast-icon">' + (ICONS[type] || ICONS.info) + '</span>' +
            '<span class="toast-msg">' + escapeHtml(message) + '</span>' +
            '<button type="button" class="toast-close" aria-label="Đóng">×</button>';
        container.appendChild(toast);

        // Trigger transition
        requestAnimationFrame(() => toast.classList.add('toast-show'));

        let dismissed = false;
        const dismiss = () => {
            if (dismissed) return;
            dismissed = true;
            toast.classList.remove('toast-show');
            toast.classList.add('toast-hide');
            const remove = () => toast.remove();
            toast.addEventListener('transitionend', remove, { once: true });
            setTimeout(remove, 400);
        };

        toast.querySelector('.toast-close').addEventListener('click', dismiss);
        const ttl = (typeof duration === 'number' ? duration : 5000);
        if (ttl > 0) setTimeout(dismiss, ttl);

        return { dismiss };
    }

    // Public API
    window.toast = {
        success: (msg, dur) => showToast('success', msg, dur),
        danger: (msg, dur) => showToast('danger', msg, dur),
        warning: (msg, dur) => showToast('warning', msg, dur),
        info: (msg, dur) => showToast('info', msg, dur),
        show: showToast
    };

    // Auto-mount: pick up server-side TempData
    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[data-toast]').forEach(el => {
            const type = el.dataset.toast || 'info';
            const message = el.dataset.toastMessage || el.textContent.trim();
            showToast(type, message);
            el.remove();
        });
    });
})();
