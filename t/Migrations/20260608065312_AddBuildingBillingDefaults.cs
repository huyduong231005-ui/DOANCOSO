using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace t.Migrations
{
    /// <inheritdoc />
    public partial class AddBuildingBillingDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NgayChotKyMacDinh",
                table: "ToaNha",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "PhanTramPhiTreMacDinh",
                table: "ToaNha",
                type: "integer",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<int>(
                name: "SoNgayTreHanMacDinh",
                table: "ToaNha",
                type: "integer",
                nullable: false,
                defaultValue: 7);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NgayChotKyMacDinh",
                table: "ToaNha");

            migrationBuilder.DropColumn(
                name: "PhanTramPhiTreMacDinh",
                table: "ToaNha");

            migrationBuilder.DropColumn(
                name: "SoNgayTreHanMacDinh",
                table: "ToaNha");
        }
    }
}
