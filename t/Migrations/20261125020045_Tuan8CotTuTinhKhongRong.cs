using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace t.Migrations
{
    /// <inheritdoc />
    public partial class Tuan8CotTuTinhKhongRong : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            // Cột tự tính luôn có giá trị: ghi NOT NULL cho đúng với mô hình (C# decimal không rỗng).
            migrationBuilder.Sql(@"ALTER TABLE ""HoaDon"" ALTER COLUMN ""TongTien"" SET NOT NULL, ALTER COLUMN ""ConNo"" SET NOT NULL;
ALTER TABLE ""ChiTietHoaDon"" ALTER COLUMN ""ThanhTien"" SET NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.Sql(@"ALTER TABLE ""HoaDon"" ALTER COLUMN ""TongTien"" DROP NOT NULL, ALTER COLUMN ""ConNo"" DROP NOT NULL;
ALTER TABLE ""ChiTietHoaDon"" ALTER COLUMN ""ThanhTien"" DROP NOT NULL;");
        }
    }
}
