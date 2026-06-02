(function () {
    'use strict';

    function setStatus(button, message) {
        const form = button.closest('form');
        const status = form?.querySelector('[data-nearby-rentals-status]');
        if (status) status.textContent = message || '';
    }

    function warn(button, message) {
        setStatus(button, message);
        window.toast?.warning(message);
    }

    function initNearbyRentals() {
        document.querySelectorAll('[data-nearby-rentals]').forEach((button) => {
            if (button.dataset.nearbyBound === 'true') return;
            button.dataset.nearbyBound = 'true';

            button.addEventListener('click', () => {
                if (!navigator.geolocation) {
                    warn(button, 'Trình duyệt không hỗ trợ xác định vị trí.');
                    return;
                }

                button.disabled = true;
                button.classList.add('is-loading');
                setStatus(button, 'Đang xác định vị trí của bạn...');

                const restoreButton = () => {
                    button.disabled = false;
                    button.classList.remove('is-loading');
                };

                navigator.geolocation.getCurrentPosition(
                    (position) => {
                        const url = new URL(window.location.href);
                        url.searchParams.delete('page');
                        url.searchParams.set('latitude', String(position.coords.latitude));
                        url.searchParams.set('longitude', String(position.coords.longitude));
                        url.searchParams.set('sort', 'distance_asc');
                        window.location.assign(url.toString());
                    },
                    () => {
                        restoreButton();
                        warn(button, 'Không thể lấy vị trí. Vui lòng kiểm tra quyền truy cập vị trí.');
                    },
                    {
                        enableHighAccuracy: false,
                        timeout: 10000,
                        maximumAge: 300000
                    });
            });
        });
    }

    document.addEventListener('DOMContentLoaded', initNearbyRentals);
    document.addEventListener('luxe:page-loaded', initNearbyRentals);
})();
