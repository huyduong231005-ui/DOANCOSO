(function () {
  'use strict';

  var photonApiUrl = 'https://photon.komoot.io/api/';
  var nominatimApiUrl = 'https://nominatim.openstreetmap.org/search';
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

    function appendVietnamHint(query) {
      return /vi(e|ệ)t\s*nam/i.test(query) ? query : query + ', Vietnam';
    }

    function isValidFeature(feature) {
      var coordinates = feature.geometry && feature.geometry.coordinates;
      return Array.isArray(coordinates) &&
        Number.isFinite(coordinates[0]) &&
        Number.isFinite(coordinates[1]);
    }

    async function fetchPhotonFeatures(query, signal) {
      var url = new URL(photonApiUrl);
      url.searchParams.set('q', appendVietnamHint(query));
      url.searchParams.set('limit', '5');
      url.searchParams.set('lat', '10.7769');
      url.searchParams.set('lon', '106.7009');
      var response = await fetch(url, { signal: signal });
      if (!response.ok) throw new Error('Photon request failed.');
      var payload = await response.json();
      return (payload.features || []).filter(isValidFeature);
    }

    function mapNominatimResult(result) {
      var lat = Number.parseFloat(result.lat);
      var lng = Number.parseFloat(result.lon);
      var address = result.address || {};
      var road = address.road || address.pedestrian || address.suburb || address.neighbourhood;
      var primary = [address.house_number, road].filter(Boolean).join(' ') ||
        result.name ||
        road ||
        result.display_name;
      return {
        geometry: { coordinates: [lng, lat] },
        properties: {
          label: result.display_name,
          name: primary,
          street: road,
          city: address.city || address.town || address.state
        }
      };
    }

    async function fetchNominatimFeatures(query, signal) {
      var url = new URL(nominatimApiUrl);
      url.searchParams.set('format', 'jsonv2');
      url.searchParams.set('q', appendVietnamHint(query));
      url.searchParams.set('limit', '5');
      url.searchParams.set('countrycodes', 'vn');
      url.searchParams.set('addressdetails', '1');
      url.searchParams.set('accept-language', 'vi,en');
      var response = await fetch(url, { signal: signal });
      if (!response.ok) throw new Error('Nominatim request failed.');
      var payload = await response.json();
      return (payload || []).map(mapNominatimResult).filter(isValidFeature);
    }

    async function fetchAddressFeatures(query, signal) {
      var photonError = null;
      try {
        var photonFeatures = await fetchPhotonFeatures(query, signal);
        if (photonFeatures.length > 0) return photonFeatures;
      } catch (error) {
        photonError = error;
      }

      try {
        return await fetchNominatimFeatures(query, signal);
      } catch (error) {
        if (error.name === 'AbortError') throw error;
        if (photonError) throw photonError;
        throw error;
      }
    }

    function featureLabel(feature) {
      var properties = feature.properties || {};
      return properties.label || properties.name || properties.street || properties.city || 'Địa điểm đã chọn';
    }

    function setCoordinateValues(lng, lat) {
      latitudeInput.value = Number(lat).toFixed(6);
      longitudeInput.value = Number(lng).toFixed(6);
    }

    function clearPosition() {
      latitudeInput.value = '';
      longitudeInput.value = '';
    }

    var form = addressInput.form;
    var maxDistanceInput = form?.querySelector('[name="maxDistanceKm"]');
    var resolvingSubmit = false;

    async function resolveAddressBeforeSubmit(event) {
      if (resolvingSubmit) {
        resolvingSubmit = false;
        return;
      }

      var query = addressInput.value.trim();
      var hasDistance = maxDistanceInput && Number.parseFloat(maxDistanceInput.value) > 0;
      var hasCoordinates = latitudeInput.value && longitudeInput.value;
      if (!query || !hasDistance || hasCoordinates) {
        return;
      }

      event.preventDefault();
      statusElement.textContent = 'Đang xác định tọa độ địa điểm...';
      try {
        var features = await fetchAddressFeatures(query);
        if (features.length === 0) {
          statusElement.textContent = 'Không xác định được tọa độ. Hãy nhập thêm quận/thành phố hoặc chọn vị trí trên bản đồ.';
          return;
        }

        var coordinates = features[0].geometry.coordinates;
        setCoordinateValues(coordinates[0], coordinates[1]);
        addressInput.value = featureLabel(features[0]);
        statusElement.textContent = 'Đã xác định tọa độ, đang áp dụng bộ lọc.';
        resolvingSubmit = true;
        form.requestSubmit(event.submitter || undefined);
      } catch (error) {
        statusElement.textContent = 'Không thể xác định tọa độ lúc này. Hãy chọn vị trí trên bản đồ hoặc thử lại sau.';
      }
    }

    if (form) {
      form.addEventListener('submit', resolveAddressBeforeSubmit);
    }

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
        setCoordinateValues(position.lng, position.lat);
        marker.setLngLat([position.lng, position.lat]);
        map.easeTo({ center: [position.lng, position.lat] });
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
        clearPosition();
        var query = addressInput.value.trim();
        if (query.length < 3) {
          clearSuggestions('');
          return;
        }
        statusElement.textContent = 'Đang tìm địa chỉ...';
        timer = window.setTimeout(async function () {
          activeRequest = new AbortController();
          try {
            var features = await fetchAddressFeatures(query, activeRequest.signal);
            suggestionsElement.replaceChildren();
            features.forEach(function (feature) {
              var coordinates = feature.geometry.coordinates;
              var label = featureLabel(feature);
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
            statusElement.textContent = suggestionsElement.hidden ? 'Không tìm thấy địa chỉ phù hợp. Hãy nhập thêm quận/thành phố hoặc chọn vị trí trên bản đồ.' : 'Chọn một địa chỉ gợi ý.';
          } catch (error) {
            if (error.name === 'AbortError') return;
            clearSuggestions('Không thể tải gợi ý. Bạn vẫn có thể chọn vị trí trên bản đồ hoặc nhập địa điểm thủ công.');
          }
        }, 500);
      });
      return map;
    } catch (error) {
      mapElement.hidden = true;
      statusElement.textContent = 'Chưa tải được bản đồ; vẫn có thể lọc theo địa điểm đã nhập.';
      return null;
    }
  };
})();
