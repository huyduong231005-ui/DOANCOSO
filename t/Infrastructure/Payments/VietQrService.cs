using System.Net.Http;
using Microsoft.Extensions.Configuration;

namespace t.Infrastructure.Payments;

/// <summary>
/// Generates a VietQR image URL using img.vietqr.io and fetches the bytes for embedding in PDFs.
/// See https://www.vietqr.io for the public spec — uses the "compact2" template with amount + addInfo.
/// </summary>
public class VietQrService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<VietQrService> _log;

    public VietQrService(IConfiguration config, IHttpClientFactory httpFactory, ILogger<VietQrService> log)
    {
        _config = config;
        _httpFactory = httpFactory;
        _log = log;
    }

    public BankInfo? GetBank()
    {
        var bin = _config["Payment:Bank:BankBin"];
        var acc = _config["Payment:Bank:AccountNumber"];
        var name = _config["Payment:Bank:AccountName"];
        if (string.IsNullOrWhiteSpace(bin) || string.IsNullOrWhiteSpace(acc)) return null;
        return new BankInfo
        {
            BankBin = bin,
            BankName = _config["Payment:Bank:BankName"] ?? "",
            AccountNumber = acc,
            AccountName = name ?? ""
        };
    }

    public string? BuildQrImageUrl(decimal amount, string? memo)
    {
        var bank = GetBank();
        if (bank == null) return null;
        var amt = ((long)Math.Round(amount)).ToString();
        var info = Uri.EscapeDataString(memo ?? "");
        var accName = Uri.EscapeDataString(bank.AccountName);
        return $"https://img.vietqr.io/image/{bank.BankBin}-{bank.AccountNumber}-compact2.png?amount={amt}&addInfo={info}&accountName={accName}";
    }

    public async Task<byte[]?> FetchQrAsync(decimal amount, string? memo, CancellationToken ct = default)
    {
        var url = BuildQrImageUrl(amount, memo);
        if (url == null) return null;
        try
        {
            using var client = _httpFactory.CreateClient("vietqr");
            client.Timeout = TimeSpan.FromSeconds(5);
            return await client.GetByteArrayAsync(url, ct);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to fetch VietQR image");
            return null;
        }
    }

    public class BankInfo
    {
        public string BankBin { get; set; } = "";
        public string BankName { get; set; } = "";
        public string AccountNumber { get; set; } = "";
        public string AccountName { get; set; } = "";
    }
}
