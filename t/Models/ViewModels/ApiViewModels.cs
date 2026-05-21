namespace t.Models.ViewModels;

public class RentalsSearchResultViewModel
{
    public List<ApartmentListViewModel> Apartments { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class EmailExistsResponse
{
    public string Email { get; set; } = string.Empty;
    public bool Exists { get; set; }
}

public class DevResetTokenResponse
{
    public string Email { get; set; } = string.Empty;
    public string? Token { get; set; }
}
