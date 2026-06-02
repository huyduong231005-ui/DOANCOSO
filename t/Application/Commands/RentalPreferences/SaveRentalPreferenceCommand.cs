using t.Models.ViewModels;

namespace t.Application.Commands.RentalPreferences;

public sealed record SaveRentalPreferenceCommand(
    string UserId,
    RentalPreferenceDraft Draft);

public sealed record SaveRentalPreferenceResult(
    bool Success,
    IReadOnlyList<string> Errors)
{
    public static SaveRentalPreferenceResult Ok() => new(true, Array.Empty<string>());
    public static SaveRentalPreferenceResult Invalid(params string[] errors) => new(false, errors);
}
