using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace t.Migrations
{
    /// <inheritdoc />
    public partial class Tuan8TenThuocTinhRoNghia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_YeuCauBaoTri_HanXuLy",
                table: "YeuCauBaoTri");

            migrationBuilder.DropCheckConstraint(
                name: "CK_YeuCauBaoTri_NoiXayRa",
                table: "YeuCauBaoTri");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ThanhToan_MaSo",
                table: "ThanhToan");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LoaiDichVu_DonGia",
                table: "LoaiDichVu");

            migrationBuilder.RenameColumn(
                name: "ViTri",
                table: "YeuCauBaoTri",
                newName: "ViTriKhuVucChung");

            migrationBuilder.RenameColumn(
                name: "NgayBaoCao",
                table: "YeuCauBaoTri",
                newName: "NgayBaoSuCo");

            migrationBuilder.RenameIndex(
                name: "IX_YeuCauBaoTri_NgayBaoCao",
                table: "YeuCauBaoTri",
                newName: "IX_YeuCauBaoTri_NgayBaoSuCo");

            migrationBuilder.RenameColumn(
                name: "PhuongThuc",
                table: "ThanhToan",
                newName: "PhuongThucThanhToan");

            migrationBuilder.RenameColumn(
                name: "NhaCungCap",
                table: "ThanhToan",
                newName: "DonViThanhToan");

            migrationBuilder.RenameColumn(
                name: "PhanHe",
                table: "Quyen",
                newName: "PhanHeQuyen");

            migrationBuilder.RenameIndex(
                name: "IX_Quyen_PhanHe",
                table: "Quyen",
                newName: "IX_Quyen_PhanHeQuyen");

            migrationBuilder.RenameColumn(
                name: "HanhDong",
                table: "NhatKyHeThong",
                newName: "LoaiThaoTac");

            migrationBuilder.RenameColumn(
                name: "NhatKyId",
                table: "NhatKyHeThong",
                newName: "NhatKyHeThongId");

            migrationBuilder.RenameColumn(
                name: "AnhDaiDien",
                table: "NguoiDung",
                newName: "AnhDaiDienNguoiDung");

            migrationBuilder.RenameColumn(
                name: "DonGia",
                table: "LoaiDichVu",
                newName: "DonGiaDichVu");

            migrationBuilder.RenameColumn(
                name: "NgayXacNhan",
                table: "CuTru",
                newName: "NgayXacNhanCuTru");

            migrationBuilder.AddCheckConstraint(
                name: "CK_YeuCauBaoTri_HanXuLy",
                table: "YeuCauBaoTri",
                sql: "\"HanXuLy\" IS NULL OR \"HanXuLy\" >= \"NgayBaoSuCo\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_YeuCauBaoTri_NoiXayRa",
                table: "YeuCauBaoTri",
                sql: "(\"CanHoId\" IS NOT NULL AND \"ToaNhaId\" IS NULL) OR (\"CanHoId\" IS NULL AND \"ToaNhaId\" IS NOT NULL AND \"ViTriKhuVucChung\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ThanhToan_MaSo",
                table: "ThanhToan",
                sql: "\"PhuongThucThanhToan\" BETWEEN 0 AND 4 AND \"TrangThaiThanhToan\" BETWEEN 0 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LoaiDichVu_DonGia",
                table: "LoaiDichVu",
                sql: "\"DonGiaDichVu\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_YeuCauBaoTri_HanXuLy",
                table: "YeuCauBaoTri");

            migrationBuilder.DropCheckConstraint(
                name: "CK_YeuCauBaoTri_NoiXayRa",
                table: "YeuCauBaoTri");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ThanhToan_MaSo",
                table: "ThanhToan");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LoaiDichVu_DonGia",
                table: "LoaiDichVu");

            migrationBuilder.RenameColumn(
                name: "ViTriKhuVucChung",
                table: "YeuCauBaoTri",
                newName: "ViTri");

            migrationBuilder.RenameColumn(
                name: "NgayBaoSuCo",
                table: "YeuCauBaoTri",
                newName: "NgayBaoCao");

            migrationBuilder.RenameIndex(
                name: "IX_YeuCauBaoTri_NgayBaoSuCo",
                table: "YeuCauBaoTri",
                newName: "IX_YeuCauBaoTri_NgayBaoCao");

            migrationBuilder.RenameColumn(
                name: "PhuongThucThanhToan",
                table: "ThanhToan",
                newName: "PhuongThuc");

            migrationBuilder.RenameColumn(
                name: "DonViThanhToan",
                table: "ThanhToan",
                newName: "NhaCungCap");

            migrationBuilder.RenameColumn(
                name: "PhanHeQuyen",
                table: "Quyen",
                newName: "PhanHe");

            migrationBuilder.RenameIndex(
                name: "IX_Quyen_PhanHeQuyen",
                table: "Quyen",
                newName: "IX_Quyen_PhanHe");

            migrationBuilder.RenameColumn(
                name: "LoaiThaoTac",
                table: "NhatKyHeThong",
                newName: "HanhDong");

            migrationBuilder.RenameColumn(
                name: "NhatKyHeThongId",
                table: "NhatKyHeThong",
                newName: "NhatKyId");

            migrationBuilder.RenameColumn(
                name: "AnhDaiDienNguoiDung",
                table: "NguoiDung",
                newName: "AnhDaiDien");

            migrationBuilder.RenameColumn(
                name: "DonGiaDichVu",
                table: "LoaiDichVu",
                newName: "DonGia");

            migrationBuilder.RenameColumn(
                name: "NgayXacNhanCuTru",
                table: "CuTru",
                newName: "NgayXacNhan");

            migrationBuilder.AddCheckConstraint(
                name: "CK_YeuCauBaoTri_HanXuLy",
                table: "YeuCauBaoTri",
                sql: "\"HanXuLy\" IS NULL OR \"HanXuLy\" >= \"NgayBaoCao\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_YeuCauBaoTri_NoiXayRa",
                table: "YeuCauBaoTri",
                sql: "(\"CanHoId\" IS NOT NULL AND \"ToaNhaId\" IS NULL) OR (\"CanHoId\" IS NULL AND \"ToaNhaId\" IS NOT NULL AND \"ViTri\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ThanhToan_MaSo",
                table: "ThanhToan",
                sql: "\"PhuongThuc\" BETWEEN 0 AND 4 AND \"TrangThaiThanhToan\" BETWEEN 0 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LoaiDichVu_DonGia",
                table: "LoaiDichVu",
                sql: "\"DonGia\" >= 0");
        }
    }
}
