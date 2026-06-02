namespace t.Infrastructure.Formatting;

public static class RentalPriceFormatter
{
    public static string Format(decimal price)
    {
        if (price >= 1_000_000)
            return $"{price / 1_000_000:0.#}tr";

        return $"{price / 1_000:0}k";
    }
}
