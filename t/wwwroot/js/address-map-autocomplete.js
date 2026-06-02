(function () {
  'use strict';

  var photonApiUrl = 'https://photon.komoot.io/api/';
  var mapStyleUrl = 'https://tiles.openfreemap.org/styles/liberty';

  window.createLuxeAddressMap = async function (options) {
    var mapElement = options.mapElement;
    var addressInput = options.addressInput;
    var latitudeInput = options.latitudeInput;
    var longitudeInput = options.longitudeInput;
    var suggestionsElement = options.suggestionsElement;
    var statusElement = options.statusElement;
    var defaultPosition = options.defaultPosition || { lat: 10.762622, lng: 106.660172 };
    var suggestionClassName = options.suggestionClassName || 'posting-address-suggestion';
    if (!mapElement || !addressInput || !latitudeInput || !longitudeInput || !suggestionsElement || !statusElement) {
      return null;
    }
    if (mapElement.dataset.luxeAddressMapInitialized === 'true') {
      return null;
    }
    mapElement.dataset.luxeAddressMapInitialized = 'true';

    var initialPosition = {
      lat: Number.parseFloat(latitudeInput.value) || defaultPosition.lat,
      lng: Number.parseFloat(longitudeInput.value) || defaultPosition.lng
    };

    try {
      var maplibregl = await window.loadMapLibre();
      var map = new maplibregl.Map({
        container: mapElement,
        style: mapStyleUrl,
        center: [initialPosition.lng, initialPosition.lat],
        zoom: 14,
        scrollZoom: false
      });
      map.addControl(new maplibregl.NavigationControl(), 'top-right');
      var marker = new maplibregl.Marker({ draggable: true })
        .setLngLat([initialPosition.lng, initialPosition.lat])
        .addTo(map);

      function setPosition(position) {
        latitudeInput.value = Number(position.lat).toFixed(6);
        longitudeInput.value = Number(position.lng).toFixed(6);
        marker.setLngLat([position.lng, position.lat]);
        map.easeTo({ center: [position.lng, position.lat] });
      }

      if (!latitudeInput.value || !longitudeInput.value) {
        setPosition(initialPosition);
      }
      marker.on('dragend', function () {
        setPosition(marker.getLngLat());
      });
      map.on('click', function (event) {
        setPosition(event.lngLat);
      });

      var timer;
      var activeRequest;
      function clearSuggestions(message) {
        suggestionsElement.replaceChildren();
        suggestionsElement.hidden = true;
        addressInput.setAttribute('aria-expanded', 'false');
        statusElement.textContent = message || '';
      }
      addressInput.addEventListener('input', function () {
        window.clearTimeout(timer);
        if (activeRequest) activeRequest.abort();
        var query = addressInput.value.trim();
        if (query.length < 3) {
          clearSuggestions('');
          return;
        }
        statusElement.textContent = 'Đang tìm địa chỉ...';
        timer = window.setTimeout(async function () {
          activeRequest = new AbortController();
          var url = new URL(photonApiUrl);
          url.searchParams.set('q', query);
          url.searchParams.set('limit', '5');
          try {
            var response = await fetch(url, { signal: activeRequest.signal });
            if (!response.ok) throw new Error('Photon request failed.');
            var payload = await response.json();
            suggestionsElement.replaceChildren();
            (payload.features || []).filter(function (feature) {
              var coordinates = feature.geometry && feature.geometry.coordinates;
              return Array.isArray(coordinates) &&
                Number.isFinite(coordinates[0]) &&
                Number.isFinite(coordinates[1]);
            }).forEach(function (feature) {
              var coordinates = feature.geometry.coordinates;
              var properties = feature.properties || {};
              var label = properties.name || properties.street || properties.city || 'Địa điểm đã chọn';
              var button = document.createElement('button');
              button.type = 'button';
              button.className = suggestionClassName;
              button.textContent = label;
              button.addEventListener('click', function () {
                addressInput.value = label;
                setPosition({ lng: coordinates[0], lat: coordinates[1] });
                clearSuggestions('Đã chọn địa chỉ.');
              });
              suggestionsElement.appendChild(button);
            });
            suggestionsElement.hidden = suggestionsElement.children.length === 0;
            addressInput.setAttribute('aria-expanded', String(!suggestionsElement.hidden));
            statusElement.textContent = suggestionsElement.hidden ? 'Không tìm thấy địa chỉ phù hợp.' : 'Chọn một địa chỉ gợi ý.';
          } catch (error) {
            if (error.name === 'AbortError') return;
            clearSuggestions('Không thể tải gợi ý. Bạn vẫn có thể chọn vị trí trên bản đồ.');
          }
        }, 500);
      });
      return map;
    } catch (error) {
      mapElement.textContent = 'Không thể tải bản đồ. Bạn vẫn có thể nhập địa chỉ thủ công.';
      statusElement.textContent = 'Bản đồ chưa sẵn sàng.';
      return null;
    }
  };
})();
