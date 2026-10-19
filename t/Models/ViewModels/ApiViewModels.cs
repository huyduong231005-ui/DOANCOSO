namespace t.Models.ViewModels;

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
