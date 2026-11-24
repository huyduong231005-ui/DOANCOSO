using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace t.Migrations
{
    /// <inheritdoc />
    public partial class Tuan7KhoaNguoiDungVaiTro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "VaiTro",
                newName: "VaiTroId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "NguoiDung",
                newName: "NguoiDungId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "VaiTroId",
                table: "VaiTro",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "NguoiDungId",
                table: "NguoiDung",
                newName: "Id");
        }
    }
}
