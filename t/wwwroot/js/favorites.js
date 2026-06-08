(function () {
  "use strict";

  function applyState(form, isFavorite) {
    // Lần bấm kế tiếp sẽ đảo ngược trạng thái hiện tại.
    const shouldInput = form.querySelector('input[name="shouldBeFavorite"]');
    if (shouldInput) shouldInput.value = (!isFavorite).toString();

    const button = form.querySelector('[data-favorite-button]');
    if (!button) return;

    const icon = button.querySelector(".material-symbols-outlined");
    if (icon) icon.style.fontVariationSettings = isFavorite ? "'FILL' 1" : "";

    const onClass = button.getAttribute("data-fav-on-class");
    const offClass = button.getAttribute("data-fav-off-class");
    if (onClass && offClass) button.className = isFavorite ? onClass : offClass;

    const label = button.querySelector("[data-fav-label]");
    const onLabel = button.getAttribute("data-fav-on-label");
    const offLabel = button.getAttribute("data-fav-off-label");
    if (label && onLabel && offLabel) label.textContent = isFavorite ? onLabel : offLabel;

    const onTitle = button.getAttribute("data-fav-on-title");
    const offTitle = button.getAttribute("data-fav-off-title");
    if (onTitle && offTitle) button.setAttribute("title", isFavorite ? onTitle : offTitle);
  }

  function removeCard(form) {
    const card = form.closest("[data-favorite-card]");
    if (!card) return;
    card.style.transition = "opacity .3s ease, transform .3s ease";
    card.style.opacity = "0";
    card.style.transform = "scale(.96)";
    setTimeout(function () {
      card.remove();

      const counter = document.querySelector("[data-favorite-count]");
      if (counter) {
        const next = Math.max(0, (parseInt(counter.textContent, 10) || 0) - 1);
        counter.textContent = next.toString();
      }

      const grid = document.querySelector("[data-favorite-grid]");
      const empty = document.querySelector("[data-favorite-empty]");
      if (grid && empty && grid.querySelectorAll("[data-favorite-card]").length === 0) {
        grid.classList.add("hidden");
        empty.classList.remove("hidden");
      }
    }, 300);
  }

  document.addEventListener("submit", async function (event) {
    const form = event.target.closest("[data-favorite-form]");
    if (!form) return;

    event.preventDefault();

    if (form.dataset.submitting === "true") return;
    form.dataset.submitting = "true";
    form.setAttribute("aria-busy", "true");

    const button = form.querySelector('button[type="submit"]');
    if (button) button.disabled = true;

    try {
      const response = await fetch(form.action, {
        method: "POST",
        body: new FormData(form),
        credentials: "same-origin",
        headers: { "X-Requested-With": "fetch" }
      });

      if (!response.ok) throw new Error("Yêu cầu thất bại: " + response.status);
      const data = await response.json();

      if (form.hasAttribute("data-favorite-remove") && !data.favorite) {
        removeCard(form);
        return;
      }
      applyState(form, data.favorite);
    } catch (err) {
      // Dự phòng: nếu AJAX lỗi, submit form bình thường như trước.
      console.error("Không thể cập nhật yêu thích:", err);
      form.submit();
    } finally {
      form.dataset.submitting = "false";
      form.removeAttribute("aria-busy");
      if (button) button.disabled = false;
    }
  });
})();
