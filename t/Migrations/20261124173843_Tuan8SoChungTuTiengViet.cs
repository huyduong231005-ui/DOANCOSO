using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace t.Migrations
{
    /// <summary>
    /// Số chứng từ dùng tiền tố tiếng Việt: YC- (yêu cầu sự cố), HD- (hoá đơn), PT- (phiếu thu).
    /// Phần sau tiền tố giữ nguyên nên số thứ tự vẫn chạy tiếp.
    /// </summary>
    public partial class Tuan8SoChungTuTiengViet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "YeuCauBaoTri" SET "SoYeuCau" = 'YC-' || substr("SoYeuCau", 4) WHERE "SoYeuCau" LIKE 'MR-%';
                UPDATE "HoaDon" SET "SoHoaDon" = 'HD-' || substr("SoHoaDon", 5) WHERE "SoHoaDon" LIKE 'INV-%';
                UPDATE "ThanhToan" SET "SoThanhToan" = 'PT-' || substr("SoThanhToan", 5) WHERE "SoThanhToan" LIKE 'PMT-%';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "YeuCauBaoTri" SET "SoYeuCau" = 'MR-' || substr("SoYeuCau", 4) WHERE "SoYeuCau" LIKE 'YC-%';
                UPDATE "HoaDon" SET "SoHoaDon" = 'INV-' || substr("SoHoaDon", 4) WHERE "SoHoaDon" LIKE 'HD-%';
                UPDATE "ThanhToan" SET "SoThanhToan" = 'PMT-' || substr("SoThanhToan", 4) WHERE "SoThanhToan" LIKE 'PT-%';
                """);
        }
    }
}
