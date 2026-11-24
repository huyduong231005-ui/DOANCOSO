using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace t.Migrations
{
    /// <inheritdoc />
    public partial class Tuan7ChiSoPhiKhongXoaTheoCan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChiSoDichVu_CanHo_CanHoId",
                table: "ChiSoDichVu");

            migrationBuilder.DropForeignKey(
                name: "FK_PhiDinhKy_CanHo_CanHoId",
                table: "PhiDinhKy");

            migrationBuilder.AddForeignKey(
                name: "FK_ChiSoDichVu_CanHo_CanHoId",
                table: "ChiSoDichVu",
                column: "CanHoId",
                principalTable: "CanHo",
                principalColumn: "CanHoId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PhiDinhKy_CanHo_CanHoId",
                table: "PhiDinhKy",
                column: "CanHoId",
                principalTable: "CanHo",
                principalColumn: "CanHoId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChiSoDichVu_CanHo_CanHoId",
                table: "ChiSoDichVu");

            migrationBuilder.DropForeignKey(
                name: "FK_PhiDinhKy_CanHo_CanHoId",
                table: "PhiDinhKy");

            migrationBuilder.AddForeignKey(
                name: "FK_ChiSoDichVu_CanHo_CanHoId",
                table: "ChiSoDichVu",
                column: "CanHoId",
                principalTable: "CanHo",
                principalColumn: "CanHoId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PhiDinhKy_CanHo_CanHoId",
                table: "PhiDinhKy",
                column: "CanHoId",
                principalTable: "CanHo",
                principalColumn: "CanHoId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
