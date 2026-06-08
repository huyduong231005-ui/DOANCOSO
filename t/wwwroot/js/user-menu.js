(function () {
  "use strict";

  function closeAll(except) {
    document.querySelectorAll("[data-user-menu].open").forEach(function (menu) {
      if (menu === except) return;
      menu.classList.remove("open");
      const trigger = menu.querySelector("[data-user-menu-trigger]");
      if (trigger) trigger.setAttribute("aria-expanded", "false");
    });
  }

  document.addEventListener("click", function (event) {
    const trigger = event.target.closest("[data-user-menu-trigger]");

    if (trigger) {
      const menu = trigger.closest("[data-user-menu]");
      const willOpen = !menu.classList.contains("open");
      closeAll(menu);
      menu.classList.toggle("open", willOpen);
      trigger.setAttribute("aria-expanded", willOpen ? "true" : "false");
      return;
    }

    // Bấm ra ngoài hoặc chọn một mục → đóng menu.
    closeAll(null);
  });

  document.addEventListener("keydown", function (event) {
    if (event.key === "Escape") closeAll(null);
  });
})();
