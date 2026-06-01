(function () {
  const state = {
    slideshowTimer: null,
    showNextSlide: null,
    revealObserver: null,
    softNavBound: false,
    visibilityBound: false,
    navigating: false,
    scrollTopHandler: null,
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

  const initCard3DTilt = () => {
    const cards = document.querySelectorAll(".listing-card, .region-card, .feature-card, .mini-card");
    cards.forEach((card) => {
      if (!(card instanceof HTMLElement) || card.dataset.tiltBound === "true") {
        return;
      }
      card.dataset.tiltBound = "true";

      let translateY = -8;
      if (card.classList.contains("listing-card")) {
        translateY = -14;
      } else if (card.classList.contains("region-card")) {
        translateY = -6;
      } else if (card.classList.contains("mini-card")) {
        translateY = -6;
      } else if (card.classList.contains("feature-card")) {
        translateY = -6;
      }

      card.addEventListener("mousemove", (e) => {
        const rect = card.getBoundingClientRect();
        const x = e.clientX - rect.left;
        const y = e.clientY - rect.top;
        const centerX = rect.width / 2;
        const centerY = rect.height / 2;

        // Max rotation angles (degrees)
        const maxRotateX = 8;
        const maxRotateY = 8;

        const rotateX = (((y - centerY) / centerY) * -maxRotateX).toFixed(2);
        const rotateY = (((x - centerX) / centerX) * maxRotateY).toFixed(2);

        // Remove transition during mousemove to prevent lagging
        card.style.transition = "transform 0.08s ease-out, box-shadow 0.15s ease-out";
        card.style.transform = `perspective(1000px) rotateX(${rotateX}deg) rotateY(${rotateY}deg) translateY(${translateY}px) scale(1.02)`;
      });

      card.addEventListener("mouseleave", () => {
        // Restore stylesheet transition so it snaps back smoothly
        card.style.transition = "";
        card.style.transform = "";
      });
    });
  };

  const initMagneticScrollTop = () => {
    let btn = document.getElementById("back-to-top");
    if (!btn) {
      btn = document.createElement("button");
      btn.id = "back-to-top";
      btn.type = "button";
      btn.setAttribute("aria-label", "Cuộn lên đầu trang");
      btn.innerHTML = `
        <svg class="progress-ring" width="46" height="46">
          <circle class="progress-ring__circle-bg" stroke="#f1f5f9" stroke-width="3" fill="transparent" r="20" cx="23" cy="23"/>
          <circle class="progress-ring__circle" stroke="#fe6e60" stroke-width="3" fill="transparent" r="20" cx="23" cy="23"/>
        </svg>
        <span class="material-symbols-outlined icon">arrow_upward</span>
      `;
      document.body.appendChild(btn);
    }

    const circle = btn.querySelector(".progress-ring__circle");
    if (!circle) return;
    const radius = 20; // Circle radius
    const circumference = radius * 2 * Math.PI;

    circle.style.strokeDasharray = `${circumference} ${circumference}`;
    circle.style.strokeDashoffset = `${circumference}`;

    const setProgress = (percent) => {
      const offset = circumference - (percent / 100) * circumference;
      circle.style.strokeDashoffset = offset;
    };

    // 1. Progress scroll listener
    const handleScroll = () => {
      const docHeight = document.documentElement.scrollHeight - window.innerHeight;
      const scrollPercent = docHeight > 0 ? (window.scrollY / docHeight) * 100 : 0;
      setProgress(scrollPercent || 0);

      if (window.scrollY > 300) {
        btn.classList.add("is-visible");
      } else {
        btn.classList.remove("is-visible");
      }
    };

    if (state.scrollTopHandler !== null) {
      window.removeEventListener("scroll", state.scrollTopHandler);
    }
    state.scrollTopHandler = handleScroll;
    window.addEventListener("scroll", state.scrollTopHandler);
    handleScroll();

    // 2. Click listener
    btn.onclick = () => {
      window.scrollTo({ top: 0, behavior: "smooth" });
    };

    // 3. Magnetic effect
    btn.onmousemove = (e) => {
      const rect = btn.getBoundingClientRect();
      const mouseX = e.clientX - (rect.left + rect.width / 2);
      const mouseY = e.clientY - (rect.top + rect.height / 2);

      const maxMove = 10;
      const moveX = (mouseX / (rect.width / 2)) * maxMove;
      const moveY = (mouseY / (rect.height / 2)) * maxMove;

      btn.style.transform = `translate(${moveX}px, ${moveY}px) scale(1.06)`;
      btn.style.transition = "transform 0.05s ease-out";
    };

    btn.onmouseleave = () => {
      btn.style.transform = "";
      btn.style.transition = "transform 0.3s cubic-bezier(0.25, 1, 0.5, 1), opacity 0.3s ease, visibility 0.3s ease";
    };
  };

  const initPageEffects = () => {
    initHeroSlideshow();
    initReveal();
    initApartmentDetail();
    initAuthForms();
    initFormValidation();
    initCard3DTilt();
    initMagneticScrollTop();
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

  const getProgressBar = () => {
    let bar = document.getElementById("top-progress-bar");
    if (!bar) {
      bar = document.createElement("div");
      bar.id = "top-progress-bar";
      document.body.appendChild(bar);
    }
    return bar;
  };

  const startProgressBar = () => {
    const bar = getProgressBar();
    bar.style.transition = "none";
    bar.style.width = "0%";
    bar.style.opacity = "1";
    // Force reflow to register styles
    void bar.offsetWidth;
    bar.style.transition = "width 0.8s cubic-bezier(0.1, 0.8, 0.1, 1), opacity 0.2s ease-in-out";
    bar.style.width = "80%";
  };

  const finishProgressBar = () => {
    const bar = getProgressBar();
    bar.style.transition = "width 0.3s ease-out, opacity 0.2s ease-in-out";
    bar.style.width = "100%";
    setTimeout(() => {
      bar.style.opacity = "0";
    }, 250);
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
    startProgressBar();

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
      finishProgressBar();
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
