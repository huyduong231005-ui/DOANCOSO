using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace t.Migrations
{
    /// <inheritdoc />
    public partial class SuCoMucUuTienKhoaNgoaiSla : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_YeuCauBaoTri_MucSla_MucDoUuTien",
                table: "YeuCauBaoTri",
                column: "MucDoUuTien",
                principalTable: "MucSla",
                principalColumn: "MucDoUuTien",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_YeuCauBaoTri_MucSla_MucDoUuTien",
                table: "YeuCauBaoTri");
        }
    }
}
