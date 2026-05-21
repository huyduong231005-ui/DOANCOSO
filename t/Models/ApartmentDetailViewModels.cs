namespace t.Models;

public sealed class ApartmentDetailViewModel
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string Area { get; init; } = string.Empty;
    public string Rating { get; init; } = string.Empty;
    public string ReviewSummary { get; init; } = string.Empty;
    public string Price { get; init; } = string.Empty;
    public string FeeNote { get; init; } = string.Empty;
    public string MainImage { get; init; } = string.Empty;
    public string SideImageOne { get; init; } = string.Empty;
    public string SideImageTwo { get; init; } = string.Empty;
    public string SideImageThree { get; init; } = string.Empty;
    public string MapImage { get; init; } = string.Empty;
    public string DescriptionOne { get; init; } = string.Empty;
    public string DescriptionTwo { get; init; } = string.Empty;
}

public sealed class SimilarApartmentViewModel
{
    public int Id { get; init; }
    public string Tag { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Meta { get; init; } = string.Empty;
    public string Price { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string Image { get; init; } = string.Empty;
}

public sealed class ApartmentDetailPageViewModel
{
    public ApartmentDetailViewModel Apartment { get; init; } = new();
    public IReadOnlyList<SimilarApartmentViewModel> SimilarApartments { get; init; } = Array.Empty<SimilarApartmentViewModel>();
}
