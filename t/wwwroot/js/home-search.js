(function () {
  "use strict";

  var form = document.querySelector("[data-name-search]");
  if (!form) return;

  var input = form.querySelector("[data-suggest-input]");
  var list = form.querySelector("[data-suggest-list]");
  if (!input || !list) return;

  var suggestUrl = input.getAttribute("data-suggest-url");
  var detailUrl = input.getAttribute("data-detail-url");
  var minChars = 2;
  var debounceMs = 220;

  var timer = null;
  var controller = null;
  var items = [];
  var activeIndex = -1;
  var lastQuery = "";

  function formatPrice(value) {
    if (value == null) return "";
    var n = Number(value);
    if (isNaN(n)) return "";
    if (n >= 1000000) {
      var m = n / 1000000;
      return (Math.round(m * 10) / 10).toString().replace(".0", "") + "tr/tháng";
    }
    return n.toLocaleString("vi-VN") + "đ/tháng";
  }

  function escapeHtml(s) {
    return String(s == null ? "" : s)
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;");
  }

  function highlight(text, query) {
    var safe = escapeHtml(text);
    if (!query) return safe;
    var idx = safe.toLowerCase().indexOf(query.toLowerCase());
    if (idx < 0) return safe;
    return (
      safe.slice(0, idx) +
      "<mark>" + safe.slice(idx, idx + query.length) + "</mark>" +
      safe.slice(idx + query.length)
    );
  }

  function detailHref(id) {
    if (!detailUrl) return "#";
    // Url.Action renders ".../ApartmentDetail"; append /id
    return detailUrl.replace(/\/$/, "") + "/" + id;
  }

  function closeList() {
    list.hidden = true;
    list.innerHTML = "";
    items = [];
    activeIndex = -1;
    input.setAttribute("aria-expanded", "false");
  }

  function render(data, query) {
    items = data || [];
    activeIndex = -1;
    if (!items.length) {
      list.innerHTML =
        '<div class="search-suggest-empty">Không tìm thấy tin nào khớp "' +
        escapeHtml(query) + '"</div>';
      list.hidden = false;
      input.setAttribute("aria-expanded", "true");
      return;
    }
    var html = items
      .map(function (it, i) {
        var cover = it.cover
          ? '<img src="' + escapeHtml(it.cover) + '" alt="" loading="lazy">'
          : '<span class="material-symbols-outlined">apartment</span>';
        return (
          '<a class="search-suggest-item" role="option" data-index="' + i +
          '" href="' + escapeHtml(detailHref(it.id)) + '">' +
          '<span class="search-suggest-thumb">' + cover + "</span>" +
          '<span class="search-suggest-body">' +
          '<span class="search-suggest-title">' + highlight(it.title, query) + "</span>" +
          '<span class="search-suggest-meta">' + escapeHtml(it.address || "") + "</span>" +
          "</span>" +
          '<span class="search-suggest-price">' + escapeHtml(formatPrice(it.price)) + "</span>" +
          "</a>"
        );
      })
      .join("");
    list.innerHTML = html;
    list.hidden = false;
    input.setAttribute("aria-expanded", "true");
  }

  function setActive(next) {
    var nodes = list.querySelectorAll(".search-suggest-item");
    if (!nodes.length) return;
    if (activeIndex >= 0 && nodes[activeIndex]) nodes[activeIndex].classList.remove("is-active");
    activeIndex = (next + nodes.length) % nodes.length;
    nodes[activeIndex].classList.add("is-active");
    nodes[activeIndex].scrollIntoView({ block: "nearest" });
  }

  function fetchSuggestions(query) {
    if (controller) controller.abort();
    controller = new AbortController();
    fetch(suggestUrl + "?q=" + encodeURIComponent(query), {
      signal: controller.signal,
      headers: { "X-Requested-With": "XMLHttpRequest" }
    })
      .then(function (r) { return r.ok ? r.json() : []; })
      .then(function (data) {
        if (input.value.trim() === query) render(data, query);
      })
      .catch(function () { /* aborted or network error: ignore */ });
  }

  input.addEventListener("input", function () {
    var query = input.value.trim();
    lastQuery = query;
    if (timer) clearTimeout(timer);
    if (query.length < minChars) {
      closeList();
      return;
    }
    timer = setTimeout(function () { fetchSuggestions(query); }, debounceMs);
  });

  input.addEventListener("keydown", function (e) {
    if (list.hidden) return;
    if (e.key === "ArrowDown") { e.preventDefault(); setActive(activeIndex + 1); }
    else if (e.key === "ArrowUp") { e.preventDefault(); setActive(activeIndex - 1); }
    else if (e.key === "Enter") {
      if (activeIndex >= 0 && items[activeIndex]) {
        e.preventDefault();
        window.location.href = detailHref(items[activeIndex].id);
      }
      // otherwise let the form submit normally (keyword search)
    } else if (e.key === "Escape") {
      closeList();
    }
  });

  input.addEventListener("focus", function () {
    if (input.value.trim().length >= minChars && items.length) {
      list.hidden = false;
    }
  });

  document.addEventListener("click", function (e) {
    if (!form.contains(e.target)) closeList();
  });

  // Prevent submitting an empty keyword (avoid useless full-list reload on blank).
  form.addEventListener("submit", function (e) {
    if (!input.value.trim()) {
      e.preventDefault();
      input.focus();
    }
  });
})();
