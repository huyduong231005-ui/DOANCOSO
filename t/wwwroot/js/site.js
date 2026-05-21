(function () {
  const state = {
    slideshowTimer: null,
    showNextSlide: null,
    revealObserver: null,
    softNavBound: false,
    visibilityBound: false,
    navigating: false,
  };

  const stopSlideshow = () => {
    if (state.slideshowTimer !== null) {
      window.clearInterval(state.slideshowTimer);
      state.slideshowTimer = null;
    }
    state.showNextSlide = null;
  };

  const initHeroSlideshow = () => {
    stopSlideshow();

    const heroSlides = Array.from(document.querySelectorAll(".hero-slide"));
    if (heroSlides.length < 2) {
      return;
    }

    let current = heroSlides.findIndex((slide) => slide.classList.contains("is-active"));
    if (current < 0) {
      current = 0;
      heroSlides[0].classList.add("is-active");
    }

    state.showNextSlide = () => {
      const next = (current + 1) % heroSlides.length;
      heroSlides[current].classList.remove("is-active");
      heroSlides[next].classList.add("is-active");
      current = next;
    };

    state.slideshowTimer = window.setInterval(state.showNextSlide, 10000);

    if (!state.visibilityBound) {
      document.addEventListener("visibilitychange", () => {
        if (!state.showNextSlide) {
          return;
        }

        if (document.hidden) {
          stopSlideshow();
        } else if (state.slideshowTimer === null) {
          state.slideshowTimer = window.setInterval(state.showNextSlide, 10000);
        }
      });

      state.visibilityBound = true;
    }
  };

  const initReveal = () => {
    if (state.revealObserver) {
      state.revealObserver.disconnect();
      state.revealObserver = null;
    }

    const revealItems = Array.from(document.querySelectorAll(".reveal-item"));
    if (!revealItems.length) {
      return;
    }

    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
      revealItems.forEach((item) => item.classList.add("is-visible"));
      return;
    }

    document.body.classList.add("js-reveal");

    revealItems.forEach((item, index) => {
      const customDelay = Number(item.getAttribute("data-reveal-delay"));
      const delay = Number.isFinite(customDelay) ? customDelay : (index % 5) * 55;
      item.style.setProperty("--reveal-delay", `${delay}ms`);
    });

    const revealIfVisible = (item) => {
      const rect = item.getBoundingClientRect();
      return rect.top <= window.innerHeight * 0.9 && rect.bottom >= 0;
    };

    revealItems.forEach((item) => {
      if (revealIfVisible(item)) {
        item.classList.add("is-visible");
      }
    });

    state.revealObserver = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (!entry.isIntersecting) {
            return;
          }
          entry.target.classList.add("is-visible");
          state.revealObserver?.unobserve(entry.target);
        });
      },
      {
        threshold: 0.16,
        rootMargin: "0px 0px -4% 0px",
      },
    );

    revealItems.forEach((item) => {
      if (!item.classList.contains("is-visible")) {
        state.revealObserver?.observe(item);
      }
    });
  };

  const initPageEffects = () => {
    initHeroSlideshow();
    initReveal();
    initApartmentDetail();
    initAuthForms();
    initFormValidation();
  };

  const initApartmentDetail = () => {
    const toggle = document.querySelector("[data-phone-toggle]");
    const phoneValue = document.querySelector("[data-phone-value]");

    if (toggle && phoneValue && toggle.dataset.bound !== "true") {
      toggle.dataset.bound = "true";
      let visible = false;

      toggle.addEventListener("click", () => {
        const hiddenPhone = phoneValue.getAttribute("data-phone-hidden") || "";
        const visiblePhone = phoneValue.getAttribute("data-phone-visible") || hiddenPhone;
        visible = !visible;

        phoneValue.textContent = visible ? visiblePhone : hiddenPhone;
        toggle.textContent = visible ? "Ẩn số" : "Hiện số";
      });
    }

    initApartmentGallery();
  };

  const initApartmentGallery = () => {
    const gallery = document.querySelector("[data-gallery-images]");
    const lightbox = document.querySelector("[data-ap-lightbox]");
    const lightboxImage = lightbox?.querySelector("[data-ap-lightbox-image]");
    const lightboxCounter = lightbox?.querySelector("[data-ap-lightbox-counter]");
    const closeButton = lightbox?.querySelector("[data-ap-lightbox-close]");
    const prevButton = lightbox?.querySelector("[data-ap-lightbox-prev]");
    const nextButton = lightbox?.querySelector("[data-ap-lightbox-next]");

    if (!gallery || !lightbox || !lightboxImage || !lightboxCounter || !closeButton || !prevButton || !nextButton) {
      return;
    }

    let images = [];
    const galleryJson = gallery.getAttribute("data-gallery-images") || "[]";

    try {
      const parsed = JSON.parse(galleryJson);
      if (Array.isArray(parsed)) {
        images = parsed.filter((item) => typeof item === "string" && item.trim().length > 0);
      }
    } catch {
      images = [];
    }

    if (!images.length) {
      return;
    }

    const openers = Array.from(gallery.querySelectorAll("[data-gallery-open]"));
    if (!openers.length) {
      return;
    }

    const normalizeIndex = (index) => {
      if (index < 0) return images.length - 1;
      if (index >= images.length) return 0;
      return index;
    };

    let currentIndex = 0;

    const render = () => {
      currentIndex = normalizeIndex(currentIndex);
      lightboxImage.setAttribute("src", images[currentIndex]);
      lightboxCounter.textContent = `${currentIndex + 1}/${images.length}`;
    };

    const open = (index) => {
      currentIndex = normalizeIndex(index);
      render();
      lightbox.classList.add("is-open");
      lightbox.setAttribute("aria-hidden", "false");
      document.body.classList.add("ap-lightbox-open");
      lightbox.focus();
    };

    const close = () => {
      lightbox.classList.remove("is-open");
      lightbox.setAttribute("aria-hidden", "true");
      document.body.classList.remove("ap-lightbox-open");
    };

    const prev = () => {
      currentIndex = normalizeIndex(currentIndex - 1);
      render();
    };

    const next = () => {
      currentIndex = normalizeIndex(currentIndex + 1);
      render();
    };

    openers.forEach((opener) => {
      if (!(opener instanceof HTMLElement) || opener.dataset.galleryBound === "true") {
        return;
      }

      opener.dataset.galleryBound = "true";
      opener.addEventListener("click", () => {
        const indexValue = Number(opener.getAttribute("data-gallery-index"));
        open(Number.isFinite(indexValue) ? indexValue : 0);
      });
    });

    if (lightbox.dataset.galleryBound === "true") {
      return;
    }

    lightbox.dataset.galleryBound = "true";
    closeButton.addEventListener("click", close);
    prevButton.addEventListener("click", prev);
    nextButton.addEventListener("click", next);
    lightbox.addEventListener("click", (event) => {
      if (event.target === lightbox) {
        close();
      }
    });
    lightbox.addEventListener("keydown", (event) => {
      if (!lightbox.classList.contains("is-open")) {
        return;
      }

      if (event.key === "Escape") {
        close();
        return;
      }

      if (event.key === "ArrowLeft") {
        prev();
        return;
      }

      if (event.key === "ArrowRight") {
        next();
      }
    });
  };

  const initAuthForms = () => {
    const toggles = Array.from(document.querySelectorAll("[data-password-toggle]"));
    if (!toggles.length) {
      return;
    }

    toggles.forEach((toggle) => {
      if (!(toggle instanceof HTMLElement) || toggle.dataset.bound === "true") {
        return;
      }

      const inputId = toggle.getAttribute("data-password-toggle");
      if (!inputId) {
        return;
      }

      const input = document.getElementById(inputId);
      if (!(input instanceof HTMLInputElement)) {
        return;
      }

      toggle.dataset.bound = "true";
      toggle.addEventListener("click", () => {
        const shouldShow = input.type === "password";
        input.type = shouldShow ? "text" : "password";

        const icon = toggle.querySelector(".material-symbols-outlined");
        if (icon) {
          icon.textContent = shouldShow ? "visibility_off" : "visibility";
        }
      });
    });
  };

  const initFormValidation = () => {
    const jq = window.jQuery;
    if (!jq || !jq.validator || !jq.validator.unobtrusive) {
      return;
    }

    const forms = document.querySelectorAll("form");
    forms.forEach((form) => {
      const $form = jq(form);
      $form.removeData("validator");
      $form.removeData("unobtrusiveValidation");
      jq.validator.unobtrusive.parse(form);
    });
  };

  const softNavigate = async (url, pushState) => {
    if (state.navigating) {
      return;
    }

    const shell = document.querySelector("#app-shell");
    if (!shell) {
      window.location.assign(url);
      return;
    }

    state.navigating = true;
    document.documentElement.classList.add("soft-nav-loading");

    try {
      const response = await fetch(url, {
        credentials: "same-origin",
        headers: {
          "X-Requested-With": "XMLHttpRequest",
          "X-Soft-Navigation": "1",
        },
      });

      if (!response.ok) {
        window.location.assign(url);
        return;
      }

      const html = await response.text();
      const nextDocument = new DOMParser().parseFromString(html, "text/html");
      const nextShell = nextDocument.querySelector("#app-shell");

      if (!nextShell) {
        window.location.assign(url);
        return;
      }

      shell.innerHTML = nextShell.innerHTML;
      document.body.className = nextDocument.body.className;
      document.title = nextDocument.title;

      if (pushState) {
        window.history.pushState({ softNav: true }, "", url);
      }

      if (window.tailwind && typeof window.tailwind.refresh === "function") {
        window.tailwind.refresh();
      }

      window.scrollTo(0, 0);
      initPageEffects();
    } catch {
      window.location.assign(url);
    } finally {
      state.navigating = false;
      document.documentElement.classList.remove("soft-nav-loading");
    }
  };

  const handleSoftNavClick = (event) => {
    if (event.defaultPrevented || event.button !== 0) {
      return;
    }

    if (event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) {
      return;
    }

    const link = event.target.closest("a[data-soft-nav='true']");
    if (!link) {
      return;
    }

    if (link.target && link.target !== "_self") {
      return;
    }

    const href = link.getAttribute("href");
    if (!href || href.startsWith("#")) {
      return;
    }

    const nextUrl = new URL(link.href, window.location.href);
    if (nextUrl.origin !== window.location.origin) {
      return;
    }

    const currentUrl = new URL(window.location.href);
    if (nextUrl.pathname === currentUrl.pathname && nextUrl.search === currentUrl.search) {
      return;
    }

    event.preventDefault();
    softNavigate(nextUrl.href, true);
  };

  const bindSoftNavigation = () => {
    if (state.softNavBound) {
      return;
    }

    document.addEventListener("click", handleSoftNavClick);
    window.addEventListener("popstate", () => {
      softNavigate(window.location.href, false);
    });

    window.history.replaceState({ softNav: true }, "", window.location.href);
    state.softNavBound = true;
  };

  document.addEventListener("DOMContentLoaded", () => {
    initPageEffects();
    bindSoftNavigation();
  });
})();
