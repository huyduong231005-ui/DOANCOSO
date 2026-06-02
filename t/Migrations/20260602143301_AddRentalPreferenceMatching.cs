using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace t.Migrations
{
    /// <inheritdoc />
    public partial class AddRentalPreferenceMatching : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ChoPhepThuCung",
                table: "CanHo",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "HuongNha",
                table: "CanHo",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LoaiChoDauXe",
                table: "CanHo",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MucNoiThat",
                table: "CanHo",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "NgayCoTheVaoO",
                table: "CanHo",
                type: "date",
                nullable: false,
                defaultValueSql: "CURRENT_DATE");

            migrationBuilder.AddColumn<int>(
                name: "SoTang",
                table: "CanHo",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SoThangThueToiDa",
                table: "CanHo",
                type: "integer",
                nullable: false,
                defaultValue: 12);

            migrationBuilder.AddColumn<int>(
                name: "SoThangThueToiThieu",
                table: "CanHo",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql(
                """
                UPDATE "CanHo" AS c
                SET "MucNoiThat" = 2
                WHERE EXISTS (
                    SELECT 1
                    FROM "CanHo_TienIch" AS ct
                    INNER JOIN "TienIch" AS t ON t."Id" = ct."TienIchId"
                    WHERE ct."CanHoId" = c."Id"
                      AND t."Slug" = 'furniture'
                      AND t."DaXoa" = FALSE
                );

                UPDATE "CanHo" AS c
                SET "LoaiChoDauXe" = 1
                WHERE EXISTS (
                    SELECT 1
                    FROM "CanHo_TienIch" AS ct
                    INNER JOIN "TienIch" AS t ON t."Id" = ct."TienIchId"
                    WHERE ct."CanHoId" = c."Id"
                      AND t."Slug" = 'parking'
                      AND t."DaXoa" = FALSE
                );

                UPDATE "CanHo" AS c
                SET
                    "SoThangThueToiThieu" = CASE
                        WHEN dm."Slug" IN ('nha-tro', 'chung-cu-mini') THEN 3
                        WHEN dm."Slug" IN ('can-ho-cao-cap', 'penthouse') THEN 6
                        WHEN dm."Slug" IN ('nha-nguyen-can', 'biet-thu') THEN 12
                        ELSE "SoThangThueToiThieu"
                    END,
                    "SoThangThueToiDa" = CASE
                        WHEN dm."Slug" IN ('nha-tro', 'chung-cu-mini') THEN 12
                        WHEN dm."Slug" IN ('can-ho-cao-cap', 'penthouse') THEN 24
                        WHEN dm."Slug" IN ('nha-nguyen-can', 'biet-thu') THEN 36
                        ELSE "SoThangThueToiDa"
                    END
                FROM "DanhMuc" AS dm
                WHERE dm."Id" = c."DanhMucId"
                  AND dm."DaXoa" = FALSE;
                """);

            migrationBuilder.CreateTable(
                name: "HoSoNhuCauThue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NguoiDungId = table.Column<string>(type: "text", nullable: false),
                    KhuVucId = table.Column<int>(type: "integer", nullable: true),
                    GiaToiThieu = table.Column<decimal>(type: "numeric(18,0)", nullable: true),
                    GiaToiDa = table.Column<decimal>(type: "numeric(18,0)", nullable: true),
                    DienTichToiThieu = table.Column<double>(type: "double precision", nullable: true),
                    DienTichToiDa = table.Column<double>(type: "double precision", nullable: true),
                    SoPhongNguToiThieu = table.Column<int>(type: "integer", nullable: true),
                    DiaChiUuTien = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ViDoUuTien = table.Column<double>(type: "double precision", nullable: true),
                    KinhDoUuTien = table.Column<double>(type: "double precision", nullable: true),
                    BanKinhToiDaKm = table.Column<double>(type: "double precision", nullable: true),
                    MucNoiThat = table.Column<int>(type: "integer", nullable: true),
                    CanChoPhepThuCung = table.Column<bool>(type: "boolean", nullable: true),
                    LoaiChoDauXe = table.Column<int>(type: "integer", nullable: true),
                    NgayDuKienVaoO = table.Column<DateOnly>(type: "date", nullable: true),
                    TangToiThieu = table.Column<int>(type: "integer", nullable: true),
                    TangToiDa = table.Column<int>(type: "integer", nullable: true),
                    HuongNha = table.Column<int>(type: "integer", nullable: true),
                    SoThangThueToiThieu = table.Column<int>(type: "integer", nullable: true),
                    SoThangThueToiDa = table.Column<int>(type: "integer", nullable: true),
                    BatBuocKhuVuc = table.Column<bool>(type: "boolean", nullable: false),
                    BatBuocKhoangGia = table.Column<bool>(type: "boolean", nullable: false),
                    BatBuocDienTich = table.Column<bool>(type: "boolean", nullable: false),
                    BatBuocPhongNgu = table.Column<bool>(type: "boolean", nullable: false),
                    BatBuocLoaiHinh = table.Column<bool>(type: "boolean", nullable: false),
                    BatBuocKhoangCach = table.Column<bool>(type: "boolean", nullable: false),
                    BatBuocNoiThat = table.Column<bool>(type: "boolean", nullable: false),
                    BatBuocThuCung = table.Column<bool>(type: "boolean", nullable: false),
                    BatBuocDauXe = table.Column<bool>(type: "boolean", nullable: false),
                    BatBuocNgayVaoO = table.Column<bool>(type: "boolean", nullable: false),
                    BatBuocKhoangTang = table.Column<bool>(type: "boolean", nullable: false),
                    BatBuocHuongNha = table.Column<bool>(type: "boolean", nullable: false),
                    BatBuocThoiHanThue = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_HoSoNhuCauThue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HoSoNhuCauThue_KhuVuc_KhuVucId",
                        column: x => x.KhuVucId,
                        principalTable: "KhuVuc",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HoSoNhuCauThue_NguoiDung_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HoSoNhuCauThue_DanhMuc",
                columns: table => new
                {
                    HoSoNhuCauThueId = table.Column<int>(type: "integer", nullable: false),
                    DanhMucId = table.Column<int>(type: "integer", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoSoNhuCauThue_DanhMuc", x => new { x.HoSoNhuCauThueId, x.DanhMucId });
                    table.ForeignKey(
                        name: "FK_HoSoNhuCauThue_DanhMuc_DanhMuc_DanhMucId",
                        column: x => x.DanhMucId,
                        principalTable: "DanhMuc",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HoSoNhuCauThue_DanhMuc_HoSoNhuCauThue_HoSoNhuCauThueId",
                        column: x => x.HoSoNhuCauThueId,
                        principalTable: "HoSoNhuCauThue",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HoSoNhuCauThue_TienIch",
                columns: table => new
                {
                    HoSoNhuCauThueId = table.Column<int>(type: "integer", nullable: false),
                    TienIchId = table.Column<int>(type: "integer", nullable: false),
                    BatBuoc = table.Column<bool>(type: "boolean", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoSoNhuCauThue_TienIch", x => new { x.HoSoNhuCauThueId, x.TienIchId });
                    table.ForeignKey(
                        name: "FK_HoSoNhuCauThue_TienIch_HoSoNhuCauThue_HoSoNhuCauThueId",
                        column: x => x.HoSoNhuCauThueId,
                        principalTable: "HoSoNhuCauThue",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HoSoNhuCauThue_TienIch_TienIch_TienIchId",
                        column: x => x.TienIchId,
                        principalTable: "TienIch",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CanHo_LoaiChoDauXe",
                table: "CanHo",
                column: "LoaiChoDauXe");

            migrationBuilder.CreateIndex(
                name: "IX_CanHo_MucNoiThat",
                table: "CanHo",
                column: "MucNoiThat");

            migrationBuilder.CreateIndex(
                name: "IX_CanHo_NgayCoTheVaoO",
                table: "CanHo",
                column: "NgayCoTheVaoO");

            migrationBuilder.CreateIndex(
                name: "IX_HoSoNhuCauThue_KhuVucId",
                table: "HoSoNhuCauThue",
                column: "KhuVucId");

            migrationBuilder.CreateIndex(
                name: "IX_HoSoNhuCauThue_NguoiDungId",
                table: "HoSoNhuCauThue",
                column: "NguoiDungId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HoSoNhuCauThue_DanhMuc_DanhMucId",
                table: "HoSoNhuCauThue_DanhMuc",
                column: "DanhMucId");

            migrationBuilder.CreateIndex(
                name: "IX_HoSoNhuCauThue_TienIch_TienIchId",
                table: "HoSoNhuCauThue_TienIch",
                column: "TienIchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HoSoNhuCauThue_DanhMuc");

            migrationBuilder.DropTable(
                name: "HoSoNhuCauThue_TienIch");

            migrationBuilder.DropTable(
                name: "HoSoNhuCauThue");

            migrationBuilder.DropIndex(
                name: "IX_CanHo_LoaiChoDauXe",
                table: "CanHo");

            migrationBuilder.DropIndex(
                name: "IX_CanHo_MucNoiThat",
                table: "CanHo");

            migrationBuilder.DropIndex(
                name: "IX_CanHo_NgayCoTheVaoO",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "ChoPhepThuCung",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "HuongNha",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "LoaiChoDauXe",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "MucNoiThat",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "NgayCoTheVaoO",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "SoTang",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "SoThangThueToiDa",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "SoThangThueToiThieu",
                table: "CanHo");
        }
    }
}
