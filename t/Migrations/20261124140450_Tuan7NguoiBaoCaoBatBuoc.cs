using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace t.Migrations
{
    /// <inheritdoc />
    public partial class Tuan7NguoiBaoCaoBatBuoc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Dữ liệu mẫu cũ thiếu người báo: lấy người đại diện đang ở của căn (hoặc người cư trú bất kỳ).
            migrationBuilder.Sql("""
                UPDATE "YeuCauBaoTri" y SET "NguoiBaoCaoId" = (SELECT c."NguoiDungId" FROM "CuTru" c
                    WHERE c."CanHoId" = y."CanHoId" ORDER BY c."LaDaiDien" DESC, c."NgayChuyenDi" NULLS FIRST, c."CuTruId" LIMIT 1)
                WHERE y."NguoiBaoCaoId" IS NULL;
                -- Căn không có ai cư trú: coi như quản lý tạo thay (tài khoản quản lý hoặc quản trị đầu tiên).
                UPDATE "YeuCauBaoTri" y SET "NguoiBaoCaoId" = (SELECT nv."NguoiDungId" FROM "NguoiDung_VaiTro" nv
                    JOIN "VaiTro" v ON v."VaiTroId" = nv."VaiTroId" WHERE v."TenVaiTro" IN ('Manager', 'Admin')
                    ORDER BY v."TenVaiTro" DESC, nv."NguoiDungId" LIMIT 1)
                WHERE y."NguoiBaoCaoId" IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "NguoiBaoCaoId",
                table: "YeuCauBaoTri",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "NguoiBaoCaoId",
                table: "YeuCauBaoTri",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
