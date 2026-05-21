using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using t.Models.Entities;

namespace t.Infrastructure.Pdf;

public class LeaseContractPdfGenerator
{
    private readonly IConfiguration _config;

    public LeaseContractPdfGenerator(IConfiguration config)
    {
        _config = config;
    }

    public byte[] Generate(Lease lease)
    {
        PdfBootstrap.Initialize();

        var companyName = _config["Company:Name"] ?? "Công ty Quản lý Luxe Haven";
        var companyAddress = _config["Company:Address"] ?? "TP. Hồ Chí Minh";
        var companyTaxId = _config["Company:TaxId"];
        var companyPhone = _config["Company:Phone"];

        var doc = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Margin(40);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(10).FontColor("#0f172a").LineHeight(1.4f));

                page.Header().AlignCenter().Column(col =>
                {
                    col.Item().AlignCenter().Text("CỘNG HOÀ XÃ HỘI CHỦ NGHĨA VIỆT NAM").Bold().FontSize(11);
                    col.Item().AlignCenter().Text("Độc lập – Tự do – Hạnh phúc").FontSize(10);
                    col.Item().PaddingTop(15).AlignCenter().Text("HỢP ĐỒNG THUÊ CĂN HỘ").Bold().FontSize(16).FontColor("#001e40");
                    col.Item().AlignCenter().Text($"Số: {lease.LeaseNumber}").FontColor(Colors.Grey.Medium);
                });

