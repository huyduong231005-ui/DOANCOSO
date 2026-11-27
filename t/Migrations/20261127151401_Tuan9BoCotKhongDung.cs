using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace t.Migrations
{
    /// <inheritdoc />
    public partial class Tuan9BoCotKhongDung : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnhYeuCau_NguoiDung_NguoiTaiId",
                table: "AnhYeuCau");

            migrationBuilder.DropForeignKey(
                name: "FK_ToaNha_NguoiDung_QuanLyId",
                table: "ToaNha");

            migrationBuilder.DropIndex(
                name: "IX_ToaNha_QuanLyId",
                table: "ToaNha");

            migrationBuilder.DropIndex(
                name: "IX_AnhYeuCau_NguoiTaiId",
                table: "AnhYeuCau");

            migrationBuilder.DropColumn(
                name: "QuanLyId",
                table: "ToaNha");

            migrationBuilder.DropColumn(
                name: "NguoiTaiId",
                table: "AnhYeuCau");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QuanLyId",
                table: "ToaNha",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NguoiTaiId",
                table: "AnhYeuCau",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToaNha_QuanLyId",
                table: "ToaNha",
                column: "QuanLyId");

            migrationBuilder.CreateIndex(
                name: "IX_AnhYeuCau_NguoiTaiId",
                table: "AnhYeuCau",
                column: "NguoiTaiId");

            migrationBuilder.AddForeignKey(
                name: "FK_AnhYeuCau_NguoiDung_NguoiTaiId",
                table: "AnhYeuCau",
                column: "NguoiTaiId",
                principalTable: "NguoiDung",
                principalColumn: "NguoiDungId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ToaNha_NguoiDung_QuanLyId",
                table: "ToaNha",
                column: "QuanLyId",
                principalTable: "NguoiDung",
                principalColumn: "NguoiDungId",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
