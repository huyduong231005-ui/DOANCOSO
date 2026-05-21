// Wires <input type="file" data-upload="folder" data-target="#input-id">
// Uploads to /admin/uploads/{folder}, appends URL into target text input (comma-separated).
(function () {
    function attach(input) {
        if (input.dataset.uploadBound === '1') return;
        input.dataset.uploadBound = '1';

        var folder = input.dataset.upload || 'misc';
        var targetSel = input.dataset.target;
        var preview = input.parentElement && input.parentElement.querySelector('[data-upload-preview]');

        input.addEventListener('change', async function () {
            if (!input.files || !input.files.length) return;
            var target = targetSel ? document.querySelector(targetSel) : null;
            var status = input.parentElement && input.parentElement.querySelector('[data-upload-status]');
            if (status) status.textContent = 'Đang tải lên...';

            var fd = new FormData();
            for (var i = 0; i < input.files.length; i++) fd.append('files', input.files[i]);
            try {
                var res = await fetch('/admin/uploads/multi/' + encodeURIComponent(folder), {
                    method: 'POST', body: fd, credentials: 'same-origin'
                });
                if (!res.ok) throw new Error('HTTP ' + res.status);
                var json = await res.json();
                var urls = json.items.filter(function (i) { return i.success; }).map(function (i) { return i.url; });
                if (target && urls.length) {
                    var current = (target.value || '').split(',').map(function (s) { return s.trim(); }).filter(Boolean);
                    current = current.concat(urls);
                    target.value = current.join(', ');
                    target.dispatchEvent(new Event('change'));
                }
                if (preview) {
                    urls.forEach(function (u) {
                        var img = document.createElement('img');
                        img.src = u;
                        img.style.cssText = 'width:90px;height:70px;object-fit:cover;border-radius:6px;border:1px solid var(--admin-line);margin:2px;';
                        preview.appendChild(img);
                    });
                }
                if (status) status.textContent = urls.length + '/' + json.items.length + ' tệp đã tải';
                input.value = '';
            } catch (e) {
                if (status) status.textContent = 'Lỗi tải lên: ' + e.message;
            }
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('input[type=file][data-upload]').forEach(attach);
    });
})();
