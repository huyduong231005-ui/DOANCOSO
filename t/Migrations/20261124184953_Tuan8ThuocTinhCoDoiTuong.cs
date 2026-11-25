using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace t.Migrations
{
    /// <inheritdoc />
    public partial class Tuan8ThuocTinhCoDoiTuong : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "CK_YeuCauBaoTri_KetQua",
                table: "YeuCauBaoTri");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ToaNha_NgayChotKy",
                table: "ToaNha");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ToaNha_PhiTre",
                table: "ToaNha");

            migrationBuilder.DropCheckConstraint(
                name: "CK_HoaDon_CongThuc",
                table: "HoaDon");

            migrationBuilder.DropCheckConstraint(
                name: "CK_HoaDon_ConLai",
                table: "HoaDon");

            migrationBuilder.DropCheckConstraint(
                name: "CK_HoaDon_HanThanhToan",
                table: "HoaDon");

            migrationBuilder.DropCheckConstraint(
                name: "CK_HoaDon_SoTien",
                table: "HoaDon");

            migrationBuilder.DropIndex(
                name: "UX_CuTru_DaiDien",
                table: "CuTru");

            migrationBuilder.DropIndex(
                name: "UX_CuTru_DangO",
                table: "CuTru");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CuTru_NgayChuyenDi",
                table: "CuTru");

            migrationBuilder.RenameColumn(
                name: "KetQuaXuLy",
                table: "YeuCauBaoTri",
                newName: "KetQuaXuLyYeuCau");

            migrationBuilder.RenameColumn(
                name: "HanXuLy",
                table: "YeuCauBaoTri",
                newName: "HanXuLyYeuCau");

            migrationBuilder.RenameColumn(
                name: "DiemDanhGia",
                table: "YeuCauBaoTri",
                newName: "DiemDanhGiaYeuCau");

            migrationBuilder.RenameColumn(
                name: "ChiPhiUocTinh",
                table: "YeuCauBaoTri",
                newName: "ChiPhiUocTinhYeuCau");

            migrationBuilder.RenameColumn(
                name: "ChiPhiThucTe",
                table: "YeuCauBaoTri",
                newName: "ChiPhiThucTeYeuCau");

            migrationBuilder.RenameIndex(
                name: "IX_YeuCauBaoTri_HanXuLy",
                table: "YeuCauBaoTri",
                newName: "IX_YeuCauBaoTri_HanXuLyYeuCau");

            migrationBuilder.RenameColumn(
                name: "SoNgayAnHan",
                table: "ToaNha",
                newName: "SoNgayAnHanToaNha");

            migrationBuilder.RenameColumn(
                name: "PhanTramPhiTre",
                table: "ToaNha",
                newName: "PhanTramPhiTreToaNha");

            migrationBuilder.RenameColumn(
                name: "NgayChotKy",
                table: "ToaNha",
                newName: "NgayChotKyToaNha");

            migrationBuilder.RenameColumn(
                name: "MaGiaoDich",
                table: "ThanhToan",
                newName: "MaGiaoDichThanhToan");

            migrationBuilder.RenameIndex(
                name: "IX_ThanhToan_MaGiaoDich",
                table: "ThanhToan",
                newName: "IX_ThanhToan_MaGiaoDichThanhToan");

            migrationBuilder.RenameColumn(
                name: "DangApDung",
                table: "PhiDinhKy",
                newName: "DangApDungKhoanPhi");

            migrationBuilder.RenameColumn(
                name: "TrinhDuyet",
                table: "NhatKyHeThong",
                newName: "TrinhDuyetNhatKy");

            migrationBuilder.RenameColumn(
                name: "TenBang",
                table: "NhatKyHeThong",
                newName: "TenBangNhatKy");

            migrationBuilder.RenameColumn(
                name: "KhoaDong",
                table: "NhatKyHeThong",
                newName: "KhoaDongNhatKy");

            migrationBuilder.RenameColumn(
                name: "GiaTriMoi",
                table: "NhatKyHeThong",
                newName: "GiaTriMoiNhatKy");

            migrationBuilder.RenameColumn(
                name: "GiaTriCu",
                table: "NhatKyHeThong",
                newName: "GiaTriCuNhatKy");

            migrationBuilder.RenameColumn(
                name: "DiaChiIp",
                table: "NhatKyHeThong",
                newName: "DiaChiIpNhatKy");

            migrationBuilder.RenameIndex(
                name: "IX_NhatKyHeThong_TenBang_KhoaDong",
                table: "NhatKyHeThong",
                newName: "IX_NhatKyHeThong_TenBangNhatKy_KhoaDongNhatKy");

            migrationBuilder.RenameColumn(
                name: "DonViTinh",
                table: "LoaiDichVu",
                newName: "DonViTinhDichVu");

            migrationBuilder.RenameColumn(
                name: "TrangThaiTruoc",
                table: "LichSuYeuCau",
                newName: "TrangThaiTruocChuyen");

            migrationBuilder.RenameColumn(
                name: "TrangThaiSau",
                table: "LichSuYeuCau",
                newName: "TrangThaiSauChuyen");

            migrationBuilder.RenameColumn(
                name: "TongTien",
                table: "HoaDon",
                newName: "TongTienHoaDon");

            migrationBuilder.RenameColumn(
                name: "Thue",
                table: "HoaDon",
                newName: "ThueHoaDon");

            migrationBuilder.RenameColumn(
                name: "TamTinh",
                table: "HoaDon",
                newName: "TamTinhHoaDon");

            migrationBuilder.RenameColumn(
                name: "PhiTreHan",
                table: "HoaDon",
                newName: "PhiTreHanHoaDon");

            migrationBuilder.RenameColumn(
                name: "NgayPhatHanh",
                table: "HoaDon",
                newName: "NgayPhatHanhHoaDon");

            migrationBuilder.RenameColumn(
                name: "NgayDenHan",
                table: "HoaDon",
                newName: "NgayDenHanHoaDon");

            migrationBuilder.RenameColumn(
                name: "GiamGia",
                table: "HoaDon",
                newName: "GiamGiaHoaDon");

            migrationBuilder.RenameColumn(
                name: "DaThanhToan",
                table: "HoaDon",
                newName: "DaThanhToanHoaDon");

            migrationBuilder.RenameColumn(
                name: "ConLai",
                table: "HoaDon",
                newName: "ConLaiHoaDon");

            migrationBuilder.RenameIndex(
                name: "IX_HoaDon_NgayDenHan",
                table: "HoaDon",
                newName: "IX_HoaDon_NgayDenHanHoaDon");

            migrationBuilder.RenameColumn(
                name: "NgayVaoO",
                table: "CuTru",
                newName: "NgayBatDauCuTru");

            migrationBuilder.RenameColumn(
                name: "NgayChuyenDi",
                table: "CuTru",
                newName: "NgayKetThucCuTru");

            migrationBuilder.RenameColumn(
                name: "LaDaiDien",
                table: "CuTru",
                newName: "LaDaiDienCanHo");

            migrationBuilder.RenameColumn(
                name: "NgayTaiLen",
                table: "AnhYeuCau",
                newName: "NgayTaiLenAnh");

            migrationBuilder.AddCheckConstraint(
                name: "CK_YeuCauBaoTri_ChiPhi",
                table: "YeuCauBaoTri",
                sql: "\"ChiPhiUocTinhYeuCau\" >= 0 AND \"ChiPhiThucTeYeuCau\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_YeuCauBaoTri_DanhGia",
                table: "YeuCauBaoTri",
                sql: "\"DiemDanhGiaYeuCau\" IS NULL OR \"DiemDanhGiaYeuCau\" BETWEEN 1 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "CK_YeuCauBaoTri_HanXuLy",
                table: "YeuCauBaoTri",
                sql: "\"HanXuLyYeuCau\" IS NULL OR \"HanXuLyYeuCau\" >= \"NgayBaoSuCo\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_YeuCauBaoTri_KetQua",
                table: "YeuCauBaoTri",
                sql: "\"TrangThaiYeuCau\" NOT IN (3, 4) OR coalesce(btrim(\"KetQuaXuLyYeuCau\"), '') <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ToaNha_NgayChotKy",
                table: "ToaNha",
                sql: "\"NgayChotKyToaNha\" BETWEEN 1 AND 28");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ToaNha_PhiTre",
                table: "ToaNha",
                sql: "\"SoNgayAnHanToaNha\" >= 0 AND \"PhanTramPhiTreToaNha\" BETWEEN 0 AND 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HoaDon_CongThuc",
                table: "HoaDon",
                sql: "\"TongTienHoaDon\" = \"TamTinhHoaDon\" - \"GiamGiaHoaDon\" + \"ThueHoaDon\" + \"PhiTreHanHoaDon\" AND \"ConLaiHoaDon\" = \"TongTienHoaDon\" - \"DaThanhToanHoaDon\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HoaDon_ConLai",
                table: "HoaDon",
                sql: "\"ConLaiHoaDon\" >= 0 AND \"ConLaiHoaDon\" <= \"TongTienHoaDon\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HoaDon_HanThanhToan",
                table: "HoaDon",
                sql: "\"NgayDenHanHoaDon\" >= \"NgayPhatHanhHoaDon\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HoaDon_SoTien",
                table: "HoaDon",
                sql: "\"TongTienHoaDon\" >= 0 AND \"DaThanhToanHoaDon\" >= 0 AND \"GiamGiaHoaDon\" >= 0 AND \"ThueHoaDon\" >= 0 AND \"PhiTreHanHoaDon\" >= 0");

            migrationBuilder.CreateIndex(
                name: "UX_CuTru_DaiDien",
                table: "CuTru",
                column: "CanHoId",
                unique: true,
                filter: "\"LaDaiDienCanHo\" AND \"NgayKetThucCuTru\" IS NULL AND \"DaXoa\" = false");

            migrationBuilder.CreateIndex(
                name: "UX_CuTru_DangO",
                table: "CuTru",
                columns: new[] { "CanHoId", "NguoiDungId" },
                unique: true,
                filter: "\"NgayKetThucCuTru\" IS NULL AND \"DaXoa\" = false");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CuTru_NgayChuyenDi",
                table: "CuTru",
                sql: "\"NgayKetThucCuTru\" IS NULL OR \"NgayKetThucCuTru\" >= \"NgayBatDauCuTru\"");

            // Thân hàm plpgsql lưu tên cột dạng chữ nên phải tạo lại; khung nhìn tạo lại để tên cột đầu ra theo tên mới.
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION fn_kiem_tra_tong_thanh_toan() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE da_thu numeric; tong numeric;
BEGIN
  SELECT coalesce(sum(""SoTienThanhToan"" - ""SoTienHoanTra""), 0) INTO da_thu
  FROM ""ThanhToan"" WHERE ""HoaDonId"" = NEW.""HoaDonId"" AND NOT ""DaXoa"" AND ""TrangThaiThanhToan"" IN (1, 3, 4);
  SELECT ""TongTienHoaDon"" INTO tong FROM ""HoaDon"" WHERE ""HoaDonId"" = NEW.""HoaDonId"";
  IF da_thu > tong THEN
    RAISE EXCEPTION 'Tổng đã thu % vượt tổng tiền % của hoá đơn %', da_thu, tong, NEW.""HoaDonId"";
  END IF;
  RETURN NULL;
