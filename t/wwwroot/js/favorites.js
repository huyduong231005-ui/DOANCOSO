(function () {
  document.addEventListener("submit", function (event) {
    const form = event.target.closest("[data-favorite-form]");
    if (!form) return;
    if (form.dataset.submitting === "true") {
      event.preventDefault();
      return;
    }

    form.dataset.submitting = "true";
    form.setAttribute("aria-busy", "true");

    const button = form.querySelector('button[type="submit"]');
    if (button) button.disabled = true;
  });
})();
