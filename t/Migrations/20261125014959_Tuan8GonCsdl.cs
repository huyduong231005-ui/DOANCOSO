using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace t.Migrations
{
    /// <inheritdoc />
    public partial class Tuan8GonCsdl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Khung nhìn đối chiếu đọc các cột sắp đổi thành cột tự tính: bỏ trước, tạo lại ở cuối.
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_doi_chieu_hoa_don;");

            migrationBuilder.DropForeignKey(
                name: "FK_AnhYeuCau_NguoiDung_NguoiTaiLenId",
                table: "AnhYeuCau");

            migrationBuilder.DropForeignKey(
                name: "FK_DuAn_KhuVuc_KhuVucId",
                table: "DuAn");

            migrationBuilder.DropForeignKey(
                name: "FK_ToaNha_DuAn_DuAnId",
                table: "ToaNha");

            migrationBuilder.DropForeignKey(
                name: "FK_YeuCauBaoTri_NguoiDung_NguoiBaoCaoId",
                table: "YeuCauBaoTri");

            migrationBuilder.DropTable(
                name: "KhuVuc");

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
                name: "CK_YeuCauBaoTri_NoiXayRa",
                table: "YeuCauBaoTri");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ToaNha_DonGiaPhiQuanLy",
                table: "ToaNha");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ToaNha_NgayChotKy",
                table: "ToaNha");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ToaNha_PhiTre",
                table: "ToaNha");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ThanhToan_HoanTra",
                table: "ThanhToan");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ThanhToan_MaSo",
                table: "ThanhToan");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ThanhToan_SoTien",
                table: "ThanhToan");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PhiDinhKy_SoTien",
                table: "PhiDinhKy");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LoaiDichVu_DonGia",
                table: "LoaiDichVu");

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
                name: "CK_HoaDon_Ky",
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

            migrationBuilder.DropCheckConstraint(
                name: "CK_ChiTietHoaDon_SoTien",
                table: "ChiTietHoaDon");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ChiTietHoaDon_ThanhTien",
                table: "ChiTietHoaDon");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ChiSoDichVu_Ky",
                table: "ChiSoDichVu");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CanHo_HuongNha",
                table: "CanHo");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CanHo_SoPhong",
                table: "CanHo");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AnhYeuCau_GiaiDoan",
                table: "AnhYeuCau");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DuAn",
                table: "DuAn");

            migrationBuilder.DropIndex(
                name: "IX_DuAn_KhuVucId",
                table: "DuAn");

            migrationBuilder.DropColumn(
                name: "ChiPhiUocTinhYeuCau",
                table: "YeuCauBaoTri");

            migrationBuilder.DropColumn(
                name: "AnhDaiDienToaNha",
                table: "ToaNha");

            migrationBuilder.DropColumn(
                name: "MoTaToaNha",
                table: "ToaNha");

            migrationBuilder.DropColumn(
                name: "TenToaNha",
                table: "ToaNha");

            migrationBuilder.DropColumn(
                name: "DonViThanhToan",
                table: "ThanhToan");

            migrationBuilder.DropColumn(
                name: "ThuTuKhoanPhi",
                table: "PhiDinhKy");

            migrationBuilder.DropColumn(
                name: "CotThayDoi",
                table: "NhatKyHeThong");

            migrationBuilder.DropColumn(
                name: "TenNguoiGhi",
                table: "NhatKyHeThong");

            migrationBuilder.DropColumn(
                name: "TrinhDuyetNhatKy",
                table: "NhatKyHeThong");

            migrationBuilder.DropColumn(
                name: "AnhDaiDienNguoiDung",
                table: "NguoiDung");

            migrationBuilder.DropColumn(
                name: "NgayDangNhapCuoi",
                table: "NguoiDung");

            migrationBuilder.DropColumn(
                name: "TrangThaiTruocChuyen",
                table: "LichSuYeuCau");

            migrationBuilder.DropColumn(
                name: "GiamGiaHoaDon",
                table: "HoaDon");

            migrationBuilder.DropColumn(
                name: "ThueHoaDon",
                table: "HoaDon");

            migrationBuilder.DropColumn(
                name: "GhiChuCuTru",
                table: "CuTru");

            migrationBuilder.DropColumn(
                name: "ThuTuKhoan",
                table: "ChiTietHoaDon");

            migrationBuilder.DropColumn(
                name: "GhiChuChiSo",
                table: "ChiSoDichVu");

            migrationBuilder.DropColumn(
                name: "HuongNha",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "MoTaCanHo",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "SoPhongTam",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "AnhDaiDienDuAn",
                table: "DuAn");

            migrationBuilder.DropColumn(
                name: "KhuVucId",
                table: "DuAn");

            migrationBuilder.DropColumn(
                name: "MoTaDuAn",
                table: "DuAn");

            migrationBuilder.RenameTable(
                name: "DuAn",
                newName: "Khu");

            migrationBuilder.RenameColumn(
                name: "ViTriKhuVucChung",
                table: "YeuCauBaoTri",
                newName: "ViTri");

            migrationBuilder.RenameColumn(
                name: "TieuDeYeuCau",
                table: "YeuCauBaoTri",
                newName: "TieuDe");

            migrationBuilder.RenameColumn(
                name: "NhanXetCuDan",
                table: "YeuCauBaoTri",
                newName: "NhanXet");

            migrationBuilder.RenameColumn(
                name: "NguoiBaoCaoId",
                table: "YeuCauBaoTri",
                newName: "NguoiBaoId");

            migrationBuilder.RenameColumn(
                name: "NgayBaoSuCo",
                table: "YeuCauBaoTri",
                newName: "NgayBao");

            migrationBuilder.RenameColumn(
                name: "MoTaYeuCau",
                table: "YeuCauBaoTri",
                newName: "MoTa");

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
                name: "ChiPhiThucTeYeuCau",
                table: "YeuCauBaoTri",
                newName: "ChiPhiSuaChua");

            migrationBuilder.RenameIndex(
                name: "IX_YeuCauBaoTri_NguoiBaoCaoId",
                table: "YeuCauBaoTri",
                newName: "IX_YeuCauBaoTri_NguoiBaoId");

            migrationBuilder.RenameIndex(
                name: "IX_YeuCauBaoTri_NgayBaoSuCo",
                table: "YeuCauBaoTri",
                newName: "IX_YeuCauBaoTri_NgayBao");

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
                newName: "TiLePhiTre");

            migrationBuilder.RenameColumn(
                name: "NgayChotKyToaNha",
                table: "ToaNha",
                newName: "NgayLapPhi");

            migrationBuilder.RenameColumn(
                name: "DuAnId",
                table: "ToaNha",
                newName: "KhuId");

            migrationBuilder.RenameColumn(
                name: "DonGiaPhiQuanLy",
                table: "ToaNha",
                newName: "PhiQuanLyM2");

            migrationBuilder.RenameIndex(
                name: "IX_ToaNha_DuAnId",
                table: "ToaNha",
                newName: "IX_ToaNha_KhuId");

            migrationBuilder.RenameColumn(
                name: "SoTienThanhToan",
                table: "ThanhToan",
                newName: "SoTien");

            migrationBuilder.RenameColumn(
                name: "PhuongThucThanhToan",
                table: "ThanhToan",
                newName: "PhuongThuc");

            migrationBuilder.RenameColumn(
                name: "MaGiaoDichThanhToan",
                table: "ThanhToan",
                newName: "MaGiaoDich");

            migrationBuilder.RenameColumn(
                name: "GhiChuThanhToan",
                table: "ThanhToan",
                newName: "GhiChu");

            migrationBuilder.RenameIndex(
                name: "IX_ThanhToan_MaGiaoDichThanhToan",
                table: "ThanhToan",
                newName: "IX_ThanhToan_MaGiaoDich");

            migrationBuilder.RenameColumn(
                name: "PhanHeQuyen",
                table: "Quyen",
                newName: "PhanHe");

            migrationBuilder.RenameIndex(
                name: "IX_Quyen_PhanHeQuyen",
                table: "Quyen",
                newName: "IX_Quyen_PhanHe");

            migrationBuilder.RenameColumn(
                name: "TenKhoanPhi",
                table: "PhiDinhKy",
                newName: "TenPhi");

            migrationBuilder.RenameColumn(
                name: "SoTienKhoanPhi",
                table: "PhiDinhKy",
                newName: "SoTien");

            migrationBuilder.RenameColumn(
                name: "DangApDungKhoanPhi",
                table: "PhiDinhKy",
                newName: "DangApDung");

            migrationBuilder.RenameColumn(
                name: "ThoiGianGhi",
                table: "NhatKyHeThong",
                newName: "ThoiDiem");

            migrationBuilder.RenameColumn(
                name: "TenBangNhatKy",
                table: "NhatKyHeThong",
                newName: "TenBang");

            migrationBuilder.RenameColumn(
                name: "NguoiGhiId",
                table: "NhatKyHeThong",
                newName: "NguoiThaoTacId");

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
                name: "IX_NhatKyHeThong_ThoiGianGhi",
                table: "NhatKyHeThong",
                newName: "IX_NhatKyHeThong_ThoiDiem");

            migrationBuilder.RenameIndex(
                name: "IX_NhatKyHeThong_TenBangNhatKy_KhoaDongNhatKy",
                table: "NhatKyHeThong",
                newName: "IX_NhatKyHeThong_TenBang_KhoaDong");

            migrationBuilder.RenameIndex(
                name: "IX_NhatKyHeThong_NguoiGhiId",
                table: "NhatKyHeThong",
                newName: "IX_NhatKyHeThong_NguoiThaoTacId");

            migrationBuilder.RenameColumn(
                name: "DonViTinhDichVu",
                table: "LoaiDichVu",
                newName: "DonViTinh");

            migrationBuilder.RenameColumn(
                name: "DonGiaDichVu",
                table: "LoaiDichVu",
                newName: "DonGia");

            migrationBuilder.RenameColumn(
                name: "TrangThaiSauChuyen",
                table: "LichSuYeuCau",
                newName: "TrangThaiMoi");

            migrationBuilder.RenameColumn(
                name: "ThoiDiemChuyen",
                table: "LichSuYeuCau",
                newName: "ThoiDiem");

            migrationBuilder.RenameColumn(
                name: "GhiChuChuyen",
                table: "LichSuYeuCau",
                newName: "GhiChu");

            migrationBuilder.RenameIndex(
                name: "IX_LichSuYeuCau_YeuCauBaoTriId_ThoiDiemChuyen",
                table: "LichSuYeuCau",
                newName: "IX_LichSuYeuCau_YeuCauBaoTriId_ThoiDiem");

            migrationBuilder.RenameColumn(
                name: "TongTienHoaDon",
                table: "HoaDon",
                newName: "TongTien");

            migrationBuilder.RenameColumn(
                name: "TamTinhHoaDon",
                table: "HoaDon",
                newName: "TienPhi");

            migrationBuilder.RenameColumn(
                name: "PhiTreHanHoaDon",
                table: "HoaDon",
                newName: "PhiTreHan");

            migrationBuilder.RenameColumn(
                name: "NgayPhatHanhHoaDon",
                table: "HoaDon",
                newName: "NgayLap");

            migrationBuilder.RenameColumn(
                name: "NgayDenHanHoaDon",
                table: "HoaDon",
                newName: "HanThanhToan");

            migrationBuilder.RenameColumn(
                name: "KyHoaDon",
                table: "HoaDon",
                newName: "Ky");

            migrationBuilder.RenameColumn(
                name: "GhiChuHoaDon",
                table: "HoaDon",
                newName: "GhiChu");

            migrationBuilder.RenameColumn(
                name: "DaThanhToanHoaDon",
                table: "HoaDon",
                newName: "DaThu");

            migrationBuilder.RenameColumn(
                name: "ConLaiHoaDon",
                table: "HoaDon",
                newName: "ConNo");

            migrationBuilder.RenameIndex(
                name: "IX_HoaDon_NgayDenHanHoaDon",
                table: "HoaDon",
                newName: "IX_HoaDon_HanThanhToan");

            migrationBuilder.RenameIndex(
                name: "IX_HoaDon_CanHoId_KyHoaDon_LoaiHoaDon",
                table: "HoaDon",
                newName: "IX_HoaDon_CanHoId_Ky_LoaiHoaDon");

            migrationBuilder.RenameColumn(
                name: "NgayXacNhanCuTru",
                table: "CuTru",
                newName: "NgayXacNhan");

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
                name: "ThanhTienKhoan",
                table: "ChiTietHoaDon",
                newName: "ThanhTien");

            migrationBuilder.RenameColumn(
                name: "SoLuongKhoan",
                table: "ChiTietHoaDon",
                newName: "SoLuong");

            migrationBuilder.RenameColumn(
                name: "NoiDungKhoan",
                table: "ChiTietHoaDon",
                newName: "NoiDung");

            migrationBuilder.RenameColumn(
                name: "DonGiaKhoan",
                table: "ChiTietHoaDon",
                newName: "DonGia");

            migrationBuilder.RenameColumn(
                name: "NgayDocChiSo",
                table: "ChiSoDichVu",
                newName: "NgayDoc");

            migrationBuilder.RenameColumn(
                name: "KyChiSo",
                table: "ChiSoDichVu",
                newName: "Ky");

            migrationBuilder.RenameIndex(
                name: "IX_ChiSoDichVu_KyChiSo",
                table: "ChiSoDichVu",
                newName: "IX_ChiSoDichVu_Ky");

            migrationBuilder.RenameIndex(
                name: "IX_ChiSoDichVu_CanHoId_LoaiDichVuId_KyChiSo",
                table: "ChiSoDichVu",
                newName: "IX_ChiSoDichVu_CanHoId_LoaiDichVuId_Ky");

            migrationBuilder.RenameColumn(
                name: "NguoiTaiLenId",
                table: "AnhYeuCau",
                newName: "NguoiTaiId");

            migrationBuilder.RenameColumn(
                name: "NgayTaiLenAnh",
                table: "AnhYeuCau",
                newName: "NgayTai");

            migrationBuilder.RenameColumn(
                name: "GiaiDoanAnh",
                table: "AnhYeuCau",
                newName: "GiaiDoan");

            migrationBuilder.RenameColumn(
                name: "DuongDanAnh",
                table: "AnhYeuCau",
                newName: "DuongDan");

            migrationBuilder.RenameIndex(
                name: "IX_AnhYeuCau_NguoiTaiLenId",
                table: "AnhYeuCau",
                newName: "IX_AnhYeuCau_NguoiTaiId");

            migrationBuilder.RenameColumn(
                name: "TenDuAn",
                table: "Khu",
                newName: "TenKhu");

            migrationBuilder.RenameColumn(
                name: "DiaChiDuAn",
                table: "Khu",
                newName: "DiaChi");

            migrationBuilder.RenameColumn(
                name: "DuAnId",
                table: "Khu",
                newName: "KhuId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Khu",
                table: "Khu",
                column: "KhuId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_YeuCauBaoTri_ChiPhi",
                table: "YeuCauBaoTri",
                sql: "\"ChiPhiSuaChua\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_YeuCauBaoTri_DanhGia",
                table: "YeuCauBaoTri",
                sql: "\"DiemDanhGia\" IS NULL OR \"DiemDanhGia\" BETWEEN 1 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "CK_YeuCauBaoTri_HanXuLy",
                table: "YeuCauBaoTri",
                sql: "\"HanXuLy\" IS NULL OR \"HanXuLy\" >= \"NgayBao\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_YeuCauBaoTri_KetQua",
                table: "YeuCauBaoTri",
                sql: "\"TrangThaiYeuCau\" NOT IN (3, 4) OR coalesce(btrim(\"KetQuaXuLy\"), '') <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_YeuCauBaoTri_NoiXayRa",
                table: "YeuCauBaoTri",
                sql: "(\"CanHoId\" IS NOT NULL AND \"ToaNhaId\" IS NULL) OR (\"CanHoId\" IS NULL AND \"ToaNhaId\" IS NOT NULL AND \"ViTri\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ToaNha_NgayLapPhi",
                table: "ToaNha",
                sql: "\"NgayLapPhi\" BETWEEN 1 AND 28");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ToaNha_PhiQuanLyM2",
                table: "ToaNha",
                sql: "\"PhiQuanLyM2\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ToaNha_PhiTre",
                table: "ToaNha",
                sql: "\"SoNgayAnHan\" >= 0 AND \"TiLePhiTre\" BETWEEN 0 AND 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ThanhToan_HoanTra",
                table: "ThanhToan",
                sql: "\"SoTienHoanTra\" >= 0 AND \"SoTienHoanTra\" <= \"SoTien\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ThanhToan_PhuongThuc",
                table: "ThanhToan",
                sql: "\"PhuongThuc\" BETWEEN 0 AND 4");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ThanhToan_SoTien",
                table: "ThanhToan",
                sql: "\"SoTien\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ThanhToan_TrangThai",
                table: "ThanhToan",
                sql: "\"TrangThaiThanhToan\" BETWEEN 0 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PhiDinhKy_SoTien",
                table: "PhiDinhKy",
                sql: "\"SoTien\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LoaiDichVu_DonGia",
                table: "LoaiDichVu",
                sql: "\"DonGia\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HoaDon_DaThu",
                table: "HoaDon",
                sql: "\"DaThu\" <= \"TienPhi\" + \"PhiTreHan\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HoaDon_HanThanhToan",
                table: "HoaDon",
                sql: "\"HanThanhToan\" >= \"NgayLap\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HoaDon_Ky",
                table: "HoaDon",
                sql: "\"Ky\" % 100 BETWEEN 1 AND 12");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HoaDon_SoTien",
                table: "HoaDon",
                sql: "\"TienPhi\" >= 0 AND \"PhiTreHan\" >= 0 AND \"DaThu\" >= 0");

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

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChiTietHoaDon_SoTien",
                table: "ChiTietHoaDon",
                sql: "\"SoLuong\" > 0 AND \"DonGia\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChiSoDichVu_Ky",
                table: "ChiSoDichVu",
                sql: "\"Ky\" % 100 BETWEEN 1 AND 12");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CanHo_SoPhongNgu",
                table: "CanHo",
                sql: "\"SoPhongNgu\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AnhYeuCau_GiaiDoan",
                table: "AnhYeuCau",
                sql: "\"GiaiDoan\" IN (0, 1)");

            migrationBuilder.AddForeignKey(
                name: "FK_AnhYeuCau_NguoiDung_NguoiTaiId",
                table: "AnhYeuCau",
                column: "NguoiTaiId",
                principalTable: "NguoiDung",
                principalColumn: "NguoiDungId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ToaNha_Khu_KhuId",
                table: "ToaNha",
                column: "KhuId",
                principalTable: "Khu",
                principalColumn: "KhuId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_YeuCauBaoTri_NguoiDung_NguoiBaoId",
                table: "YeuCauBaoTri",
                column: "NguoiBaoId",
                principalTable: "NguoiDung",
                principalColumn: "NguoiDungId",
                onDelete: ReferentialAction.Restrict);

            // Tổng tiền, còn nợ, thành tiền do CSDL tự tính (cột sinh, STORED): không ghi tay nên không thể lệch.
            migrationBuilder.Sql(@"
ALTER TABLE ""HoaDon"" DROP COLUMN ""TongTien"", DROP COLUMN ""ConNo"";
ALTER TABLE ""HoaDon""
  ADD COLUMN ""TongTien"" numeric(18,0) GENERATED ALWAYS AS (""TienPhi"" + ""PhiTreHan"") STORED,
  ADD COLUMN ""ConNo"" numeric(18,0) GENERATED ALWAYS AS (""TienPhi"" + ""PhiTreHan"" - ""DaThu"") STORED;
ALTER TABLE ""ChiTietHoaDon"" DROP COLUMN ""ThanhTien"";
ALTER TABLE ""ChiTietHoaDon""
  ADD COLUMN ""ThanhTien"" numeric(18,0) GENERATED ALWAYS AS (round(""SoLuong"" * ""DonGia"")) STORED;");

            // Thân hàm plpgsql lưu tên cột dạng chữ nên phải tạo lại theo tên mới.
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION fn_kiem_tra_tong_thanh_toan() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE da_thu numeric; tong numeric;
BEGIN
  SELECT coalesce(sum(""SoTien"" - ""SoTienHoanTra""), 0) INTO da_thu
  FROM ""ThanhToan"" WHERE ""HoaDonId"" = NEW.""HoaDonId"" AND NOT ""DaXoa"" AND ""TrangThaiThanhToan"" IN (1, 3, 4);
  SELECT ""TongTien"" INTO tong FROM ""HoaDon"" WHERE ""HoaDonId"" = NEW.""HoaDonId"";
  IF da_thu > tong THEN
    RAISE EXCEPTION 'Tổng đã thu % vượt tổng tiền % của hoá đơn %', da_thu, tong, NEW.""HoaDonId"";
  END IF;
  RETURN NULL;
END $$;

CREATE VIEW vw_doi_chieu_hoa_don AS
SELECT h.""HoaDonId"", h.""SoHoaDon"", h.""TienPhi"", h.""DaThu"",
       coalesce((SELECT sum(ct.""ThanhTien"") FROM ""ChiTietHoaDon"" ct
                 WHERE ct.""HoaDonId"" = h.""HoaDonId"" AND NOT ct.""DaXoa""), 0) AS ""TienPhiTinhLai"",
       coalesce((SELECT sum(t.""SoTien"" - t.""SoTienHoanTra"") FROM ""ThanhToan"" t
                 WHERE t.""HoaDonId"" = h.""HoaDonId"" AND NOT t.""DaXoa"" AND t.""TrangThaiThanhToan"" IN (1, 3, 4)), 0) AS ""DaThuTinhLai""
FROM ""HoaDon"" h WHERE NOT h.""DaXoa"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ponytail: không viết bước lùi cho lần gọn lược đồ này; khôi phục bằng db_backup/LuxeHavenBql_truoc_gon_csdl_20261125.dump.
            throw new NotSupportedException("Khôi phục bằng bản sao lưu db_backup/LuxeHavenBql_truoc_gon_csdl_20261125.dump");
        }
    }
}
