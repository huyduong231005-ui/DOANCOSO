using t.Models.ViewModels;

namespace t.Application.Commands.Listings;

public sealed class CreateListingCommand
{
    public required CreateApartmentViewModel Model { get; init; }
    public required string HostId { get; init; }
}

public sealed class CreateListingResult
{
    public bool Success { get; init; }
    public int ApartmentId { get; init; }
    public List<(string Key, string Message)> Errors { get; init; } = new();

    public static CreateListingResult Fail(params (string Key, string Message)[] errors)
        => new() { Success = false, Errors = errors.ToList() };

    public static CreateListingResult Fail(List<(string Key, string Message)> errors)
        => new() { Success = false, Errors = errors };

    public static CreateListingResult Ok(int apartmentId)
        => new() { Success = true, ApartmentId = apartmentId };
}
