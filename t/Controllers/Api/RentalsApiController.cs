using Microsoft.AspNetCore.Mvc;
using t.Application.Queries.Rentals;
using t.Infrastructure.Geo;
using t.Models.ViewModels;

namespace t.Controllers.Api;

[ApiController]
[Route("api/rentals")]
public class RentalsApiController : ControllerBase
{
    private readonly RentalsQueryHandler _rentalsQueryHandler;

    public RentalsApiController(RentalsQueryHandler rentalsQueryHandler)
    {
        _rentalsQueryHandler = rentalsQueryHandler;
    }

    [HttpGet("search")]
    public async Task<ActionResult<RentalsSearchResultViewModel>> Search(
        string? region, decimal? minPrice, decimal? maxPrice,
        double? minArea, double? maxArea,
        [FromQuery] List<int>? categoryIds,
        [FromQuery] List<int>? amenityIds,
        string? sort,
        int page = 1,
        int pageSize = 12,
        double? latitude = null,
        double? longitude = null)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);
        var coordinates = GeoDistance.ValidatePair(latitude, longitude);
        if (!coordinates.IsValid)
            return BadRequest(new { error = coordinates.Error });

        var model = await _rentalsQueryHandler.SearchAsync(
            region, minPrice, maxPrice,
            minArea, maxArea,
            categoryIds, amenityIds,
            sort, page, pageSize,
            latitude: latitude,
            longitude: longitude);

        return Ok(new RentalsSearchResultViewModel
        {
            Apartments = model.Apartments,
            TotalCount = model.TotalCount,
            Page = model.Page,
            PageSize = model.PageSize
        });
    }
}
