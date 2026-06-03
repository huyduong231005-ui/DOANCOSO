(function () {
  function queryFromFilter(form) {
    var params = new URLSearchParams(new FormData(form));
    params.delete('page');
    params.set('sort', 'match_desc');
    return params;
  }

  function populateSaveForm(filterForm, saveForm) {
    saveForm.querySelectorAll('[data-preference-copy]').forEach(function (input) {
      input.remove();
    });
    new FormData(filterForm).forEach(function (value, name) {
      var input = document.createElement('input');
      input.type = 'hidden';
      input.name = name;
      input.value = value;
      input.dataset.preferenceCopy = 'true';
      saveForm.appendChild(input);
    });
  }

  function syncLeaseDurationControls() {
    document.querySelectorAll('[data-lease-duration-control]').forEach(function (control) {
      var valueInput = control.querySelector('[data-lease-display-value]');
      var unitInput = control.querySelector('[data-lease-display-unit]');
      var targetInput = control.querySelector('[data-lease-months-target]');
      if (!valueInput || !unitInput || !targetInput) {
        return;
      }

      var value = Number.parseInt(valueInput.value, 10);
      if (!Number.isFinite(value) || value <= 0) {
        targetInput.value = '';
        return;
      }

      targetInput.value = String(unitInput.value === 'years' ? value * 12 : value);
    });
  }

  function bind() {
    var filterForm = document.querySelector('#rentals-filter-form');
    var drawer = document.querySelector('[data-rentals-advanced-drawer]');
    var openButtons = document.querySelectorAll('[data-rentals-filter-open]');
    var closeButton = document.querySelector('[data-rentals-filter-close]');
    var saveButton = document.querySelector('[data-preference-save]');
    var saveForm = document.querySelector('[data-preference-save-form]');
    if (!filterForm || !drawer || !saveButton || !saveForm || saveButton.dataset.bound === 'true') {
      return;
    }
    saveButton.dataset.bound = 'true';

    function initPreferenceMap() {
      if (!window.createLuxeAddressMap) return;
      window.createLuxeAddressMap({
        mapElement: document.querySelector('#preference-map'),
        addressInput: document.querySelector('[name="preferredAddress"]'),
        latitudeInput: document.querySelector('[name="preferredLatitude"]'),
        longitudeInput: document.querySelector('[name="preferredLongitude"]'),
        suggestionsElement: document.querySelector('#preference-address-suggestions'),
        statusElement: document.querySelector('#preference-address-status'),
        defaultPosition: { lat: 10.762622, lng: 106.660172 },
        suggestionClassName: 'preference-address-suggestion'
      });
    }

    openButtons.forEach(function (openButton) {
      openButton.addEventListener('click', function () {
        drawer.hidden = false;
        initPreferenceMap();
      });
    });
    closeButton?.addEventListener('click', function () {
      drawer.hidden = true;
    });
    filterForm.addEventListener('submit', syncLeaseDurationControls);

    function submitPreference() {
      syncLeaseDurationControls();
      populateSaveForm(filterForm, saveForm);
      var returnUrl = saveForm.querySelector('input[name="returnUrl"]');
      if (returnUrl) {
        var url = new URL(window.location.href);
        url.searchParams.delete('pendingPreferenceSave');
        returnUrl.value = url.pathname + url.search;
      }
      saveForm.submit();
    }

    saveButton.addEventListener('click', function () {
      if (saveButton.dataset.isAuthenticated !== 'true') {
        var returnUrl = new URL(window.location.pathname, window.location.origin);
        returnUrl.search = queryFromFilter(filterForm).toString();
        returnUrl.searchParams.set('pendingPreferenceSave', 'true');
        window.location.href = '/Home/Login?returnUrl=' + encodeURIComponent(returnUrl.pathname + returnUrl.search);
        return;
      }
      submitPreference();
    });

    var current = new URL(window.location.href);
    if (saveButton.dataset.isAuthenticated === 'true' &&
        current.searchParams.get('pendingPreferenceSave') === 'true') {
      submitPreference();
    }
  }

  document.addEventListener('DOMContentLoaded', bind);
  document.addEventListener('luxe:page-loaded', bind);
})();
