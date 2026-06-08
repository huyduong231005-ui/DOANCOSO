using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace t.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringCharge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PhiDinhKy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HopDongId = table.Column<int>(type: "integer", nullable: false),
                    MoTa = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    SoTien = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    ThuTu = table.Column<int>(type: "integer", nullable: false),
                    DangApDung = table.Column<bool>(type: "boolean", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false),
                    NgayXoa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXoa = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    PhienBan = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhiDinhKy", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhiDinhKy_HopDongThue_HopDongId",
                        column: x => x.HopDongId,
                        principalTable: "HopDongThue",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhiDinhKy_HopDongId",
                table: "PhiDinhKy",
                column: "HopDongId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhiDinhKy");
        }
    }
}
