# Nearby Rentals Discovery Design

## Context

Luxe Haven already stores optional `Latitude` and `Longitude` values on apartments.
The apartment detail page displays the selected apartment on MapLibre with an
OpenFreeMap style. The public rentals list and `/api/rentals/search` endpoint
currently support region, price, area, category, amenity, and basic sort options.
The apartment detail page currently recommends three active apartments from the
same region, ordered by creation time.

This iteration improves discovery with distance-based ordering. It does not add
the split list-and-map experience reserved for a later iteration.

## Goals

- Recommend apartments nearest to the apartment currently being viewed.
- Add a simple `Gan ban` action on the rentals page.
- Request browser geolocation only after the user explicitly presses that action.
- Preserve all existing filters while ordering matching apartments from nearest
  to farthest.
- Keep the interface simple: do not expose a radius filter.
- Show distance labels where they help users understand the ordering.

## Non-Goals

- Do not add a map to the rentals list page.
- Do not add a radius selector or hide results beyond a distance threshold.
- Do not persist the user's coordinates to the database, cookies, local storage,
  or analytics. Do not add application logging for raw coordinates.
- Do not call an external routing service. Distances are straight-line distances,
  not travel times or road distances.
- Do not add a database geospatial extension in this iteration.

## User Experience

### Apartment Detail Recommendations

Rename the recommendation section from `Can ho tuong tu` to
`Can ho gan vi tri nay`.

When the current apartment has valid coordinates:

1. Select other active apartments.
2. Put apartments with valid coordinates first.
3. Sort apartments with coordinates by straight-line distance from the current
   apartment, nearest first.
4. Return the first three apartments.
5. Show a compact label on each recommended card, such as `Cach 850 m` or
   `Cach 2,4 km`.

When the current apartment has no valid coordinates, preserve the current
fallback: select active apartments in the same region, newest first. Distance
labels are omitted in the fallback state.

### Rentals List

Add one secondary action labeled `Gan ban` near the existing filters. The action
does not request location permission during page load.

When the user presses `Gan ban`:

1. Use the browser Geolocation API to request the current position.
2. If successful, navigate to the rentals page with the existing query
   parameters plus `latitude`, `longitude`, and `sort=distance_asc`.
3. Preserve existing region, price, area, category, and amenity filters.
4. Render all matching apartments. Do not apply a radius cutoff.
5. Put apartments with valid coordinates first, nearest to farthest.
6. Put apartments without valid coordinates after geocoded apartments using a
   stable fallback order: newest first, then highest apartment ID first.
7. Show a compact distance label on each card that has a calculable distance.

Pagination links preserve `latitude`, `longitude`, and `sort=distance_asc`.
While nearby mode is active, the filter form includes hidden coordinate inputs
so applying another filter also preserves nearby ordering.
The existing clear-filter action returns to the unfiltered rentals page and
therefore exits nearby mode.

The existing sort dropdown gains a selected `Gan ban` option only while nearby
mode is active. The page does not show raw coordinates to the user.

### Geolocation Errors

If geolocation is unsupported, denied, unavailable, or times out, keep the user
on the current results page and show a short Vietnamese message. Existing search
results remain usable.

## Request Contract

Extend the rentals page action, rentals query handler, and
`GET /api/rentals/search` endpoint with optional query parameters:

| Parameter | Type | Rule |
| --- | --- | --- |
| `latitude` | `double?` | Valid range is `-90` to `90`. |
| `longitude` | `double?` | Valid range is `-180` to `180`. |
| `sort` | `string?` | Existing values remain supported; add `distance_asc`. |

Distance sorting is active only when:

- `sort=distance_asc`;
- both coordinates are supplied; and
- both coordinates pass validation.

The API returns `400 Bad Request` when only one coordinate is supplied, when a
coordinate is non-finite, or when a coordinate is outside its valid range.

The MVC rentals page handles invalid coordinate input without an exception:
ignore nearby mode, render the normal list ordering, and show a short warning.
This distinction keeps the JSON API contract strict while keeping the
browser-facing page resilient to manually edited URLs.

