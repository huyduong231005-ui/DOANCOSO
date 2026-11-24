using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace t.Migrations
{
    /// <inheritdoc />
    public partial class Tuan8RaSoatTuGoc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnhSuCo_NguoiDung_NguoiTaiLenId",
                table: "AnhSuCo");

            migrationBuilder.DropForeignKey(
                name: "FK_AnhSuCo_YeuCauBaoTri_YeuCauBaoTriId",
                table: "AnhSuCo");

            migrationBuilder.DropForeignKey(
                name: "FK_LichSuTrangThaiSuCo_NguoiDung_NguoiThucHienId",
                table: "LichSuTrangThaiSuCo");

            migrationBuilder.DropForeignKey(
                name: "FK_LichSuTrangThaiSuCo_YeuCauBaoTri_YeuCauBaoTriId",
                table: "LichSuTrangThaiSuCo");

            migrationBuilder.DropIndex(
                name: "IX_ToaNha_SlugToaNha",
                table: "ToaNha");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ToaNha_NgayChotKy",
                table: "ToaNha");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ToaNha_PhiTre",
                table: "ToaNha");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LoaiDichVu_TinhPhi",
                table: "LoaiDichVu");

            migrationBuilder.DropIndex(
                name: "IX_KhuVuc_SlugKhuVuc",
                table: "KhuVuc");

            migrationBuilder.DropIndex(
                name: "IX_DuAn_SlugDuAn",
                table: "DuAn");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ChiSoDichVu_TieuThu",
                table: "ChiSoDichVu");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CanHo_TinhTrangThue",
                table: "CanHo");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LichSuTrangThaiSuCo",
                table: "LichSuTrangThaiSuCo");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AnhSuCo",
                table: "AnhSuCo");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AnhSuCo_GiaiDoan",
                table: "AnhSuCo");

            migrationBuilder.DropColumn(
                name: "PhienBan",
                table: "YeuCauBaoTri");

            migrationBuilder.DropColumn(
                name: "DiaChiToaNha",
                table: "ToaNha");

            migrationBuilder.DropColumn(
                name: "PhienBan",
                table: "ToaNha");

            migrationBuilder.DropColumn(
                name: "SlugToaNha",
                table: "ToaNha");

            migrationBuilder.DropColumn(
                name: "PhienBan",
                table: "ThanhToan");

            migrationBuilder.DropColumn(
                name: "NhanTang",
                table: "Tang");

            migrationBuilder.DropColumn(
                name: "PhienBan",
                table: "Tang");

            migrationBuilder.DropColumn(
                name: "MoTaQuyen",
                table: "Quyen");

            migrationBuilder.DropColumn(
                name: "PhienBan",
                table: "Quyen");

            migrationBuilder.DropColumn(
                name: "PhienBan",
                table: "PhiDinhKy");

            migrationBuilder.DropColumn(
                name: "BieuTuong",
                table: "LoaiDichVu");

            migrationBuilder.DropColumn(
                name: "CheDoTinhPhi",
                table: "LoaiDichVu");

            migrationBuilder.DropColumn(
                name: "PhienBan",
                table: "LoaiDichVu");

            migrationBuilder.DropColumn(
                name: "PhienBan",
                table: "KhuVuc");

            migrationBuilder.DropColumn(
                name: "SlugKhuVuc",
                table: "KhuVuc");

            migrationBuilder.DropColumn(
                name: "DonViTienTe",
                table: "HoaDon");

            migrationBuilder.DropColumn(
                name: "LaHoaDonDinhKy",
                table: "HoaDon");

            migrationBuilder.DropColumn(
                name: "PhienBan",
                table: "HoaDon");

            migrationBuilder.DropColumn(
                name: "PhienBan",
                table: "DuAn");

            migrationBuilder.DropColumn(
                name: "SlugDuAn",
                table: "DuAn");

            migrationBuilder.DropColumn(
                name: "PhienBan",
                table: "CuTru");

            migrationBuilder.DropColumn(
                name: "PhienBan",
                table: "ChiTietHoaDon");

            migrationBuilder.DropColumn(
                name: "DonGiaChiSo",
                table: "ChiSoDichVu");

            migrationBuilder.DropColumn(
                name: "PhienBan",
                table: "ChiSoDichVu");

            migrationBuilder.DropColumn(
                name: "ThanhTienChiSo",
                table: "ChiSoDichVu");

            migrationBuilder.DropColumn(
                name: "TieuThu",
                table: "ChiSoDichVu");

            migrationBuilder.DropColumn(
                name: "PhienBan",
                table: "CanHo");

            migrationBuilder.RenameTable(
                name: "LichSuTrangThaiSuCo",
                newName: "LichSuYeuCau");

            migrationBuilder.RenameTable(
                name: "AnhSuCo",
                newName: "AnhYeuCau");

            migrationBuilder.RenameColumn(
                name: "SoNgayTreHanMacDinh",
                table: "ToaNha",
                newName: "SoNgayTreHan");

            migrationBuilder.RenameColumn(
                name: "PhanTramPhiTreMacDinh",
                table: "ToaNha",
                newName: "PhanTramPhiTre");

            migrationBuilder.RenameColumn(
                name: "NgayChotKyMacDinh",
                table: "ToaNha",
                newName: "NgayChotKy");

            migrationBuilder.RenameColumn(
                name: "DonGiaMacDinh",
                table: "LoaiDichVu",
                newName: "DonGia");

            migrationBuilder.RenameColumn(
                name: "TinhTrangThue",
                table: "CanHo",
                newName: "TinhTrangCanHo");

            // "Đang ở" (2) không lưu ở căn nữa mà suy ra từ CuTru: căn đó chỉ còn là "Đã bàn giao" (0).
            migrationBuilder.Sql("UPDATE \"CanHo\" SET \"TinhTrangCanHo\" = 0 WHERE \"TinhTrangCanHo\" = 2;");

            migrationBuilder.RenameIndex(
                name: "IX_CanHo_TinhTrangThue",
                table: "CanHo",
                newName: "IX_CanHo_TinhTrangCanHo");

            migrationBuilder.RenameColumn(
                name: "LichSuTrangThaiId",
                table: "LichSuYeuCau",
                newName: "LichSuYeuCauId");

            migrationBuilder.RenameIndex(
                name: "IX_LichSuTrangThaiSuCo_YeuCauBaoTriId_ThoiDiemChuyen",
                table: "LichSuYeuCau",
                newName: "IX_LichSuYeuCau_YeuCauBaoTriId_ThoiDiemChuyen");

            migrationBuilder.RenameIndex(
                name: "IX_LichSuTrangThaiSuCo_NguoiThucHienId",
                table: "LichSuYeuCau",
                newName: "IX_LichSuYeuCau_NguoiThucHienId");

            migrationBuilder.RenameColumn(
                name: "AnhSuCoId",
                table: "AnhYeuCau",
                newName: "AnhYeuCauId");

            migrationBuilder.RenameIndex(
                name: "IX_AnhSuCo_YeuCauBaoTriId",
                table: "AnhYeuCau",
                newName: "IX_AnhYeuCau_YeuCauBaoTriId");

            migrationBuilder.RenameIndex(
                name: "IX_AnhSuCo_NguoiTaiLenId",
                table: "AnhYeuCau",
                newName: "IX_AnhYeuCau_NguoiTaiLenId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LichSuYeuCau",
                table: "LichSuYeuCau",
                column: "LichSuYeuCauId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AnhYeuCau",
                table: "AnhYeuCau",
                column: "AnhYeuCauId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ToaNha_NgayChotKy",
                table: "ToaNha",
                sql: "\"NgayChotKy\" BETWEEN 1 AND 28");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ToaNha_PhiTre",
                table: "ToaNha",
                sql: "\"SoNgayTreHan\" >= 0 AND \"PhanTramPhiTre\" BETWEEN 0 AND 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LoaiDichVu_DonGia",
                table: "LoaiDichVu",
                sql: "\"DonGia\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CanHo_TinhTrangCanHo",
                table: "CanHo",
                sql: "\"TinhTrangCanHo\" IN (0, 1, 3)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AnhYeuCau_GiaiDoan",
                table: "AnhYeuCau",
                sql: "\"GiaiDoanAnh\" IN (0, 1)");

            migrationBuilder.AddForeignKey(
                name: "FK_AnhYeuCau_NguoiDung_NguoiTaiLenId",
                table: "AnhYeuCau",
                column: "NguoiTaiLenId",
                principalTable: "NguoiDung",
                principalColumn: "NguoiDungId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AnhYeuCau_YeuCauBaoTri_YeuCauBaoTriId",
                table: "AnhYeuCau",
                column: "YeuCauBaoTriId",
                principalTable: "YeuCauBaoTri",
                principalColumn: "YeuCauBaoTriId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LichSuYeuCau_NguoiDung_NguoiThucHienId",
                table: "LichSuYeuCau",
                column: "NguoiThucHienId",
                principalTable: "NguoiDung",
                principalColumn: "NguoiDungId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_LichSuYeuCau_YeuCauBaoTri_YeuCauBaoTriId",
                table: "LichSuYeuCau",
                column: "YeuCauBaoTriId",
                principalTable: "YeuCauBaoTri",
                principalColumn: "YeuCauBaoTriId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnhYeuCau_NguoiDung_NguoiTaiLenId",
                table: "AnhYeuCau");

            migrationBuilder.DropForeignKey(
                name: "FK_AnhYeuCau_YeuCauBaoTri_YeuCauBaoTriId",
                table: "AnhYeuCau");

            migrationBuilder.DropForeignKey(
                name: "FK_LichSuYeuCau_NguoiDung_NguoiThucHienId",
                table: "LichSuYeuCau");

            migrationBuilder.DropForeignKey(
                name: "FK_LichSuYeuCau_YeuCauBaoTri_YeuCauBaoTriId",
                table: "LichSuYeuCau");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ToaNha_NgayChotKy",
                table: "ToaNha");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ToaNha_PhiTre",
                table: "ToaNha");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LoaiDichVu_DonGia",
                table: "LoaiDichVu");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CanHo_TinhTrangCanHo",
                table: "CanHo");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LichSuYeuCau",
                table: "LichSuYeuCau");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AnhYeuCau",
                table: "AnhYeuCau");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AnhYeuCau_GiaiDoan",
                table: "AnhYeuCau");

            migrationBuilder.RenameTable(
                name: "LichSuYeuCau",
                newName: "LichSuTrangThaiSuCo");

            migrationBuilder.RenameTable(
                name: "AnhYeuCau",
                newName: "AnhSuCo");

            migrationBuilder.RenameColumn(
                name: "SoNgayTreHan",
                table: "ToaNha",
                newName: "SoNgayTreHanMacDinh");

            migrationBuilder.RenameColumn(
                name: "PhanTramPhiTre",
                table: "ToaNha",
                newName: "PhanTramPhiTreMacDinh");

            migrationBuilder.RenameColumn(
                name: "NgayChotKy",
                table: "ToaNha",
                newName: "NgayChotKyMacDinh");

            migrationBuilder.RenameColumn(
                name: "DonGia",
                table: "LoaiDichVu",
                newName: "DonGiaMacDinh");

            migrationBuilder.RenameColumn(
                name: "TinhTrangCanHo",
                table: "CanHo",
                newName: "TinhTrangThue");

            migrationBuilder.Sql("UPDATE \"CanHo\" h SET \"TinhTrangThue\" = 2 WHERE \"TinhTrangThue\" = 0 AND EXISTS (SELECT 1 FROM \"CuTru\" c WHERE c.\"CanHoId\" = h.\"CanHoId\" AND c.\"NgayChuyenDi\" IS NULL AND NOT c.\"DaXoa\");");

            migrationBuilder.RenameIndex(
                name: "IX_CanHo_TinhTrangCanHo",
                table: "CanHo",
                newName: "IX_CanHo_TinhTrangThue");

            migrationBuilder.RenameColumn(
                name: "LichSuYeuCauId",
                table: "LichSuTrangThaiSuCo",
                newName: "LichSuTrangThaiId");

            migrationBuilder.RenameIndex(
                name: "IX_LichSuYeuCau_YeuCauBaoTriId_ThoiDiemChuyen",
                table: "LichSuTrangThaiSuCo",
                newName: "IX_LichSuTrangThaiSuCo_YeuCauBaoTriId_ThoiDiemChuyen");

            migrationBuilder.RenameIndex(
                name: "IX_LichSuYeuCau_NguoiThucHienId",
                table: "LichSuTrangThaiSuCo",
                newName: "IX_LichSuTrangThaiSuCo_NguoiThucHienId");

            migrationBuilder.RenameColumn(
                name: "AnhYeuCauId",
                table: "AnhSuCo",
                newName: "AnhSuCoId");

            migrationBuilder.RenameIndex(
                name: "IX_AnhYeuCau_YeuCauBaoTriId",
                table: "AnhSuCo",
                newName: "IX_AnhSuCo_YeuCauBaoTriId");

            migrationBuilder.RenameIndex(
                name: "IX_AnhYeuCau_NguoiTaiLenId",
                table: "AnhSuCo",
                newName: "IX_AnhSuCo_NguoiTaiLenId");

            migrationBuilder.AddColumn<byte[]>(
                name: "PhienBan",
                table: "YeuCauBaoTri",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiaChiToaNha",
                table: "ToaNha",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "PhienBan",
                table: "ToaNha",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SlugToaNha",
                table: "ToaNha",
                type: "character varying(220)",
                maxLength: 220,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "PhienBan",
                table: "ThanhToan",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NhanTang",
                table: "Tang",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "PhienBan",
                table: "Tang",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MoTaQuyen",
                table: "Quyen",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "PhienBan",
                table: "Quyen",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "PhienBan",
                table: "PhiDinhKy",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BieuTuong",
                table: "LoaiDichVu",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CheDoTinhPhi",
                table: "LoaiDichVu",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "PhienBan",
                table: "LoaiDichVu",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "PhienBan",
                table: "KhuVuc",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SlugKhuVuc",
                table: "KhuVuc",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DonViTienTe",
                table: "HoaDon",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "LaHoaDonDinhKy",
                table: "HoaDon",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "PhienBan",
                table: "HoaDon",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "PhienBan",
                table: "DuAn",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SlugDuAn",
                table: "DuAn",
                type: "character varying(220)",
                maxLength: 220,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "PhienBan",
                table: "CuTru",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "PhienBan",
                table: "ChiTietHoaDon",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DonGiaChiSo",
                table: "ChiSoDichVu",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<byte[]>(
                name: "PhienBan",
                table: "ChiSoDichVu",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ThanhTienChiSo",
                table: "ChiSoDichVu",
                type: "numeric(18,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TieuThu",
                table: "ChiSoDichVu",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<byte[]>(
                name: "PhienBan",
                table: "CanHo",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_LichSuTrangThaiSuCo",
                table: "LichSuTrangThaiSuCo",
                column: "LichSuTrangThaiId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AnhSuCo",
                table: "AnhSuCo",
                column: "AnhSuCoId");

            migrationBuilder.CreateIndex(
                name: "IX_ToaNha_SlugToaNha",
                table: "ToaNha",
                column: "SlugToaNha",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ToaNha_NgayChotKy",
                table: "ToaNha",
                sql: "\"NgayChotKyMacDinh\" BETWEEN 1 AND 28");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ToaNha_PhiTre",
                table: "ToaNha",
                sql: "\"SoNgayTreHanMacDinh\" >= 0 AND \"PhanTramPhiTreMacDinh\" BETWEEN 0 AND 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LoaiDichVu_TinhPhi",
                table: "LoaiDichVu",
                sql: "\"CheDoTinhPhi\" IN (0, 1) AND \"DonGiaMacDinh\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_KhuVuc_SlugKhuVuc",
                table: "KhuVuc",
                column: "SlugKhuVuc",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DuAn_SlugDuAn",
                table: "DuAn",
                column: "SlugDuAn",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChiSoDichVu_TieuThu",
                table: "ChiSoDichVu",
                sql: "\"TieuThu\" = \"ChiSoMoi\" - \"ChiSoCu\" AND \"ThanhTienChiSo\" = round(\"TieuThu\" * \"DonGiaChiSo\")");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CanHo_TinhTrangThue",
                table: "CanHo",
                sql: "\"TinhTrangThue\" BETWEEN 0 AND 3");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AnhSuCo_GiaiDoan",
                table: "AnhSuCo",
                sql: "\"GiaiDoanAnh\" IN (0, 1)");

            migrationBuilder.AddForeignKey(
                name: "FK_AnhSuCo_NguoiDung_NguoiTaiLenId",
                table: "AnhSuCo",
                column: "NguoiTaiLenId",
                principalTable: "NguoiDung",
                principalColumn: "NguoiDungId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AnhSuCo_YeuCauBaoTri_YeuCauBaoTriId",
                table: "AnhSuCo",
                column: "YeuCauBaoTriId",
                principalTable: "YeuCauBaoTri",
                principalColumn: "YeuCauBaoTriId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LichSuTrangThaiSuCo_NguoiDung_NguoiThucHienId",
                table: "LichSuTrangThaiSuCo",
                column: "NguoiThucHienId",
                principalTable: "NguoiDung",
                principalColumn: "NguoiDungId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_LichSuTrangThaiSuCo_YeuCauBaoTri_YeuCauBaoTriId",
                table: "LichSuTrangThaiSuCo",
                column: "YeuCauBaoTriId",
                principalTable: "YeuCauBaoTri",
                principalColumn: "YeuCauBaoTriId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
