namespace t.Infrastructure.Formatting;

public static class RentalPriceFormatter
{
    public static string Format(decimal price) => price.ToString("N0");
}
