using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace t.Migrations
{
    /// <inheritdoc />
    public partial class BqlKhuToaCanCuDan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CanHo_DanhMuc_DanhMucId",
                table: "CanHo");

            migrationBuilder.DropForeignKey(
                name: "FK_CanHo_DuAn_DuAnId",
                table: "CanHo");

            migrationBuilder.DropForeignKey(
                name: "FK_CanHo_KhuVuc_KhuVucId",
                table: "CanHo");

            migrationBuilder.DropForeignKey(
                name: "FK_CanHo_NguoiDung_ChuNhaId",
                table: "CanHo");

            migrationBuilder.DropForeignKey(
                name: "FK_CanHo_Tang_TangId",
                table: "CanHo");

            migrationBuilder.DropForeignKey(
                name: "FK_CanHo_ToaNha_ToaNhaId",
                table: "CanHo");

            migrationBuilder.DropForeignKey(
                name: "FK_ChiSoDichVu_HopDongThue_HopDongId",
                table: "ChiSoDichVu");

            migrationBuilder.DropForeignKey(
                name: "FK_HoaDon_HopDongThue_HopDongId",
                table: "HoaDon");

            migrationBuilder.DropForeignKey(
                name: "FK_PhiDinhKy_HopDongThue_HopDongId",
                table: "PhiDinhKy");

            migrationBuilder.DropForeignKey(
                name: "FK_YeuCauBaoTri_HopDongThue_HopDongId",
                table: "YeuCauBaoTri");

            // ── Chuyển dữ liệu: hợp đồng → căn hộ (chạy trước khi xoá bảng/cột cũ) ──

            // Hai hợp đồng nối tiếp trên cùng căn có thể trùng (loại, kỳ) chỉ số → giữ bản mới nhất
            // để không vỡ unique index (CanHoId, LoaiDichVuId, ThangTinhPhi).
            migrationBuilder.Sql(@"
DELETE FROM ""ChiSoDichVu"" r
USING ""HopDongThue"" l, ""ChiSoDichVu"" r2, ""HopDongThue"" l2
WHERE l.""Id"" = r.""HopDongId"" AND l2.""Id"" = r2.""HopDongId"" AND l.""CanHoId"" = l2.""CanHoId""
  AND r.""LoaiDichVuId"" = r2.""LoaiDichVuId"" AND r.""ThangTinhPhi"" = r2.""ThangTinhPhi"" AND r.""Id"" < r2.""Id"";

UPDATE ""HoaDon"" x SET ""HopDongId"" = l.""CanHoId"" FROM ""HopDongThue"" l WHERE l.""Id"" = x.""HopDongId"";
-- Unique index (HopDongId, LoaiDichVuId, ThangTinhPhi) còn hiệu lực: đổi qua số âm trước để id căn
-- không đụng id hợp đồng chưa kịp đổi.
UPDATE ""ChiSoDichVu"" x SET ""HopDongId"" = -l.""CanHoId"" FROM ""HopDongThue"" l WHERE l.""Id"" = x.""HopDongId"";
UPDATE ""ChiSoDichVu"" SET ""HopDongId"" = -""HopDongId"" WHERE ""HopDongId"" < 0;
UPDATE ""PhiDinhKy"" x SET ""HopDongId"" = l.""CanHoId"" FROM ""HopDongThue"" l WHERE l.""Id"" = x.""HopDongId"";

-- Hoá đơn tiền cọc (LoaiHoaDon = 1) không còn loại riêng → phát sinh (3).
UPDATE ""HoaDon"" SET ""LoaiHoaDon"" = 3 WHERE ""LoaiHoaDon"" = 1;
-- Tình trạng 'Đã đặt giữ' (1) nay là 'Chưa bàn giao' → coi như căn trống.
UPDATE ""CanHo"" SET ""TinhTrangThue"" = 0 WHERE ""TinhTrangThue"" = 1;

-- Người thuê chính / người ở cùng của hợp đồng còn hiệu lực (Active=1, Renewing=4) → cư dân của căn.
CREATE TEMP TABLE bql_cudan AS
SELECT l.""CanHoId"" AS can_ho, u.""Id"" AS uid, u.""HoTen"" AS ho_ten, u.""SoDienThoai"" AS sdt, u.""Email"" AS email,
       1 AS vai_tro, TRUE AS dai_dien, l.""NgayBatDau"" AS vao_o
FROM ""HopDongThue"" l JOIN ""NguoiDung"" u ON u.""Id"" = l.""NguoiThueChinhId""
WHERE l.""TrangThai"" IN (1, 4) AND NOT l.""DaXoa""
UNION ALL
SELECT l.""CanHoId"", u.""Id"", u.""HoTen"", u.""SoDienThoai"", u.""Email"", 2, FALSE, l.""NgayBatDau""
FROM ""HopDong_NguoiThue"" t
JOIN ""HopDongThue"" l ON l.""Id"" = t.""HopDongId""
JOIN ""NguoiDung"" u ON u.""Id"" = t.""NguoiThueId""
WHERE l.""TrangThai"" IN (1, 4) AND NOT l.""DaXoa"";

-- Căn chỉ là tin đăng (không thuộc toà, không có lịch sử) → bỏ; ảnh/đánh giá/yêu thích xoá theo cascade.
DELETE FROM ""CanHo"" c
WHERE c.""ToaNhaId"" IS NULL
  AND NOT EXISTS (SELECT 1 FROM ""HopDongThue"" x WHERE x.""CanHoId"" = c.""Id"")
  AND NOT EXISTS (SELECT 1 FROM ""YeuCauBaoTri"" x WHERE x.""CanHoId"" = c.""Id"")
  AND NOT EXISTS (SELECT 1 FROM ""HoaDon"" x WHERE x.""HopDongId"" = c.""Id"")
  AND NOT EXISTS (SELECT 1 FROM ""ChiSoDichVu"" x WHERE x.""HopDongId"" = c.""Id"")
  AND NOT EXISTS (SELECT 1 FROM ""PhiDinhKy"" x WHERE x.""HopDongId"" = c.""Id"");

-- Căn có lịch sử nhưng chưa thuộc toà → gom vào toà tạm 'Chưa phân toà' theo khu vực để giữ dữ liệu.
INSERT INTO ""ToaNha"" (""Ten"", ""Slug"", ""Ma"", ""KhuVucId"", ""DiaChi"", ""SoLuongTang"", ""TrangThai"",
                        ""NgayChotKyMacDinh"", ""SoNgayTreHanMacDinh"", ""PhanTramPhiTreMacDinh"", ""NgayTao"", ""DaXoa"")
SELECT DISTINCT 'Chưa phân toà – ' || k.""Ten"", 'chua-phan-toa-' || k.""Id"", 'CPT' || k.""Id"", k.""Id"", '', 1, 0, 1, 7, 5, now(), FALSE
FROM ""CanHo"" c JOIN ""KhuVuc"" k ON k.""Id"" = c.""KhuVucId""
WHERE c.""ToaNhaId"" IS NULL;
UPDATE ""CanHo"" c SET ""ToaNhaId"" = t.""Id""
FROM ""ToaNha"" t WHERE c.""ToaNhaId"" IS NULL AND t.""Slug"" = 'chua-phan-toa-' || c.""KhuVucId"";

-- Căn chưa gắn tầng → tầng theo số tầng cũ (tạo tầng nếu thiếu).
INSERT INTO ""Tang"" (""ToaNhaId"", ""SoTang"", ""NhanTang"", ""NgayTao"", ""DaXoa"")
SELECT DISTINCT c.""ToaNhaId"", COALESCE(c.""SoTang"", 1), 'Tầng ' || COALESCE(c.""SoTang"", 1), now(), FALSE
FROM ""CanHo"" c
WHERE c.""TangId"" IS NULL
  AND NOT EXISTS (SELECT 1 FROM ""Tang"" f WHERE f.""ToaNhaId"" = c.""ToaNhaId"" AND f.""SoTang"" = COALESCE(c.""SoTang"", 1));
UPDATE ""CanHo"" c SET ""TangId"" = f.""Id""
FROM ""Tang"" f WHERE c.""TangId"" IS NULL AND f.""ToaNhaId"" = c.""ToaNhaId"" AND f.""SoTang"" = COALESCE(c.""SoTang"", 1);

UPDATE ""CanHo"" SET ""MaCanHo"" = 'CH-' || ""Id"" WHERE ""MaCanHo"" IS NULL OR ""MaCanHo"" = '';
");

            migrationBuilder.DropTable(
                name: "AnhCanHo");

            migrationBuilder.DropTable(
                name: "AnhDuAn");

            migrationBuilder.DropTable(
                name: "CanHo_TienIch");

            migrationBuilder.DropTable(
                name: "DanhGia");

            migrationBuilder.DropTable(
                name: "GiaoDichDatCoc");

            migrationBuilder.DropTable(
                name: "HopDong_NguoiThue");

            migrationBuilder.DropTable(
                name: "HoSoNhuCauThue_DanhMuc");

            migrationBuilder.DropTable(
                name: "HoSoNhuCauThue_TienIch");

            migrationBuilder.DropTable(
                name: "LichXemNha");

            migrationBuilder.DropTable(
                name: "YeuThich");

            migrationBuilder.DropTable(
                name: "KiemKeCanHo");

            migrationBuilder.DropTable(
                name: "DanhMuc");

            migrationBuilder.DropTable(
                name: "HoSoNhuCauThue");

            migrationBuilder.DropTable(
                name: "TienIch");

            migrationBuilder.DropTable(
                name: "HopDongThue");

            migrationBuilder.DropIndex(
                name: "IX_YeuCauBaoTri_HopDongId",
                table: "YeuCauBaoTri");

            migrationBuilder.DropIndex(
                name: "IX_HoaDon_HopDongId",
                table: "HoaDon");

            migrationBuilder.DropIndex(
                name: "IX_DuAn_TrangThai",
                table: "DuAn");

            migrationBuilder.DropIndex(
                name: "IX_ChiSoDichVu_HopDongId_LoaiDichVuId_ThangTinhPhi",
                table: "ChiSoDichVu");

            migrationBuilder.DropIndex(
                name: "IX_CanHo_ChuNhaId",
                table: "CanHo");

            migrationBuilder.DropIndex(
                name: "IX_CanHo_DanhMucId",
                table: "CanHo");

            migrationBuilder.DropIndex(
                name: "IX_CanHo_DuAnId",
                table: "CanHo");

            migrationBuilder.DropIndex(
                name: "IX_CanHo_Gia",
                table: "CanHo");

            migrationBuilder.DropIndex(
                name: "IX_CanHo_KhuVucId",
                table: "CanHo");

            migrationBuilder.DropIndex(
                name: "IX_CanHo_LoaiChoDauXe",
                table: "CanHo");

            migrationBuilder.DropIndex(
                name: "IX_CanHo_MucNoiThat",
                table: "CanHo");

            migrationBuilder.DropIndex(
                name: "IX_CanHo_NgayCoTheVaoO",
                table: "CanHo");

            migrationBuilder.DropIndex(
                name: "IX_CanHo_NoiBat",
                table: "CanHo");

            migrationBuilder.DropIndex(
                name: "IX_CanHo_Slug",
                table: "CanHo");

            migrationBuilder.DropIndex(
                name: "IX_CanHo_ToaNhaId",
                table: "CanHo");

            migrationBuilder.DropIndex(
                name: "IX_CanHo_ToaNhaId_MaCanHo",
                table: "CanHo");

            migrationBuilder.DropIndex(
                name: "IX_CanHo_TrangThai",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "HopDongId",
                table: "YeuCauBaoTri");

            migrationBuilder.DropColumn(
                name: "DanhXungChuNha",
                table: "NguoiDung");

            migrationBuilder.DropColumn(
                name: "LaChuNha",
                table: "NguoiDung");

            migrationBuilder.DropColumn(
                name: "GiaTu",
                table: "DuAn");

            migrationBuilder.DropColumn(
                name: "MoTaDayDu",
                table: "DuAn");

            migrationBuilder.DropColumn(
                name: "TrangThai",
                table: "DuAn");

            migrationBuilder.DropColumn(
                name: "ChoPhepThuCung",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "ChuNhaId",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "DanhMucId",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "DiaChi",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "DuAnId",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "GhiChuKiemDuyet",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "GhiChuPhi",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "Gia",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "KhuVucId",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "KinhDo",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "LoaiChoDauXe",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "LuotXem",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "MoTaThem",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "MucNoiThat",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "NgayCoTheVaoO",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "NgayDuyet",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "NguoiDuyet",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "NoiBat",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "Slug",
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

            migrationBuilder.DropColumn(
                name: "TienDatCocMacDinh",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "TieuDe",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "TrangThai",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "ViDo",
                table: "CanHo");

            migrationBuilder.RenameColumn(
                name: "HopDongId",
                table: "PhiDinhKy",
                newName: "CanHoId");

            migrationBuilder.RenameIndex(
                name: "IX_PhiDinhKy_HopDongId",
                table: "PhiDinhKy",
                newName: "IX_PhiDinhKy_CanHoId");

            migrationBuilder.RenameColumn(
                name: "HopDongId",
                table: "HoaDon",
                newName: "CanHoId");

            migrationBuilder.RenameIndex(
                name: "IX_HoaDon_HopDongId_ThangTinhPhi_LoaiHoaDon",
                table: "HoaDon",
                newName: "IX_HoaDon_CanHoId_ThangTinhPhi_LoaiHoaDon");

            migrationBuilder.RenameColumn(
                name: "HopDongId",
                table: "ChiSoDichVu",
                newName: "CanHoId");

            migrationBuilder.AddColumn<decimal>(
                name: "DonGiaPhiQuanLy",
                table: "ToaNha",
                type: "numeric(18,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<int>(
                name: "ToaNhaId",
                table: "CanHo",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TangId",
                table: "CanHo",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MaCanHo",
                table: "CanHo",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "CuDan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CanHoId = table.Column<int>(type: "integer", nullable: false),
                    HoTen = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    SoDienThoai = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SoGiayTo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    VaiTro = table.Column<int>(type: "integer", nullable: false),
                    DaiDienCanHo = table.Column<bool>(type: "boolean", nullable: false),
                    NgayVaoO = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NgayChuyenDi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiDungId = table.Column<string>(type: "text", nullable: true),
                    GhiChu = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_CuDan", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CuDan_CanHo_CanHoId",
                        column: x => x.CanHoId,
                        principalTable: "CanHo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CuDan_NguoiDung_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChiSoDichVu_CanHoId_LoaiDichVuId_ThangTinhPhi",
                table: "ChiSoDichVu",
                columns: new[] { "CanHoId", "LoaiDichVuId", "ThangTinhPhi" },
                unique: true,
                filter: "\"DaXoa\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_CanHo_ToaNhaId_MaCanHo",
                table: "CanHo",
                columns: new[] { "ToaNhaId", "MaCanHo" },
                unique: true,
                filter: "\"DaXoa\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_CuDan_CanHoId",
                table: "CuDan",
                column: "CanHoId");

            migrationBuilder.CreateIndex(
                name: "IX_CuDan_NguoiDungId",
                table: "CuDan",
                column: "NguoiDungId");

            migrationBuilder.Sql(@"
INSERT INTO ""CuDan"" (""CanHoId"", ""HoTen"", ""SoDienThoai"", ""Email"", ""VaiTro"", ""DaiDienCanHo"", ""NgayVaoO"", ""NguoiDungId"", ""NgayTao"", ""DaXoa"")
SELECT can_ho, ho_ten, sdt, email, vai_tro, dai_dien, vao_o, uid, now(), FALSE FROM bql_cudan;
DROP TABLE bql_cudan;
UPDATE ""CanHo"" c SET ""TinhTrangThue"" = 2
WHERE EXISTS (SELECT 1 FROM ""CuDan"" r WHERE r.""CanHoId"" = c.""Id"" AND r.""NgayChuyenDi"" IS NULL);
");

            migrationBuilder.AddForeignKey(
                name: "FK_CanHo_Tang_TangId",
                table: "CanHo",
                column: "TangId",
                principalTable: "Tang",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CanHo_ToaNha_ToaNhaId",
                table: "CanHo",
                column: "ToaNhaId",
                principalTable: "ToaNha",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChiSoDichVu_CanHo_CanHoId",
                table: "ChiSoDichVu",
                column: "CanHoId",
                principalTable: "CanHo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HoaDon_CanHo_CanHoId",
                table: "HoaDon",
                column: "CanHoId",
                principalTable: "CanHo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PhiDinhKy_CanHo_CanHoId",
                table: "PhiDinhKy",
                column: "CanHoId",
                principalTable: "CanHo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        // Down chỉ khôi phục cấu trúc; dữ liệu hợp đồng/tin đăng đã xoá ở Up không lấy lại được.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CanHo_Tang_TangId",
                table: "CanHo");

            migrationBuilder.DropForeignKey(
                name: "FK_CanHo_ToaNha_ToaNhaId",
                table: "CanHo");

            migrationBuilder.DropForeignKey(
                name: "FK_ChiSoDichVu_CanHo_CanHoId",
                table: "ChiSoDichVu");

            migrationBuilder.DropForeignKey(
                name: "FK_HoaDon_CanHo_CanHoId",
                table: "HoaDon");

            migrationBuilder.DropForeignKey(
                name: "FK_PhiDinhKy_CanHo_CanHoId",
                table: "PhiDinhKy");

            migrationBuilder.DropTable(
                name: "CuDan");

            migrationBuilder.DropIndex(
                name: "IX_ChiSoDichVu_CanHoId_LoaiDichVuId_ThangTinhPhi",
                table: "ChiSoDichVu");

            migrationBuilder.DropIndex(
                name: "IX_CanHo_ToaNhaId_MaCanHo",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "DonGiaPhiQuanLy",
                table: "ToaNha");

            migrationBuilder.RenameColumn(
                name: "CanHoId",
                table: "PhiDinhKy",
                newName: "HopDongId");

            migrationBuilder.RenameIndex(
                name: "IX_PhiDinhKy_CanHoId",
                table: "PhiDinhKy",
                newName: "IX_PhiDinhKy_HopDongId");

            migrationBuilder.RenameColumn(
                name: "CanHoId",
                table: "HoaDon",
                newName: "HopDongId");

            migrationBuilder.RenameIndex(
                name: "IX_HoaDon_CanHoId_ThangTinhPhi_LoaiHoaDon",
                table: "HoaDon",
                newName: "IX_HoaDon_HopDongId_ThangTinhPhi_LoaiHoaDon");

            migrationBuilder.RenameColumn(
                name: "CanHoId",
                table: "ChiSoDichVu",
                newName: "HopDongId");

            migrationBuilder.AddColumn<int>(
                name: "HopDongId",
                table: "YeuCauBaoTri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DanhXungChuNha",
                table: "NguoiDung",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LaChuNha",
                table: "NguoiDung",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "GiaTu",
                table: "DuAn",
                type: "numeric(18,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "MoTaDayDu",
                table: "DuAn",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TrangThai",
                table: "DuAn",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "ToaNhaId",
                table: "CanHo",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "TangId",
                table: "CanHo",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "MaCanHo",
                table: "CanHo",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40);

            migrationBuilder.AddColumn<bool>(
                name: "ChoPhepThuCung",
                table: "CanHo",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ChuNhaId",
                table: "CanHo",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DanhMucId",
                table: "CanHo",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DiaChi",
                table: "CanHo",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DuAnId",
                table: "CanHo",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GhiChuKiemDuyet",
                table: "CanHo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GhiChuPhi",
                table: "CanHo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Gia",
                table: "CanHo",
                type: "numeric(18,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "KhuVucId",
                table: "CanHo",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "KinhDo",
                table: "CanHo",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LoaiChoDauXe",
                table: "CanHo",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LuotXem",
                table: "CanHo",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MoTaThem",
                table: "CanHo",
                type: "text",
                nullable: true);

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

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayDuyet",
                table: "CanHo",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NguoiDuyet",
                table: "CanHo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NoiBat",
                table: "CanHo",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "CanHo",
                type: "character varying(280)",
                maxLength: 280,
                nullable: false,
                defaultValue: "");

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

            migrationBuilder.AddColumn<decimal>(
                name: "TienDatCocMacDinh",
                table: "CanHo",
                type: "numeric(18,0)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TieuDe",
                table: "CanHo",
                type: "character varying(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TrangThai",
                table: "CanHo",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "ViDo",
                table: "CanHo",
                type: "double precision",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AnhCanHo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CanHoId = table.Column<int>(type: "integer", nullable: false),
                    ChuThich = table.Column<string>(type: "text", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayXoa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXoa = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    AnhBia = table.Column<bool>(type: "boolean", nullable: false),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false),
                    PhienBan = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    ThuTu = table.Column<int>(type: "integer", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnhCanHo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnhCanHo_CanHo_CanHoId",
                        column: x => x.CanHoId,
                        principalTable: "CanHo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnhDuAn",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DuAnId = table.Column<int>(type: "integer", nullable: false),
                    ChuThich = table.Column<string>(type: "text", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayXoa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXoa = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    AnhBia = table.Column<bool>(type: "boolean", nullable: false),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false),
                    PhienBan = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    ThuTu = table.Column<int>(type: "integer", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnhDuAn", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnhDuAn_DuAn_DuAnId",
                        column: x => x.DuAnId,
                        principalTable: "DuAn",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DanhGia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CanHoId = table.Column<int>(type: "integer", nullable: false),
                    NguoiDungId = table.Column<string>(type: "text", nullable: false),
                    NgayDuyet = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiDuyet = table.Column<string>(type: "text", nullable: true),
                    NoiDung = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayXoa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXoa = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false),
                    DiemSao = table.Column<int>(type: "integer", nullable: false),
                    GhiChuNguoiThue = table.Column<string>(type: "text", nullable: true),
                    PhienBan = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    TrangThai = table.Column<int>(type: "integer", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhGia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DanhGia_CanHo_CanHoId",
                        column: x => x.CanHoId,
                        principalTable: "CanHo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DanhGia_NguoiDung_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DanhMuc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayXoa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXoa = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    Icon = table.Column<string>(type: "text", nullable: true),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false),
                    Ten = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PhienBan = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    Slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhMuc", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HopDongThue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CanHoId = table.Column<int>(type: "integer", nullable: false),
                    NguoiThueChinhId = table.Column<string>(type: "text", nullable: false),
                    NgayKichHoat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NgayChotKy = table.Column<int>(type: "integer", nullable: false),
                    UrlHopDong = table.Column<string>(type: "text", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayXoa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXoa = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    TienDatCoc = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    TienDatCocDangGiu = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    TienDatCocDaHoanTra = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    NgayKetThuc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false),
                    SoNgayTreHan = table.Column<int>(type: "integer", nullable: false),
                    PhanTramPhiTre = table.Column<int>(type: "integer", nullable: false),
                    SoHopDong = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TienThueThang = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    GhiChu = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PhienBan = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    NgayBatDau = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TrangThai = table.Column<int>(type: "integer", nullable: false),
                    NgayChamDut = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LyDoChamDut = table.Column<string>(type: "text", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HopDongThue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HopDongThue_CanHo_CanHoId",
                        column: x => x.CanHoId,
                        principalTable: "CanHo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HopDongThue_NguoiDung_NguoiThueChinhId",
                        column: x => x.NguoiThueChinhId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HoSoNhuCauThue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KhuVucId = table.Column<int>(type: "integer", nullable: true),
                    NguoiDungId = table.Column<string>(type: "text", nullable: false),
                    CanChoPhepThuCung = table.Column<bool>(type: "boolean", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayXoa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXoa = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    MucNoiThat = table.Column<int>(type: "integer", nullable: true),
                    HuongNha = table.Column<int>(type: "integer", nullable: true),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false),
                    DienTichToiDa = table.Column<double>(type: "double precision", nullable: true),
                    BanKinhToiDaKm = table.Column<double>(type: "double precision", nullable: true),
                    TangToiDa = table.Column<int>(type: "integer", nullable: true),
                    SoThangThueToiDa = table.Column<int>(type: "integer", nullable: true),
                    GiaToiDa = table.Column<decimal>(type: "numeric(18,0)", nullable: true),
                    DienTichToiThieu = table.Column<double>(type: "double precision", nullable: true),
                    SoPhongNguToiThieu = table.Column<int>(type: "integer", nullable: true),
                    TangToiThieu = table.Column<int>(type: "integer", nullable: true),
                    SoThangThueToiThieu = table.Column<int>(type: "integer", nullable: true),
                    GiaToiThieu = table.Column<decimal>(type: "numeric(18,0)", nullable: true),
                    NgayDuKienVaoO = table.Column<DateOnly>(type: "date", nullable: true),
                    LoaiChoDauXe = table.Column<int>(type: "integer", nullable: true),
                    DiaChiUuTien = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ViDoUuTien = table.Column<double>(type: "double precision", nullable: true),
                    KinhDoUuTien = table.Column<double>(type: "double precision", nullable: true),
                    BatBuocDienTich = table.Column<bool>(type: "boolean", nullable: false),
                    BatBuocPhongNgu = table.Column<bool>(type: "boolean", nullable: false),
                    BatBuocLoaiHinh = table.Column<bool>(type: "boolean", nullable: false),
                    BatBuocHuongNha = table.Column<bool>(type: "boolean", nullable: false),
                    BatBuocKhoangTang = table.Column<bool>(type: "boolean", nullable: false),
                    BatBuocNoiThat = table.Column<bool>(type: "boolean", nullable: false),
                    BatBuocThoiHanThue = table.Column<bool>(type: "boolean", nullable: false),
                    BatBuocKhoangCach = table.Column<bool>(type: "boolean", nullable: false),
                    BatBuocNgayVaoO = table.Column<bool>(type: "boolean", nullable: false),
                    BatBuocDauXe = table.Column<bool>(type: "boolean", nullable: false),
                    BatBuocThuCung = table.Column<bool>(type: "boolean", nullable: false),
                    BatBuocKhoangGia = table.Column<bool>(type: "boolean", nullable: false),
                    BatBuocKhuVuc = table.Column<bool>(type: "boolean", nullable: false),
                    PhienBan = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
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
                name: "LichXemNha",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CanHoId = table.Column<int>(type: "integer", nullable: false),
                    NguoiDungId = table.Column<string>(type: "text", nullable: true),
                    LyDoHuy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NgayHuy = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiHuy = table.Column<string>(type: "text", nullable: true),
                    NgayXacNhan = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXacNhan = table.Column<string>(type: "text", nullable: true),
                    EmailLienHe = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TenLienHe = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SoDienThoaiLienHe = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayXoa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXoa = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false),
                    GhiChu = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PhienBan = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    NgayHen = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GioHen = table.Column<int>(type: "integer", nullable: false),
                    TrangThai = table.Column<int>(type: "integer", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichXemNha", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LichXemNha_CanHo_CanHoId",
                        column: x => x.CanHoId,
                        principalTable: "CanHo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LichXemNha_NguoiDung_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TienIch",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayXoa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXoa = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    Icon = table.Column<string>(type: "text", nullable: false),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false),
                    Ten = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PhienBan = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    Slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TienIch", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "YeuThich",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CanHoId = table.Column<int>(type: "integer", nullable: false),
                    NguoiDungId = table.Column<string>(type: "text", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayXoa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXoa = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false),
                    PhienBan = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YeuThich", x => x.Id);
                    table.ForeignKey(
                        name: "FK_YeuThich_CanHo_CanHoId",
                        column: x => x.CanHoId,
                        principalTable: "CanHo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_YeuThich_NguoiDung_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HopDong_NguoiThue",
                columns: table => new
                {
                    HopDongId = table.Column<int>(type: "integer", nullable: false),
                    NguoiThueId = table.Column<string>(type: "text", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    MoiQuanHe = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HopDong_NguoiThue", x => new { x.HopDongId, x.NguoiThueId });
                    table.ForeignKey(
                        name: "FK_HopDong_NguoiThue_HopDongThue_HopDongId",
                        column: x => x.HopDongId,
                        principalTable: "HopDongThue",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HopDong_NguoiThue_NguoiDung_NguoiThueId",
                        column: x => x.NguoiThueId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KiemKeCanHo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NguoiKiemTraId = table.Column<string>(type: "text", nullable: true),
                    HopDongId = table.Column<int>(type: "integer", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    GhiChuHuHong = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    NgayXoa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXoa = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    KhauTruDatCoc = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    NgayKiemKe = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false),
                    TinhTrangChung = table.Column<int>(type: "integer", nullable: false),
                    DanhSachAnh = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PhienBan = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    TomTat = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    KhachThueDaKy = table.Column<bool>(type: "boolean", nullable: false),
                    Loai = table.Column<int>(type: "integer", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KiemKeCanHo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KiemKeCanHo_HopDongThue_HopDongId",
                        column: x => x.HopDongId,
                        principalTable: "HopDongThue",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KiemKeCanHo_NguoiDung_NguoiKiemTraId",
                        column: x => x.NguoiKiemTraId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id");
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
                name: "CanHo_TienIch",
                columns: table => new
                {
                    CanHoId = table.Column<int>(type: "integer", nullable: false),
                    TienIchId = table.Column<int>(type: "integer", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanHo_TienIch", x => new { x.CanHoId, x.TienIchId });
                    table.ForeignKey(
                        name: "FK_CanHo_TienIch_CanHo_CanHoId",
                        column: x => x.CanHoId,
                        principalTable: "CanHo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CanHo_TienIch_TienIch_TienIchId",
                        column: x => x.TienIchId,
                        principalTable: "TienIch",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HoSoNhuCauThue_TienIch",
                columns: table => new
                {
                    HoSoNhuCauThueId = table.Column<int>(type: "integer", nullable: false),
                    TienIchId = table.Column<int>(type: "integer", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    BatBuoc = table.Column<bool>(type: "boolean", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "GiaoDichDatCoc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HopDongId = table.Column<int>(type: "integer", nullable: false),
                    KiemKeLienQuanId = table.Column<int>(type: "integer", nullable: true),
                    YeuCauLienQuanId = table.Column<int>(type: "integer", nullable: true),
                    SoTien = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayXoa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXoa = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false),
                    LyDo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NgayGhiNhan = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiGhiNhan = table.Column<string>(type: "text", nullable: true),
                    PhienBan = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    Loai = table.Column<int>(type: "integer", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiaoDichDatCoc", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GiaoDichDatCoc_HopDongThue_HopDongId",
                        column: x => x.HopDongId,
                        principalTable: "HopDongThue",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GiaoDichDatCoc_KiemKeCanHo_KiemKeLienQuanId",
                        column: x => x.KiemKeLienQuanId,
                        principalTable: "KiemKeCanHo",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GiaoDichDatCoc_YeuCauBaoTri_YeuCauLienQuanId",
                        column: x => x.YeuCauLienQuanId,
                        principalTable: "YeuCauBaoTri",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauBaoTri_HopDongId",
                table: "YeuCauBaoTri",
                column: "HopDongId");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDon_HopDongId",
                table: "HoaDon",
                column: "HopDongId");

            migrationBuilder.CreateIndex(
                name: "IX_DuAn_TrangThai",
                table: "DuAn",
                column: "TrangThai");

            migrationBuilder.CreateIndex(
                name: "IX_ChiSoDichVu_HopDongId_LoaiDichVuId_ThangTinhPhi",
                table: "ChiSoDichVu",
                columns: new[] { "HopDongId", "LoaiDichVuId", "ThangTinhPhi" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CanHo_ChuNhaId",
                table: "CanHo",
                column: "ChuNhaId");

            migrationBuilder.CreateIndex(
                name: "IX_CanHo_DanhMucId",
                table: "CanHo",
                column: "DanhMucId");

            migrationBuilder.CreateIndex(
                name: "IX_CanHo_DuAnId",
                table: "CanHo",
                column: "DuAnId");

            migrationBuilder.CreateIndex(
                name: "IX_CanHo_Gia",
                table: "CanHo",
                column: "Gia");

            migrationBuilder.CreateIndex(
                name: "IX_CanHo_KhuVucId",
                table: "CanHo",
                column: "KhuVucId");

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
                name: "IX_CanHo_NoiBat",
                table: "CanHo",
                column: "NoiBat");

            migrationBuilder.CreateIndex(
                name: "IX_CanHo_Slug",
                table: "CanHo",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CanHo_ToaNhaId",
                table: "CanHo",
                column: "ToaNhaId");

            migrationBuilder.CreateIndex(
                name: "IX_CanHo_ToaNhaId_MaCanHo",
                table: "CanHo",
                columns: new[] { "ToaNhaId", "MaCanHo" },
                unique: true,
                filter: "\"ToaNhaId\" IS NOT NULL AND \"MaCanHo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CanHo_TrangThai",
                table: "CanHo",
                column: "TrangThai");

            migrationBuilder.CreateIndex(
                name: "IX_AnhCanHo_CanHoId",
                table: "AnhCanHo",
                column: "CanHoId");

            migrationBuilder.CreateIndex(
                name: "IX_AnhDuAn_DuAnId",
                table: "AnhDuAn",
                column: "DuAnId");

            migrationBuilder.CreateIndex(
                name: "IX_CanHo_TienIch_TienIchId",
                table: "CanHo_TienIch",
                column: "TienIchId");

            migrationBuilder.CreateIndex(
                name: "IX_DanhGia_CanHoId",
                table: "DanhGia",
                column: "CanHoId");

            migrationBuilder.CreateIndex(
                name: "IX_DanhGia_NguoiDungId",
                table: "DanhGia",
                column: "NguoiDungId");

            migrationBuilder.CreateIndex(
                name: "IX_DanhGia_TrangThai",
                table: "DanhGia",
                column: "TrangThai");

            migrationBuilder.CreateIndex(
                name: "IX_DanhMuc_Slug",
                table: "DanhMuc",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichDatCoc_HopDongId",
                table: "GiaoDichDatCoc",
                column: "HopDongId");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichDatCoc_KiemKeLienQuanId",
                table: "GiaoDichDatCoc",
                column: "KiemKeLienQuanId");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichDatCoc_Loai",
                table: "GiaoDichDatCoc",
                column: "Loai");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichDatCoc_NgayGhiNhan",
                table: "GiaoDichDatCoc",
                column: "NgayGhiNhan");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichDatCoc_YeuCauLienQuanId",
                table: "GiaoDichDatCoc",
                column: "YeuCauLienQuanId");

            migrationBuilder.CreateIndex(
                name: "IX_HopDong_NguoiThue_NguoiThueId",
                table: "HopDong_NguoiThue",
                column: "NguoiThueId");

            migrationBuilder.CreateIndex(
                name: "IX_HopDongThue_CanHoId",
                table: "HopDongThue",
                column: "CanHoId");

            migrationBuilder.CreateIndex(
                name: "IX_HopDongThue_NgayBatDau_NgayKetThuc",
                table: "HopDongThue",
                columns: new[] { "NgayBatDau", "NgayKetThuc" });

            migrationBuilder.CreateIndex(
                name: "IX_HopDongThue_NguoiThueChinhId",
                table: "HopDongThue",
                column: "NguoiThueChinhId");

            migrationBuilder.CreateIndex(
                name: "IX_HopDongThue_SoHopDong",
                table: "HopDongThue",
                column: "SoHopDong",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HopDongThue_TrangThai",
                table: "HopDongThue",
                column: "TrangThai");

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

            migrationBuilder.CreateIndex(
                name: "IX_KiemKeCanHo_HopDongId",
                table: "KiemKeCanHo",
                column: "HopDongId");

            migrationBuilder.CreateIndex(
                name: "IX_KiemKeCanHo_HopDongId_Loai",
                table: "KiemKeCanHo",
                columns: new[] { "HopDongId", "Loai" });

            migrationBuilder.CreateIndex(
                name: "IX_KiemKeCanHo_NguoiKiemTraId",
                table: "KiemKeCanHo",
                column: "NguoiKiemTraId");

            migrationBuilder.CreateIndex(
                name: "IX_LichXemNha_CanHoId",
                table: "LichXemNha",
                column: "CanHoId");

            migrationBuilder.CreateIndex(
                name: "IX_LichXemNha_NgayHen",
                table: "LichXemNha",
                column: "NgayHen");

            migrationBuilder.CreateIndex(
                name: "IX_LichXemNha_NguoiDungId",
                table: "LichXemNha",
                column: "NguoiDungId");

            migrationBuilder.CreateIndex(
                name: "IX_LichXemNha_TrangThai",
                table: "LichXemNha",
                column: "TrangThai");

            migrationBuilder.CreateIndex(
                name: "IX_TienIch_Slug",
                table: "TienIch",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_YeuThich_CanHoId",
                table: "YeuThich",
                column: "CanHoId");

            migrationBuilder.CreateIndex(
                name: "IX_YeuThich_NguoiDungId_CanHoId",
                table: "YeuThich",
                columns: new[] { "NguoiDungId", "CanHoId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CanHo_DanhMuc_DanhMucId",
                table: "CanHo",
                column: "DanhMucId",
                principalTable: "DanhMuc",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CanHo_DuAn_DuAnId",
                table: "CanHo",
                column: "DuAnId",
                principalTable: "DuAn",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CanHo_KhuVuc_KhuVucId",
                table: "CanHo",
                column: "KhuVucId",
                principalTable: "KhuVuc",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CanHo_NguoiDung_ChuNhaId",
                table: "CanHo",
                column: "ChuNhaId",
                principalTable: "NguoiDung",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CanHo_Tang_TangId",
                table: "CanHo",
                column: "TangId",
                principalTable: "Tang",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CanHo_ToaNha_ToaNhaId",
                table: "CanHo",
                column: "ToaNhaId",
                principalTable: "ToaNha",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ChiSoDichVu_HopDongThue_HopDongId",
                table: "ChiSoDichVu",
                column: "HopDongId",
                principalTable: "HopDongThue",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HoaDon_HopDongThue_HopDongId",
                table: "HoaDon",
                column: "HopDongId",
                principalTable: "HopDongThue",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PhiDinhKy_HopDongThue_HopDongId",
                table: "PhiDinhKy",
                column: "HopDongId",
                principalTable: "HopDongThue",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_YeuCauBaoTri_HopDongThue_HopDongId",
                table: "YeuCauBaoTri",
                column: "HopDongId",
                principalTable: "HopDongThue",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