Extend `ApartmentListViewModel` with optional `DistanceKm`. Extend
`SimilarApartmentViewModel` with optional `DistanceKm`. The UI formats these
values for display:

- below `1 km`: rounded meters, for example `Cach 850 m`;
- `1 km` or above: one decimal place, for example `Cach 2,4 km`.

## Distance Calculation

Use a shared server-side distance helper based on the Haversine formula. It
calculates straight-line kilometers from two coordinate pairs.

For list-page sorting, apply distance ordering in the query before paging so
pagination remains correct. Keep the query compatible with the existing
Entity Framework Core PostgreSQL and in-memory test providers. If provider
translation of the distance expression is not reliable, use a bounded,
documented two-phase query that keeps filtering in the database and calculates
distance before paging; do not page first and sort afterward.

For detail recommendations, calculate distance before taking the first three
results. Apartments without coordinates remain eligible only after apartments
with calculable distances. Break equal-distance ties by newest creation time,
then highest apartment ID. Order unlocated candidates by the same creation-time
and ID fallback.

## Components

### Server

- `HomeController.Rentals`: accept optional coordinates, validate the
  browser-facing request, and pass valid nearby mode inputs to the query handler.
- `RentalsApiController.Search`: accept optional coordinates and reject invalid
  coordinate combinations with `400`.
- `RentalsQueryHandler.SearchAsync`: preserve existing filtering and add optional
  distance ordering plus `DistanceKm` projection.
- `HomeController.ApartmentDetail`: replace same-region-only recommendations
  with nearest-first recommendations and retain the same-region fallback.
- View models: expose optional distance values without changing existing clients
  that ignore the new field.
- Shared distance utility: centralize validation, Haversine calculation, and
  display formatting where appropriate.

### Browser

- Add a rentals-page JavaScript initializer that binds once on initial load and
  after `luxe:page-loaded`, matching the existing soft-navigation pattern.
- Bind the `Gan ban` action to `navigator.geolocation.getCurrentPosition`.
- On success, use `URL` and `URLSearchParams` to preserve current filters, reset
  `page` to `1`, set valid invariant-coordinate values, set
  `sort=distance_asc`, and navigate.
- On error, show a short inline status or existing toast notification without
  replacing current results.

### Views

- Rentals page: add the nearby action, distance sort option, optional distance
  labels, and coordinate preservation in pagination.
- Apartment detail page: rename the section and show optional distance labels.

## Error Handling

- Treat database apartments with missing or invalid coordinates as unlocated.
- Never fail an entire rentals request because one listing has invalid
  coordinates.
- Do not request browser geolocation automatically.
- Do not send browser coordinates anywhere until the user presses `Gan ban`.
- Keep existing behavior when no nearby parameters are provided.

## Testing

Add focused automated coverage:

- Distance utility tests for validation and known approximate distances.
- Rentals API tests for nearest-first ordering with existing filters.
- Rentals API tests for missing coordinate pair and out-of-range values returning
  `400`.
- Query handler or integration tests proving unlocated apartments appear after
  located apartments in nearby mode.
- MVC rentals-page tests proving the `Gan ban` hook renders and pagination
  preserves nearby parameters.
- MVC rentals-page tests proving an invalid manually edited nearby URL falls back
  without a server error.
- Apartment detail tests proving nearest-first recommendations when the current
  apartment is geocoded.
- Apartment detail tests proving the existing same-region fallback remains when
  the current apartment lacks coordinates.
- Static JavaScript checks or browser coverage proving geolocation runs only
  after a user click and successful navigation resets pagination.

## Acceptance Criteria

- Pressing `Gan ban` is the only action that triggers a geolocation permission
  request.
- A successful location request keeps active filters and displays all matching
  results nearest first without a radius control.
- Nearby mode survives pagination and is removed by clearing filters.
- Located listings display readable distances; unlocated listings remain
  available after located listings.
- Detail-page recommendations prefer the three nearest active apartments and
  display readable distances.
- Detail-page recommendations keep the existing same-region behavior when the
  current listing has no coordinates.
- Existing non-nearby rentals behavior remains unchanged.
