using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace t.Migrations
{
    /// <inheritdoc />
    public partial class Tuan9HoaDonNoiYeuCauSuCo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "YeuCauBaoTriId",
                table: "HoaDon",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_HoaDon_SuaChua_MoiYeuCau",
                table: "HoaDon",
                column: "YeuCauBaoTriId",
                unique: true,
                filter: "\"YeuCauBaoTriId\" IS NOT NULL AND \"TrangThaiHoaDon\" <> 5 AND \"DaXoa\" = false");

            migrationBuilder.AddForeignKey(
                name: "FK_HoaDon_YeuCauBaoTri_YeuCauBaoTriId",
                table: "HoaDon",
                column: "YeuCauBaoTriId",
                principalTable: "YeuCauBaoTri",
                principalColumn: "YeuCauBaoTriId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HoaDon_YeuCauBaoTri_YeuCauBaoTriId",
                table: "HoaDon");

            migrationBuilder.DropIndex(
                name: "UX_HoaDon_SuaChua_MoiYeuCau",
                table: "HoaDon");

            migrationBuilder.DropColumn(
                name: "YeuCauBaoTriId",
                table: "HoaDon");
        }
    }
}
