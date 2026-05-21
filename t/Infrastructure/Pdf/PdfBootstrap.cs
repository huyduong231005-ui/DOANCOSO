using QuestPDF.Infrastructure;

namespace t.Infrastructure.Pdf;

public static class PdfBootstrap
{
    private static bool _initialized;
    public static void Initialize()
    {
        if (_initialized) return;
        QuestPDF.Settings.License = LicenseType.Community;
        _initialized = true;
    }
}
