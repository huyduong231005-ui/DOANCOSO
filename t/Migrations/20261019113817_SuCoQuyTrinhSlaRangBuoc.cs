using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace t.Migrations
{
    /// <inheritdoc />
    public partial class SuCoQuyTrinhSlaRangBuoc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "CanHoId",
                table: "YeuCauBaoTri",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "DanhGia",
                table: "YeuCauBaoTri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HanXuLy",
                table: "YeuCauBaoTri",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NhanXet",
                table: "YeuCauBaoTri",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ToaNhaKhuVucChungId",
                table: "YeuCauBaoTri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ViTri",
                table: "YeuCauBaoTri",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AnhSuCo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    YeuCauBaoTriId = table.Column<int>(type: "integer", nullable: false),
                    DuongDan = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    GiaiDoan = table.Column<int>(type: "integer", nullable: false),
                    NguoiTaiLenId = table.Column<string>(type: "text", nullable: true),
                    NgayTaiLen = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnhSuCo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnhSuCo_NguoiDung_NguoiTaiLenId",
                        column: x => x.NguoiTaiLenId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AnhSuCo_YeuCauBaoTri_YeuCauBaoTriId",
                        column: x => x.YeuCauBaoTriId,
                        principalTable: "YeuCauBaoTri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LichSuTrangThaiSuCo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    YeuCauBaoTriId = table.Column<int>(type: "integer", nullable: false),
                    TuTrangThai = table.Column<int>(type: "integer", nullable: true),
                    SangTrangThai = table.Column<int>(type: "integer", nullable: false),
                    NguoiThucHienId = table.Column<string>(type: "text", nullable: true),
                    ThoiDiem = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GhiChu = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichSuTrangThaiSuCo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LichSuTrangThaiSuCo_NguoiDung_NguoiThucHienId",
                        column: x => x.NguoiThucHienId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LichSuTrangThaiSuCo_YeuCauBaoTri_YeuCauBaoTriId",
                        column: x => x.YeuCauBaoTriId,
                        principalTable: "YeuCauBaoTri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MucSla",
                columns: table => new
                {
                    MucDoUuTien = table.Column<int>(type: "integer", nullable: false),
                    PhutPhanHoi = table.Column<int>(type: "integer", nullable: false),
                    PhutHoanTat = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MucSla", x => x.MucDoUuTien);
                    table.CheckConstraint("CK_MucSla_ThoiHan", "\"PhutPhanHoi\" > 0 AND \"PhutHoanTat\" >= \"PhutPhanHoi\"");
                });

            migrationBuilder.InsertData(
                table: "MucSla",
                columns: new[] { "MucDoUuTien", "PhutHoanTat", "PhutPhanHoi" },
                values: new object[,]
                {
                    { 0, 4320, 720 },
                    { 1, 1440, 120 },
                    { 2, 480, 30 },
                    { 3, 120, 15 }
                });

            // Chuyển dữ liệu cũ trước khi xoá cột DanhSachAnh (chuỗi URL cách nhau dấu phẩy, vi phạm 1NF).
            migrationBuilder.Sql("""
                INSERT INTO "AnhSuCo" ("YeuCauBaoTriId", "DuongDan", "GiaiDoan", "NguoiTaiLenId", "NgayTaiLen")
                SELECT y."Id", trim(u), 0, y."NguoiBaoCaoId", y."NgayBaoCao"
                FROM "YeuCauBaoTri" y, unnest(string_to_array(y."DanhSachAnh", ',')) AS u
                WHERE coalesce(y."DanhSachAnh", '') <> '' AND trim(u) <> '';
                """);
            migrationBuilder.DropColumn(
                name: "DanhSachAnh",
                table: "YeuCauBaoTri");

            // Yêu cầu cũ: hạn SLA tính từ lúc báo theo mức ưu tiên, và lịch sử tối thiểu (tạo + trạng thái hiện tại).
            migrationBuilder.Sql("""
                UPDATE "YeuCauBaoTri" y SET "HanXuLy" = y."NgayBaoCao" + make_interval(mins => s."PhutHoanTat")
                FROM "MucSla" s WHERE s."MucDoUuTien" = y."MucDoUuTien" AND y."HanXuLy" IS NULL;

                INSERT INTO "LichSuTrangThaiSuCo" ("YeuCauBaoTriId", "TuTrangThai", "SangTrangThai", "NguoiThucHienId", "ThoiDiem", "GhiChu")
                SELECT "Id", NULL, 0, "NguoiBaoCaoId", "NgayBaoCao", 'Tạo yêu cầu' FROM "YeuCauBaoTri";

                INSERT INTO "LichSuTrangThaiSuCo" ("YeuCauBaoTriId", "TuTrangThai", "SangTrangThai", "NguoiThucHienId", "ThoiDiem", "GhiChu")
                SELECT "Id", 0, "TrangThai", NULL, coalesce("NgayDongLai", "NgayGiaiQuyet", "NgayTiepNhan", "NgayBaoCao"), 'Chuyển từ dữ liệu cũ'
                FROM "YeuCauBaoTri" WHERE "TrangThai" <> 0;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauBaoTri_HanXuLy",
                table: "YeuCauBaoTri",
                column: "HanXuLy");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauBaoTri_ToaNhaKhuVucChungId",
                table: "YeuCauBaoTri",
                column: "ToaNhaKhuVucChungId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_YeuCauBaoTri_ChiPhi",
                table: "YeuCauBaoTri",
                sql: "\"ChiPhiUocTinh\" >= 0 AND \"ChiPhiThucTe\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_YeuCauBaoTri_DanhGia",
                table: "YeuCauBaoTri",
                sql: "\"DanhGia\" IS NULL OR \"DanhGia\" BETWEEN 1 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "CK_YeuCauBaoTri_HanXuLy",
                table: "YeuCauBaoTri",
                sql: "\"HanXuLy\" IS NULL OR \"HanXuLy\" >= \"NgayBaoCao\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_YeuCauBaoTri_NoiXayRa",
                table: "YeuCauBaoTri",
                sql: "(\"CanHoId\" IS NOT NULL AND \"ToaNhaKhuVucChungId\" IS NULL) OR (\"CanHoId\" IS NULL AND \"ToaNhaKhuVucChungId\" IS NOT NULL AND \"ViTri\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ToaNha_DonGiaPhiQuanLy",
                table: "ToaNha",
                sql: "\"DonGiaPhiQuanLy\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ToaNha_NgayChotKy",
                table: "ToaNha",
                sql: "\"NgayChotKyMacDinh\" BETWEEN 1 AND 28");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ThanhToan_SoTien",
                table: "ThanhToan",
                sql: "\"SoTien\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PhiDinhKy_SoTien",
                table: "PhiDinhKy",
                sql: "\"SoTien\" >= 0");

            migrationBuilder.CreateIndex(
                name: "UX_HoaDon_PhiThang_MoiKy",
                table: "HoaDon",
                columns: new[] { "CanHoId", "ThangTinhPhi" },
                unique: true,
                filter: "\"LoaiHoaDon\" = 0 AND \"TrangThai\" <> 5 AND \"DaXoa\" = false");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HoaDon_ConLai",
                table: "HoaDon",
                sql: "\"ConLai\" >= 0 AND \"ConLai\" <= \"TongTien\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HoaDon_SoTien",
                table: "HoaDon",
                sql: "\"TongTien\" >= 0 AND \"DaThanhToan\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CuDan_NgayChuyenDi",
                table: "CuDan",
                sql: "\"NgayChuyenDi\" IS NULL OR \"NgayChuyenDi\" >= \"NgayVaoO\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChiSoDichVu_ChiSo",
                table: "ChiSoDichVu",
                sql: "\"ChiSoMoi\" >= \"ChiSoCu\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CanHo_DienTich",
                table: "CanHo",
                sql: "\"DienTich\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CanHo_SoPhongNgu",
                table: "CanHo",
                sql: "\"SoPhongNgu\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_AnhSuCo_NguoiTaiLenId",
                table: "AnhSuCo",
                column: "NguoiTaiLenId");

            migrationBuilder.CreateIndex(
                name: "IX_AnhSuCo_YeuCauBaoTriId",
                table: "AnhSuCo",
                column: "YeuCauBaoTriId");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuTrangThaiSuCo_NguoiThucHienId",
                table: "LichSuTrangThaiSuCo",
                column: "NguoiThucHienId");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuTrangThaiSuCo_YeuCauBaoTriId_ThoiDiem",
                table: "LichSuTrangThaiSuCo",
                columns: new[] { "YeuCauBaoTriId", "ThoiDiem" });

            migrationBuilder.AddForeignKey(
                name: "FK_YeuCauBaoTri_ToaNha_ToaNhaKhuVucChungId",
                table: "YeuCauBaoTri",
                column: "ToaNhaKhuVucChungId",
                principalTable: "ToaNha",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_YeuCauBaoTri_ToaNha_ToaNhaKhuVucChungId",
                table: "YeuCauBaoTri");

            migrationBuilder.DropTable(
                name: "AnhSuCo");

            migrationBuilder.DropTable(
                name: "LichSuTrangThaiSuCo");

            migrationBuilder.DropTable(
                name: "MucSla");

            migrationBuilder.DropIndex(
                name: "IX_YeuCauBaoTri_HanXuLy",
                table: "YeuCauBaoTri");

            migrationBuilder.DropIndex(
                name: "IX_YeuCauBaoTri_ToaNhaKhuVucChungId",
                table: "YeuCauBaoTri");

            migrationBuilder.DropCheckConstraint(
                name: "CK_YeuCauBaoTri_ChiPhi",
                table: "YeuCauBaoTri");

            migrationBuilder.DropCheckConstraint(
                name: "CK_YeuCauBaoTri_DanhGia",
                table: "YeuCauBaoTri");

            migrationBuilder.DropCheckConstraint(
                name: "CK_YeuCauBaoTri_HanXuLy",
                table: "YeuCauBaoTri");

            migrationBuilder.DropCheckConstraint(
                name: "CK_YeuCauBaoTri_NoiXayRa",
                table: "YeuCauBaoTri");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ToaNha_DonGiaPhiQuanLy",
                table: "ToaNha");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ToaNha_NgayChotKy",
                table: "ToaNha");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ThanhToan_SoTien",
                table: "ThanhToan");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PhiDinhKy_SoTien",
                table: "PhiDinhKy");

            migrationBuilder.DropIndex(
                name: "UX_HoaDon_PhiThang_MoiKy",
                table: "HoaDon");

            migrationBuilder.DropCheckConstraint(
                name: "CK_HoaDon_ConLai",
                table: "HoaDon");

            migrationBuilder.DropCheckConstraint(
                name: "CK_HoaDon_SoTien",
                table: "HoaDon");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CuDan_NgayChuyenDi",
                table: "CuDan");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ChiSoDichVu_ChiSo",
                table: "ChiSoDichVu");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CanHo_DienTich",
                table: "CanHo");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CanHo_SoPhongNgu",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "DanhGia",
                table: "YeuCauBaoTri");

            migrationBuilder.DropColumn(
                name: "HanXuLy",
                table: "YeuCauBaoTri");

            migrationBuilder.DropColumn(
                name: "NhanXet",
                table: "YeuCauBaoTri");

            migrationBuilder.DropColumn(
                name: "ToaNhaKhuVucChungId",
                table: "YeuCauBaoTri");

            migrationBuilder.DropColumn(
                name: "ViTri",
                table: "YeuCauBaoTri");

            migrationBuilder.AlterColumn<int>(
                name: "CanHoId",
                table: "YeuCauBaoTri",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DanhSachAnh",
                table: "YeuCauBaoTri",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }
    }
}
