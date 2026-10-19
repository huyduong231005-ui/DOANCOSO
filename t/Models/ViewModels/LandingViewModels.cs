namespace t.Models.ViewModels;

public class LandingProjectVm
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public int BuildingCount { get; set; }
    public int ApartmentCount { get; set; }
}
