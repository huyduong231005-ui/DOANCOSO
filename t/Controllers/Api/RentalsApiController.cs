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
        [FromQuery] RentalSearchRequest request)
    {
        request.PageSize = Math.Clamp(request.PageSize, 1, 50);
        var coordinates = GeoDistance.ValidatePair(request.Latitude, request.Longitude);
        if (!coordinates.IsValid)
            return BadRequest(new { error = coordinates.Error });

        var normalization = RentalPreferenceNormalizer.Normalize(request, strict: true);
        if (!normalization.IsValid)
            return BadRequest(new { error = normalization.Errors[0] });

        var model = await _rentalsQueryHandler.SearchAsync(request);

        return Ok(new RentalsSearchResultViewModel
        {
            Apartments = model.Apartments,
            TotalCount = model.TotalCount,
            Page = model.Page,
            PageSize = model.PageSize
        });
    }
}
