using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace t.Migrations
{
    /// <summary>
    /// Che mật khẩu đã băm và các tem bảo mật đã lỡ ghi vào nhật ký trước khi bộ ghi nhật ký biết ẩn chúng.
    /// Không đảo ngược được (giá trị cũ không được giữ lại ở đâu khác), nên Down để trống.
    /// </summary>
    public partial class AnGiaTriBiMatTrongNhatKy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "NhatKyHeThong"
                SET "GiaTriCu" = regexp_replace("GiaTriCu", '"(PasswordHash|SecurityStamp|ConcurrencyStamp)":"[^"]*"', '"\1":"(ẩn)"', 'g'),
                    "GiaTriMoi" = regexp_replace("GiaTriMoi", '"(PasswordHash|SecurityStamp|ConcurrencyStamp)":"[^"]*"', '"\1":"(ẩn)"', 'g')
                WHERE "GiaTriCu" ~ '"(PasswordHash|SecurityStamp|ConcurrencyStamp)":"'
                   OR "GiaTriMoi" ~ '"(PasswordHash|SecurityStamp|ConcurrencyStamp)":"';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
