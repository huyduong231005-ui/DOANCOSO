using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace t.Infrastructure;

public static class Slugify
{
    private static readonly Regex InvalidChars = new(@"[^a-z0-9-]", RegexOptions.Compiled);
    private static readonly Regex MultiDash = new(@"-{2,}", RegexOptions.Compiled);

    public static string Make(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var s = input.Trim().ToLowerInvariant()
            .Replace('đ', 'd');

        var normalized = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }
        s = sb.ToString().Normalize(NormalizationForm.FormC);

        s = s.Replace(' ', '-');
        s = InvalidChars.Replace(s, string.Empty);
        s = MultiDash.Replace(s, "-").Trim('-');

        return string.IsNullOrEmpty(s) ? "tin-dang" : s;
    }
}