END $$;
DROP VIEW IF EXISTS vw_doi_chieu_hoa_don;
CREATE VIEW vw_doi_chieu_hoa_don AS
SELECT h.""HoaDonId"", h.""SoHoaDon"", h.""TamTinhHoaDon"", h.""DaThanhToanHoaDon"", h.""ConLaiHoaDon"",
       coalesce((SELECT sum(ct.""ThanhTienKhoan"") FROM ""ChiTietHoaDon"" ct
                 WHERE ct.""HoaDonId"" = h.""HoaDonId"" AND NOT ct.""DaXoa""), 0) AS ""TamTinhTinhLai"",
       coalesce((SELECT sum(t.""SoTienThanhToan"" - t.""SoTienHoanTra"") FROM ""ThanhToan"" t
                 WHERE t.""HoaDonId"" = h.""HoaDonId"" AND NOT t.""DaXoa"" AND t.""TrangThaiThanhToan"" IN (1, 3, 4)), 0)
         AS ""DaThanhToanTinhLai""
FROM ""HoaDon"" h WHERE NOT h.""DaXoa"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                name: "CK_YeuCauBaoTri_KetQua",
                table: "YeuCauBaoTri");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ToaNha_NgayChotKy",
                table: "ToaNha");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ToaNha_PhiTre",
                table: "ToaNha");

            migrationBuilder.DropCheckConstraint(
                name: "CK_HoaDon_CongThuc",
                table: "HoaDon");

            migrationBuilder.DropCheckConstraint(
                name: "CK_HoaDon_ConLai",
                table: "HoaDon");

            migrationBuilder.DropCheckConstraint(
                name: "CK_HoaDon_HanThanhToan",
                table: "HoaDon");

            migrationBuilder.DropCheckConstraint(
                name: "CK_HoaDon_SoTien",
                table: "HoaDon");

            migrationBuilder.DropIndex(
                name: "UX_CuTru_DaiDien",
                table: "CuTru");

            migrationBuilder.DropIndex(
                name: "UX_CuTru_DangO",
                table: "CuTru");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CuTru_NgayChuyenDi",
                table: "CuTru");

            migrationBuilder.RenameColumn(
                name: "KetQuaXuLyYeuCau",
                table: "YeuCauBaoTri",
                newName: "KetQuaXuLy");

            migrationBuilder.RenameColumn(
                name: "HanXuLyYeuCau",
                table: "YeuCauBaoTri",
                newName: "HanXuLy");

            migrationBuilder.RenameColumn(
                name: "DiemDanhGiaYeuCau",
                table: "YeuCauBaoTri",
                newName: "DiemDanhGia");

            migrationBuilder.RenameColumn(
                name: "ChiPhiUocTinhYeuCau",
                table: "YeuCauBaoTri",
                newName: "ChiPhiUocTinh");

            migrationBuilder.RenameColumn(
                name: "ChiPhiThucTeYeuCau",
                table: "YeuCauBaoTri",
                newName: "ChiPhiThucTe");

            migrationBuilder.RenameIndex(
                name: "IX_YeuCauBaoTri_HanXuLyYeuCau",
                table: "YeuCauBaoTri",
                newName: "IX_YeuCauBaoTri_HanXuLy");

            migrationBuilder.RenameColumn(
                name: "SoNgayAnHanToaNha",
                table: "ToaNha",
                newName: "SoNgayAnHan");

            migrationBuilder.RenameColumn(
                name: "PhanTramPhiTreToaNha",
                table: "ToaNha",
                newName: "PhanTramPhiTre");

            migrationBuilder.RenameColumn(
                name: "NgayChotKyToaNha",
                table: "ToaNha",
                newName: "NgayChotKy");

            migrationBuilder.RenameColumn(
                name: "MaGiaoDichThanhToan",
                table: "ThanhToan",
                newName: "MaGiaoDich");

            migrationBuilder.RenameIndex(
                name: "IX_ThanhToan_MaGiaoDichThanhToan",
                table: "ThanhToan",
                newName: "IX_ThanhToan_MaGiaoDich");

            migrationBuilder.RenameColumn(
                name: "DangApDungKhoanPhi",
                table: "PhiDinhKy",
                newName: "DangApDung");

            migrationBuilder.RenameColumn(
                name: "TrinhDuyetNhatKy",
                table: "NhatKyHeThong",
                newName: "TrinhDuyet");

            migrationBuilder.RenameColumn(
                name: "TenBangNhatKy",
                table: "NhatKyHeThong",
                newName: "TenBang");

            migrationBuilder.RenameColumn(
                name: "KhoaDongNhatKy",
                table: "NhatKyHeThong",
                newName: "KhoaDong");

            migrationBuilder.RenameColumn(
                name: "GiaTriMoiNhatKy",
                table: "NhatKyHeThong",
                newName: "GiaTriMoi");

            migrationBuilder.RenameColumn(
                name: "GiaTriCuNhatKy",
                table: "NhatKyHeThong",
                newName: "GiaTriCu");

            migrationBuilder.RenameColumn(
                name: "DiaChiIpNhatKy",
                table: "NhatKyHeThong",
                newName: "DiaChiIp");

            migrationBuilder.RenameIndex(
                name: "IX_NhatKyHeThong_TenBangNhatKy_KhoaDongNhatKy",
                table: "NhatKyHeThong",
                newName: "IX_NhatKyHeThong_TenBang_KhoaDong");

            migrationBuilder.RenameColumn(
                name: "DonViTinhDichVu",
                table: "LoaiDichVu",
                newName: "DonViTinh");

            migrationBuilder.RenameColumn(
                name: "TrangThaiTruocChuyen",
                table: "LichSuYeuCau",
                newName: "TrangThaiTruoc");

            migrationBuilder.RenameColumn(
                name: "TrangThaiSauChuyen",
                table: "LichSuYeuCau",
                newName: "TrangThaiSau");

            migrationBuilder.RenameColumn(
                name: "TongTienHoaDon",
                table: "HoaDon",
                newName: "TongTien");

            migrationBuilder.RenameColumn(
                name: "ThueHoaDon",
                table: "HoaDon",
                newName: "Thue");

            migrationBuilder.RenameColumn(
                name: "TamTinhHoaDon",
                table: "HoaDon",
                newName: "TamTinh");

            migrationBuilder.RenameColumn(
                name: "PhiTreHanHoaDon",
                table: "HoaDon",
                newName: "PhiTreHan");

            migrationBuilder.RenameColumn(
                name: "NgayPhatHanhHoaDon",
                table: "HoaDon",
                newName: "NgayPhatHanh");

            migrationBuilder.RenameColumn(
                name: "NgayDenHanHoaDon",
                table: "HoaDon",
                newName: "NgayDenHan");

            migrationBuilder.RenameColumn(
                name: "GiamGiaHoaDon",
                table: "HoaDon",
                newName: "GiamGia");

            migrationBuilder.RenameColumn(
                name: "DaThanhToanHoaDon",
                table: "HoaDon",
                newName: "DaThanhToan");

            migrationBuilder.RenameColumn(
                name: "ConLaiHoaDon",
                table: "HoaDon",
                newName: "ConLai");

            migrationBuilder.RenameIndex(
                name: "IX_HoaDon_NgayDenHanHoaDon",
                table: "HoaDon",
                newName: "IX_HoaDon_NgayDenHan");

            migrationBuilder.RenameColumn(
                name: "NgayKetThucCuTru",
                table: "CuTru",
                newName: "NgayChuyenDi");

            migrationBuilder.RenameColumn(
                name: "NgayBatDauCuTru",
                table: "CuTru",
                newName: "NgayVaoO");

            migrationBuilder.RenameColumn(
                name: "LaDaiDienCanHo",
                table: "CuTru",
                newName: "LaDaiDien");

            migrationBuilder.RenameColumn(
                name: "NgayTaiLenAnh",
                table: "AnhYeuCau",
                newName: "NgayTaiLen");

            migrationBuilder.AddCheckConstraint(
                name: "CK_YeuCauBaoTri_ChiPhi",
                table: "YeuCauBaoTri",
                sql: "\"ChiPhiUocTinh\" >= 0 AND \"ChiPhiThucTe\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_YeuCauBaoTri_DanhGia",
                table: "YeuCauBaoTri",
                sql: "\"DiemDanhGia\" IS NULL OR \"DiemDanhGia\" BETWEEN 1 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "CK_YeuCauBaoTri_HanXuLy",
                table: "YeuCauBaoTri",
                sql: "\"HanXuLy\" IS NULL OR \"HanXuLy\" >= \"NgayBaoSuCo\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_YeuCauBaoTri_KetQua",
                table: "YeuCauBaoTri",
                sql: "\"TrangThaiYeuCau\" NOT IN (3, 4) OR coalesce(btrim(\"KetQuaXuLy\"), '') <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ToaNha_NgayChotKy",
                table: "ToaNha",
                sql: "\"NgayChotKy\" BETWEEN 1 AND 28");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ToaNha_PhiTre",
                table: "ToaNha",
                sql: "\"SoNgayAnHan\" >= 0 AND \"PhanTramPhiTre\" BETWEEN 0 AND 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HoaDon_CongThuc",
                table: "HoaDon",
                sql: "\"TongTien\" = \"TamTinh\" - \"GiamGia\" + \"Thue\" + \"PhiTreHan\" AND \"ConLai\" = \"TongTien\" - \"DaThanhToan\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HoaDon_ConLai",
                table: "HoaDon",
                sql: "\"ConLai\" >= 0 AND \"ConLai\" <= \"TongTien\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HoaDon_HanThanhToan",
                table: "HoaDon",
                sql: "\"NgayDenHan\" >= \"NgayPhatHanh\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HoaDon_SoTien",
                table: "HoaDon",
                sql: "\"TongTien\" >= 0 AND \"DaThanhToan\" >= 0 AND \"GiamGia\" >= 0 AND \"Thue\" >= 0 AND \"PhiTreHan\" >= 0");

            migrationBuilder.CreateIndex(
                name: "UX_CuTru_DaiDien",
                table: "CuTru",
                column: "CanHoId",
                unique: true,
                filter: "\"LaDaiDien\" AND \"NgayChuyenDi\" IS NULL AND \"DaXoa\" = false");

            migrationBuilder.CreateIndex(
                name: "UX_CuTru_DangO",
                table: "CuTru",
                columns: new[] { "CanHoId", "NguoiDungId" },
                unique: true,
                filter: "\"NgayChuyenDi\" IS NULL AND \"DaXoa\" = false");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CuTru_NgayChuyenDi",
                table: "CuTru",
                sql: "\"NgayChuyenDi\" IS NULL OR \"NgayChuyenDi\" >= \"NgayVaoO\"");

            // Thân hàm plpgsql lưu tên cột dạng chữ nên phải tạo lại; khung nhìn tạo lại để tên cột đầu ra theo tên mới.
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION fn_kiem_tra_tong_thanh_toan() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE da_thu numeric; tong numeric;
BEGIN
  SELECT coalesce(sum(""SoTienThanhToan"" - ""SoTienHoanTra""), 0) INTO da_thu
  FROM ""ThanhToan"" WHERE ""HoaDonId"" = NEW.""HoaDonId"" AND NOT ""DaXoa"" AND ""TrangThaiThanhToan"" IN (1, 3, 4);
  SELECT ""TongTien"" INTO tong FROM ""HoaDon"" WHERE ""HoaDonId"" = NEW.""HoaDonId"";
  IF da_thu > tong THEN
    RAISE EXCEPTION 'Tổng đã thu % vượt tổng tiền % của hoá đơn %', da_thu, tong, NEW.""HoaDonId"";
  END IF;
  RETURN NULL;
END $$;
DROP VIEW IF EXISTS vw_doi_chieu_hoa_don;
CREATE VIEW vw_doi_chieu_hoa_don AS
SELECT h.""HoaDonId"", h.""SoHoaDon"", h.""TamTinh"", h.""DaThanhToan"", h.""ConLai"",
       coalesce((SELECT sum(ct.""ThanhTienKhoan"") FROM ""ChiTietHoaDon"" ct
                 WHERE ct.""HoaDonId"" = h.""HoaDonId"" AND NOT ct.""DaXoa""), 0) AS ""TamTinhTinhLai"",
       coalesce((SELECT sum(t.""SoTienThanhToan"" - t.""SoTienHoanTra"") FROM ""ThanhToan"" t
                 WHERE t.""HoaDonId"" = h.""HoaDonId"" AND NOT t.""DaXoa"" AND t.""TrangThaiThanhToan"" IN (1, 3, 4)), 0)
         AS ""DaThanhToanTinhLai""
FROM ""HoaDon"" h WHERE NOT h.""DaXoa"";");
        }
    }
}
