using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using t.Infrastructure.Payments;
using t.Models.Entities;

namespace t.Infrastructure.Pdf;

public class InvoicePdfGenerator
{
    private readonly VietQrService _qr;
    private readonly IConfiguration _config;

    public InvoicePdfGenerator(VietQrService qr, IConfiguration config)
    {
        _qr = qr;
        _config = config;
    }

    public byte[] Generate(Invoice invoice)
    {
        PdfBootstrap.Initialize();

        var companyName = _config["Company:Name"] ?? "LUXE HAVEN";
        var companyAddress = _config["Company:Address"] ?? "";
        var companyPhone = _config["Company:Phone"] ?? "";

        // Only render QR if invoice still has a balance and we have bank info.
        byte[]? qrImage = null;
        var bank = _qr.GetBank();
        if (invoice.Balance > 0 && bank != null)
        {
            var memo = $"TT {invoice.InvoiceNumber}";
            // Synchronous fetch for PDF rendering. Short timeout via service.
            qrImage = _qr.FetchQrAsync(invoice.Balance, memo).GetAwaiter().GetResult();
        }

        var doc = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(10).FontColor("#0f172a"));

                page.Header().Element(h => Header(h, invoice, companyName, companyAddress, companyPhone));
                page.Content().Element(b => Body(b, invoice, bank, qrImage));
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Luxe Haven · ").FontColor(Colors.Grey.Medium);
                    t.Span("Trang ");
                    t.CurrentPageNumber();
                    t.Span(" / ");
                    t.TotalPages();
                });
            });
        });

        return doc.GeneratePdf();
    }

    private static void Header(IContainer c, Invoice inv, string companyName, string companyAddress, string companyPhone)
    {
        c.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(companyName).FontSize(18).Bold().FontColor("#001e40");
                if (!string.IsNullOrWhiteSpace(companyAddress))
                    col.Item().Text(companyAddress).FontColor(Colors.Grey.Medium).FontSize(9);
                if (!string.IsNullOrWhiteSpace(companyPhone))
                    col.Item().Text("ĐT: " + companyPhone).FontColor(Colors.Grey.Medium).FontSize(9);
            });
            row.ConstantItem(220).AlignRight().Column(col =>
            {
                col.Item().AlignRight().Text("HOÁ ĐƠN").FontSize(20).Bold().FontColor("#001e40");
                col.Item().AlignRight().Text(inv.InvoiceNumber).FontSize(11).Bold();
                col.Item().AlignRight().Text($"Phát hành: {inv.IssueDate:dd/MM/yyyy}").FontColor(Colors.Grey.Medium);
                col.Item().AlignRight().Text($"Đến hạn: {inv.DueDate:dd/MM/yyyy}").FontColor(Colors.Grey.Medium);
            });
        });
    }

    private static void Body(IContainer c, Invoice inv, VietQrService.BankInfo? bank, byte[]? qrImage)
    {
        c.PaddingVertical(10).Column(col =>
        {
            // Status stamp
            var stamp = GetStamp(inv.Status);
            if (stamp.text != null)
            {
                col.Item().AlignRight().Background(stamp.bg!).Padding(4).Width(140)
                   .AlignCenter().Text(stamp.text).Bold().FontColor(stamp.fg!).FontSize(13);
            }

            col.Item().PaddingTop(10).Row(row =>
            {
                row.RelativeItem().Background("#f3f4f7").Padding(10).Column(b =>
                {
                    b.Item().Text("Khách thuê").FontColor(Colors.Grey.Medium).FontSize(9);
                    b.Item().Text(inv.Lease?.PrimaryTenant?.FullName ?? "—").Bold();
                    b.Item().Text(inv.Lease?.PrimaryTenant?.Email ?? "").FontSize(9);
                    b.Item().Text(inv.Lease?.PrimaryTenant?.Phone ?? "").FontSize(9);
                });
                row.ConstantItem(15);
                row.RelativeItem().Background("#f3f4f7").Padding(10).Column(b =>
                {
                    b.Item().Text("Căn hộ thuê").FontColor(Colors.Grey.Medium).FontSize(9);
                    b.Item().Text(inv.Lease?.Apartment?.Title ?? "—").Bold();
                    b.Item().Text(inv.Lease?.Apartment?.UnitCode ?? "").FontSize(9);
                    b.Item().Text(inv.Lease?.Apartment?.Address ?? "").FontSize(9);
                });
            });

            col.Item().PaddingTop(15).Text(t =>
            {
                t.Span("Hợp đồng: ").FontColor(Colors.Grey.Medium);
                t.Span(inv.Lease?.LeaseNumber ?? "").Bold();
                t.Span("    Loại HĐ: ").FontColor(Colors.Grey.Medium);
                t.Span(inv.Kind.ToString()).Bold();
                if (inv.BillingMonth > 0)
                {
                    t.Span("    Kỳ: ").FontColor(Colors.Grey.Medium);
                    t.Span($"{inv.BillingMonth / 100}/{inv.BillingMonth % 100:00}").Bold();
                }
            });

            col.Item().PaddingTop(10).Table(t =>
            {
                t.ColumnsDefinition(c2 =>
                {
                    c2.RelativeColumn(5);
                    c2.RelativeColumn(1);
                    c2.RelativeColumn(2);
                    c2.RelativeColumn(2);
                });
                t.Header(h =>
                {
                    h.Cell().Element(HeaderCell).Text("Mô tả");
                    h.Cell().Element(HeaderCell).AlignRight().Text("SL");
                    h.Cell().Element(HeaderCell).AlignRight().Text("Đơn giá");
                    h.Cell().Element(HeaderCell).AlignRight().Text("Thành tiền");
                });
                foreach (var it in inv.Items.OrderBy(x => x.SortOrder))
                {
                    t.Cell().Element(BodyCell).Text(it.Description);
                    t.Cell().Element(BodyCell).AlignRight().Text(it.Quantity.ToString("0.##"));
                    t.Cell().Element(BodyCell).AlignRight().Text(it.UnitPrice.ToString("N0"));
                    t.Cell().Element(BodyCell).AlignRight().Text(it.LineTotal.ToString("N0"));
                }
            });

            // Totals + QR side-by-side
            col.Item().PaddingTop(10).Row(row =>
            {
                // QR code (or bank info text only)
                row.RelativeItem().Column(qcol =>
                {
                    if (inv.Balance <= 0 || bank == null) return;
                    qcol.Item().Text("Thông tin chuyển khoản").Bold().FontSize(10).FontColor("#001e40");
                    qcol.Item().PaddingTop(2).Text($"Ngân hàng: {bank.BankName}").FontSize(9);
                    qcol.Item().Text($"STK: {bank.AccountNumber}").FontSize(9).Bold();
                    qcol.Item().Text($"Chủ TK: {bank.AccountName}").FontSize(9);
                    qcol.Item().Text($"Số tiền: {inv.Balance:N0} VNĐ").FontSize(9).Bold();
                    qcol.Item().Text($"Nội dung: TT {inv.InvoiceNumber}").FontSize(9).Italic();
                    if (qrImage != null)
                    {
                        qcol.Item().PaddingTop(5).Width(140).Image(qrImage);
                        qcol.Item().Text("Quét mã VietQR để chuyển khoản").FontSize(8).FontColor(Colors.Grey.Medium).Italic();
                    }
                });

                row.RelativeItem().AlignRight().Width(260).Column(t =>
                {
                    t.Item().Row(r => { r.RelativeItem().Text("Tạm tính"); r.ConstantItem(110).AlignRight().Text(inv.SubTotal.ToString("N0")); });
                    if (inv.Discount > 0) t.Item().Row(r => { r.RelativeItem().Text("Giảm giá"); r.ConstantItem(110).AlignRight().Text("-" + inv.Discount.ToString("N0")); });
                    if (inv.Tax > 0) t.Item().Row(r => { r.RelativeItem().Text("Thuế"); r.ConstantItem(110).AlignRight().Text(inv.Tax.ToString("N0")); });
                    if (inv.LateFee > 0) t.Item().Row(r => { r.RelativeItem().Text("Phí trễ"); r.ConstantItem(110).AlignRight().Text(inv.LateFee.ToString("N0")); });
                    t.Item().PaddingVertical(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                    t.Item().Row(r =>
                    {
                        r.RelativeItem().Text("TỔNG CỘNG").Bold();
                        r.ConstantItem(110).AlignRight().Text(inv.Total.ToString("N0") + " " + inv.Currency).Bold().FontColor("#001e40");
                    });
                    t.Item().Row(r => { r.RelativeItem().Text("Đã thanh toán").FontColor(Colors.Grey.Medium); r.ConstantItem(110).AlignRight().Text(inv.AmountPaid.ToString("N0")).FontColor(Colors.Grey.Medium); });
                    t.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Còn lại").Bold();
                        r.ConstantItem(110).AlignRight().Text(inv.Balance.ToString("N0") + " " + inv.Currency).Bold().FontColor(inv.Balance > 0 ? "#dc2626" : "#16a34a");
                    });
                });
            });

            if (!string.IsNullOrEmpty(inv.Note))
            {
                col.Item().PaddingTop(15).Background("#fffbeb").Padding(8).Column(t =>
                {
                    t.Item().Text("Ghi chú").FontColor(Colors.Grey.Medium).FontSize(9);
                    t.Item().Text(inv.Note);
                });
            }

            col.Item().PaddingTop(20).Text("Cảm ơn quý khách!").Italic().FontColor(Colors.Grey.Medium).AlignCenter();
        });
    }

    private static (string? text, string? bg, string? fg) GetStamp(InvoiceStatus s) => s switch
    {
        InvoiceStatus.Paid => ("ĐÃ THANH TOÁN", "#dcfce7", "#16a34a"),
        InvoiceStatus.Overdue => ("QUÁ HẠN", "#fee2e2", "#dc2626"),
        InvoiceStatus.Cancelled => ("ĐÃ HUỶ", "#f3f4f6", "#6b7280"),
        InvoiceStatus.Refunded => ("ĐÃ HOÀN", "#e0e7ff", "#4f46e5"),
        InvoiceStatus.PartiallyPaid => ("THANH TOÁN MỘT PHẦN", "#fef3c7", "#b45309"),
        _ => (null, null, null)
    };

    private static IContainer HeaderCell(IContainer c) =>
        c.DefaultTextStyle(t => t.SemiBold().FontSize(9))
         .PaddingVertical(6).PaddingHorizontal(4)
         .Background("#001e40").DefaultTextStyle(t => t.FontColor("#ffffff"));

    private static IContainer BodyCell(IContainer c) =>
        c.PaddingVertical(5).PaddingHorizontal(4)
         .BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3);
}