                page.Content().PaddingVertical(15).Column(col =>
                {
                    col.Item().PaddingTop(5).Text(t =>
                    {
                        t.Span("Hôm nay, ngày ").Bold();
                        t.Span($"{lease.StartDate:dd} tháng {lease.StartDate:MM} năm {lease.StartDate:yyyy}").Bold();
                        t.Span(", chúng tôi gồm các bên sau cùng nhau ký kết hợp đồng thuê căn hộ với các nội dung và điều khoản sau:");
                    });

                    col.Item().PaddingTop(10).Text("BÊN CHO THUÊ (Bên A):").Bold();
                    col.Item().Text(companyName);
                    col.Item().Text($"Địa chỉ: {companyAddress}");
                    if (!string.IsNullOrWhiteSpace(companyTaxId)) col.Item().Text($"Mã số thuế: {companyTaxId}");
                    if (!string.IsNullOrWhiteSpace(companyPhone)) col.Item().Text($"Điện thoại: {companyPhone}");

                    col.Item().PaddingTop(10).Text("BÊN THUÊ (Bên B):").Bold();
                    col.Item().Text($"Họ tên: {lease.PrimaryTenant.FullName}");
                    col.Item().Text($"Email: {lease.PrimaryTenant.Email}");
                    col.Item().Text($"Điện thoại: {lease.PrimaryTenant.Phone ?? "—"}");

                    if (lease.AdditionalTenants.Any())
                    {
                        col.Item().PaddingTop(8).Text("Người cùng thuê:").Bold();
                        foreach (var co in lease.AdditionalTenants)
                        {
                            var relation = string.IsNullOrWhiteSpace(co.Relationship) ? "" : $" — {co.Relationship}";
                            col.Item().Text($"• {co.Tenant.FullName} ({co.Tenant.Email}){relation}");
                        }
                    }

                    col.Item().PaddingTop(15).Text("ĐIỀU 1: ĐỐI TƯỢNG HỢP ĐỒNG").Bold().FontColor("#001e40");
                    col.Item().Text(t =>
                    {
                        t.Span("Bên A đồng ý cho Bên B thuê căn hộ ");
                        t.Span(lease.Apartment.Title).Bold();
                        if (!string.IsNullOrEmpty(lease.Apartment.UnitCode)) { t.Span(" (mã: "); t.Span(lease.Apartment.UnitCode).Bold(); t.Span(")"); }
                        t.Span(", địa chỉ: ");
                        t.Span(lease.Apartment.Address).Bold();
                        t.Span(", diện tích ");
                        t.Span($"{lease.Apartment.Area} m²").Bold();
                        t.Span(", gồm ");
                        t.Span($"{lease.Apartment.Bedrooms} phòng ngủ, {lease.Apartment.Bathrooms} phòng tắm").Bold();
                        t.Span(".");
                    });

                    col.Item().PaddingTop(10).Text("ĐIỀU 2: THỜI HẠN THUÊ").Bold().FontColor("#001e40");
                    col.Item().Text(t =>
                    {
                        t.Span("Thời hạn thuê tính từ ngày ");
                        t.Span(lease.StartDate.ToString("dd/MM/yyyy")).Bold();
                        t.Span(" đến hết ngày ");
                        t.Span(lease.EndDate.ToString("dd/MM/yyyy")).Bold();
                        t.Span($". Tổng thời gian: {(int)(lease.EndDate - lease.StartDate).TotalDays} ngày.");
                    });

                    col.Item().PaddingTop(10).Text("ĐIỀU 3: GIÁ THUÊ VÀ THANH TOÁN").Bold().FontColor("#001e40");
                    col.Item().Text(t =>
                    {
                        t.Span("Giá thuê: ");
                        t.Span($"{lease.MonthlyRent:N0} VNĐ").Bold();
                        t.Span("/tháng. Thanh toán vào ngày ");
                        t.Span(lease.BillingDay.ToString()).Bold();
                        t.Span(" hằng tháng. Phí trễ hạn: ");
                        t.Span($"{lease.LateFeePercent}%").Bold();
                        t.Span(" sau ");
                        t.Span($"{lease.LateFeeAfterDays} ngày").Bold();
                        t.Span(" ân hạn.");
                    });

                    col.Item().PaddingTop(10).Text("ĐIỀU 4: TIỀN ĐẶT CỌC").Bold().FontColor("#001e40");
                    col.Item().Text(t =>
                    {
                        t.Span("Bên B đặt cọc số tiền: ");
                        t.Span($"{lease.Deposit:N0} VNĐ").Bold();
                        t.Span(". Tiền cọc sẽ được hoàn lại sau khi kết thúc hợp đồng nếu không có thiệt hại tài sản hoặc nợ công.");
                    });

                    col.Item().PaddingTop(10).Text("ĐIỀU 5: TRÁCH NHIỆM CÁC BÊN").Bold().FontColor("#001e40");
                    col.Item().Text("• Bên A: bàn giao căn hộ đúng tình trạng đã thoả thuận; xử lý sự cố hạ tầng kịp thời.");
                    col.Item().Text("• Bên B: thanh toán đúng hạn; giữ gìn căn hộ; báo ngay khi có hư hỏng.");

                    if (!string.IsNullOrEmpty(lease.Notes))
                    {
                        col.Item().PaddingTop(10).Text("ĐIỀU KHOẢN BỔ SUNG").Bold().FontColor("#001e40");
                        col.Item().Text(lease.Notes);
                    }

                    col.Item().PaddingTop(25).Row(row =>
                    {
                        row.RelativeItem().AlignCenter().Column(s =>
                        {
                            s.Item().Text("ĐẠI DIỆN BÊN A").Bold();
                            s.Item().PaddingTop(40).Text("(Ký, ghi rõ họ tên)").Italic().FontColor(Colors.Grey.Medium).FontSize(9);
                        });
                        row.RelativeItem().AlignCenter().Column(s =>
                        {
                            s.Item().Text("BÊN B").Bold();
                            s.Item().PaddingTop(40).Text(lease.PrimaryTenant.FullName).Bold();
                            s.Item().Text("(Ký, ghi rõ họ tên)").Italic().FontColor(Colors.Grey.Medium).FontSize(9);
                        });
                    });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Hợp đồng số ");
                    t.Span(lease.LeaseNumber).Bold();
                    t.Span(" · Trang ");
                    t.CurrentPageNumber();
                    t.Span("/");
                    t.TotalPages();
                });
            });
        });

        return doc.GeneratePdf();
    }
}
