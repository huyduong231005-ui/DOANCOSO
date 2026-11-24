using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace t.Migrations
{
    /// <inheritdoc />
    public partial class Tuan7LuocDoTheoGopYGv : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Chuyển dữ liệu trước khi bỏ cột: mỗi người cư trú phải có một tài khoản (NguoiDung) ──
            // Người BQL ghi nhận mà chưa có tài khoản được tạo tài khoản chưa đặt mật khẩu (kích hoạt qua thư mời).
            migrationBuilder.Sql("""
                CREATE TEMP TABLE tmp_cu_tru ON COMMIT DROP AS
                SELECT c."Id" AS cudan_id, coalesce(c."NguoiDungId", gen_random_uuid()::text) AS uid,
                       c."NguoiDungId" IS NULL AS moi, c."HoTen", c."SoDienThoai", c."Email", c."SoGiayTo", c."NgayTao"
                FROM "CuDan" c;

                INSERT INTO "NguoiDung" ("Id", "HoTen", "SoDienThoai", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
                    "EmailConfirmed", "SecurityStamp", "ConcurrencyStamp", "PhoneNumberConfirmed", "TwoFactorEnabled",
                    "LockoutEnabled", "AccessFailedCount", "NgayTao", "DaXoa")
                SELECT uid, "HoTen", "SoDienThoai", coalesce("Email", 'cudan-' || cudan_id), upper(coalesce("Email", 'cudan-' || cudan_id)),
                       "Email", upper("Email"), false, replace(gen_random_uuid()::text, '-', ''), gen_random_uuid()::text,
                       false, false, true, 0, "NgayTao", false
                FROM tmp_cu_tru WHERE moi;

                UPDATE "CuDan" c SET "NguoiDungId" = t.uid FROM tmp_cu_tru t WHERE t.cudan_id = c."Id" AND t.moi;

                -- Một yêu cầu chuyển từ dữ liệu cũ ở trạng thái Đã xử lý mà chưa có kết quả (ràng buộc mới bắt buộc có).
                UPDATE "YeuCauBaoTri" SET "GhiChuXuLy" = 'Chuyển từ dữ liệu cũ, không ghi kết quả'
                WHERE "TrangThai" IN (3, 4) AND coalesce(btrim("GhiChuXuLy"), '') = '';

                -- Hàm cũ tham chiếu tên cột cũ: bỏ, tạo lại ở cuối migration.
                DROP TRIGGER IF EXISTS trg_yeu_cau_chuyen_trang_thai ON "YeuCauBaoTri";
                DROP FUNCTION IF EXISTS fn_kiem_tra_chuyen_trang_thai();
                DROP FUNCTION IF EXISTS fn_han_xu_ly(timestamp, int);
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_AnhSuCo_NguoiDung_NguoiTaiLenId",
                table: "AnhSuCo");

            migrationBuilder.DropForeignKey(
                name: "FK_CanHo_Tang_ToaNha",
                table: "CanHo");

            migrationBuilder.DropForeignKey(
                name: "FK_CanHo_ToaNha_ToaNhaId",
                table: "CanHo");

            migrationBuilder.DropForeignKey(
                name: "FK_CuDan_CanHo_CanHoId",
                table: "CuDan");

            migrationBuilder.DropForeignKey(
                name: "FK_CuDan_NguoiDung_NguoiDungId",
                table: "CuDan");

            migrationBuilder.DropForeignKey(
                name: "FK_LichSuTrangThaiSuCo_NguoiDung_NguoiThucHienId",
                table: "LichSuTrangThaiSuCo");

            migrationBuilder.DropForeignKey(
                name: "FK_Tang_ToaNha_ToaNhaId",
                table: "Tang");

            migrationBuilder.DropForeignKey(
                name: "FK_ToaNha_DuAn_DuAnId",
                table: "ToaNha");

            migrationBuilder.DropForeignKey(
                name: "FK_ToaNha_KhuVuc_KhuVucId",
                table: "ToaNha");

            migrationBuilder.DropForeignKey(
                name: "FK_YeuCauBaoTri_MucSla_MucDoUuTien",
                table: "YeuCauBaoTri");

            migrationBuilder.DropForeignKey(
                name: "FK_YeuCauBaoTri_NguoiDung_NguoiBaoCaoId",
                table: "YeuCauBaoTri");

            migrationBuilder.DropForeignKey(
                name: "FK_YeuCauBaoTri_NguoiDung_NguoiPhuTrachId",
                table: "YeuCauBaoTri");

            migrationBuilder.DropCheckConstraint(
                name: "CK_YeuCauBaoTri_TrangThai",
                table: "YeuCauBaoTri");

            migrationBuilder.DropIndex(
                name: "IX_ToaNha_KhuVucId",
                table: "ToaNha");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ThanhToan_SoTien",
                table: "ThanhToan");

            migrationBuilder.DropUniqueConstraint(
                name: "UQ_Tang_Id_ToaNha",
                table: "Tang");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PhiDinhKy_SoTien",
                table: "PhiDinhKy");

            migrationBuilder.DropIndex(
                name: "UX_HoaDon_PhiThang_MoiKy",
                table: "HoaDon");

            migrationBuilder.DropCheckConstraint(
                name: "CK_HoaDon_SoTien",
                table: "HoaDon");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ChiSoDichVu_ChiSo",
                table: "ChiSoDichVu");

            migrationBuilder.DropIndex(
                name: "IX_CanHo_TangId_ToaNhaId",
                table: "CanHo");

            migrationBuilder.DropIndex(
                name: "IX_CanHo_ToaNhaId_MaCanHo",
                table: "CanHo");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CanHo_SoPhongNgu",
                table: "CanHo");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MucSla",
                table: "MucSla");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MucSla_ThoiHan",
                table: "MucSla");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CuDan",
                table: "CuDan");

            migrationBuilder.DropIndex(
                name: "IX_CuDan_CanHoId",
                table: "CuDan");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CuDan_NgayChuyenDi",
                table: "CuDan");

            migrationBuilder.DropColumn(
                name: "KhuVucId",
                table: "ToaNha");

            migrationBuilder.DropColumn(
                name: "DonViTienTe",
                table: "ThanhToan");

            migrationBuilder.DropColumn(
                name: "DaXuatHoaDon",
                table: "ChiSoDichVu");

            migrationBuilder.DropColumn(
                name: "ToaNhaId",
                table: "CanHo");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "CuDan");

            migrationBuilder.DropColumn(
                name: "HoTen",
                table: "CuDan");

            migrationBuilder.DropColumn(
                name: "SoDienThoai",
                table: "CuDan");

            migrationBuilder.DropColumn(
                name: "SoGiayTo",
                table: "CuDan");

            migrationBuilder.RenameTable(
                name: "MucSla",
                newName: "MucUuTien");

            migrationBuilder.RenameTable(
                name: "CuDan",
                newName: "CuTru");

            migrationBuilder.RenameColumn(
                name: "TrangThai",
                table: "YeuCauBaoTri",
                newName: "TrangThaiYeuCau");

            migrationBuilder.RenameColumn(
                name: "TieuDe",
                table: "YeuCauBaoTri",
                newName: "TieuDeYeuCau");

            migrationBuilder.RenameColumn(
                name: "NhanXet",
                table: "YeuCauBaoTri",
                newName: "NhanXetCuDan");

            migrationBuilder.RenameColumn(
                name: "NgayDongLai",
                table: "YeuCauBaoTri",
                newName: "NgayDong");

            migrationBuilder.RenameColumn(
                name: "MucDoUuTien",
                table: "YeuCauBaoTri",
                newName: "MucUuTienId");

            migrationBuilder.RenameColumn(
                name: "MoTa",
                table: "YeuCauBaoTri",
                newName: "MoTaYeuCau");

            migrationBuilder.RenameColumn(
                name: "GhiChuXuLy",
                table: "YeuCauBaoTri",
                newName: "KetQuaXuLy");

            migrationBuilder.RenameColumn(
                name: "AiTriageJson",
                table: "YeuCauBaoTri",
                newName: "KetQuaAiJson");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "YeuCauBaoTri",
                newName: "YeuCauBaoTriId");

            migrationBuilder.RenameIndex(
                name: "IX_YeuCauBaoTri_TrangThai",
                table: "YeuCauBaoTri",
                newName: "IX_YeuCauBaoTri_TrangThaiYeuCau");

            migrationBuilder.RenameIndex(
                name: "IX_YeuCauBaoTri_MucDoUuTien",
                table: "YeuCauBaoTri",
                newName: "IX_YeuCauBaoTri_MucUuTienId");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "VaiTro",
                newName: "TenVaiTro");

            migrationBuilder.RenameColumn(
                name: "UrlAnhDaiDien",
                table: "ToaNha",
                newName: "AnhDaiDienToaNha");

            migrationBuilder.RenameColumn(
                name: "TrangThai",
                table: "ToaNha",
                newName: "TrangThaiToaNha");

            migrationBuilder.RenameColumn(
                name: "Ten",
                table: "ToaNha",
                newName: "TenToaNha");

            migrationBuilder.RenameColumn(
                name: "Slug",
                table: "ToaNha",
                newName: "SlugToaNha");

            migrationBuilder.RenameColumn(
                name: "MoTa",
                table: "ToaNha",
                newName: "MoTaToaNha");

            migrationBuilder.RenameColumn(
                name: "Ma",
                table: "ToaNha",
                newName: "MaToaNha");

            migrationBuilder.RenameColumn(
                name: "DiaChi",
                table: "ToaNha",
                newName: "DiaChiToaNha");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "ToaNha",
                newName: "ToaNhaId");

            migrationBuilder.RenameIndex(
                name: "IX_ToaNha_TrangThai",
                table: "ToaNha",
                newName: "IX_ToaNha_TrangThaiToaNha");

            migrationBuilder.RenameIndex(
                name: "IX_ToaNha_Slug",
                table: "ToaNha",
                newName: "IX_ToaNha_SlugToaNha");

            migrationBuilder.RenameColumn(
                name: "TrangThai",
                table: "ThanhToan",
                newName: "TrangThaiThanhToan");

            migrationBuilder.RenameColumn(
                name: "SoTien",
                table: "ThanhToan",
                newName: "SoTienThanhToan");

            migrationBuilder.RenameColumn(
                name: "GhiChu",
                table: "ThanhToan",
                newName: "GhiChuThanhToan");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "ThanhToan",
                newName: "ThanhToanId");

            migrationBuilder.RenameIndex(
                name: "IX_ThanhToan_TrangThai",
                table: "ThanhToan",
                newName: "IX_ThanhToan_TrangThaiThanhToan");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Tang",
                newName: "TangId");

            migrationBuilder.RenameColumn(
                name: "TenHienThi",
                table: "Quyen",
                newName: "TenQuyen");

            migrationBuilder.RenameColumn(
                name: "Module",
                table: "Quyen",
                newName: "PhanHe");

            migrationBuilder.RenameColumn(
                name: "MoTa",
                table: "Quyen",
                newName: "MoTaQuyen");

            migrationBuilder.RenameColumn(
                name: "Ma",
                table: "Quyen",
                newName: "MaQuyen");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Quyen",
                newName: "QuyenId");

            migrationBuilder.RenameIndex(
                name: "IX_Quyen_Module",
                table: "Quyen",
                newName: "IX_Quyen_PhanHe");

            migrationBuilder.RenameIndex(
                name: "IX_Quyen_Ma",
                table: "Quyen",
                newName: "IX_Quyen_MaQuyen");

            migrationBuilder.RenameColumn(
                name: "ThuTu",
                table: "PhiDinhKy",
                newName: "ThuTuKhoanPhi");

            migrationBuilder.RenameColumn(
                name: "SoTien",
                table: "PhiDinhKy",
                newName: "SoTienKhoanPhi");

            migrationBuilder.RenameColumn(
                name: "MoTa",
                table: "PhiDinhKy",
                newName: "TenKhoanPhi");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "PhiDinhKy",
                newName: "PhiDinhKyId");

            migrationBuilder.RenameColumn(
                name: "UserAgent",
                table: "NhatKyHeThong",
                newName: "TrinhDuyet");

            migrationBuilder.RenameColumn(
                name: "ThoiGian",
                table: "NhatKyHeThong",
                newName: "ThoiGianGhi");

            migrationBuilder.RenameColumn(
                name: "TenNguoiDung",
                table: "NhatKyHeThong",
                newName: "TenNguoiGhi");

            migrationBuilder.RenameColumn(
                name: "NguoiDungId",
                table: "NhatKyHeThong",
                newName: "NguoiGhiId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "NhatKyHeThong",
                newName: "NhatKyId");

            migrationBuilder.RenameIndex(
                name: "IX_NhatKyHeThong_ThoiGian",
                table: "NhatKyHeThong",
                newName: "IX_NhatKyHeThong_ThoiGianGhi");

            migrationBuilder.RenameIndex(
                name: "IX_NhatKyHeThong_NguoiDungId",
                table: "NhatKyHeThong",
                newName: "IX_NhatKyHeThong_NguoiGhiId");

            migrationBuilder.RenameColumn(
                name: "UserName",
                table: "NguoiDung",
                newName: "TenDangNhap");

            migrationBuilder.RenameColumn(
                name: "UrlAvatar",
                table: "NguoiDung",
                newName: "AnhDaiDien");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "NguoiDung",
                newName: "MatKhauBam");

            migrationBuilder.RenameColumn(
                name: "Ten",
                table: "LoaiDichVu",
                newName: "TenLoaiDichVu");

            migrationBuilder.RenameColumn(
                name: "Ma",
                table: "LoaiDichVu",
                newName: "MaLoaiDichVu");

            migrationBuilder.RenameColumn(
                name: "Icon",
                table: "LoaiDichVu",
                newName: "BieuTuong");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "LoaiDichVu",
                newName: "LoaiDichVuId");

            migrationBuilder.RenameIndex(
                name: "IX_LoaiDichVu_Ma",
                table: "LoaiDichVu",
                newName: "IX_LoaiDichVu_MaLoaiDichVu");

            migrationBuilder.RenameColumn(
                name: "TuTrangThai",
                table: "LichSuTrangThaiSuCo",
                newName: "TrangThaiTruoc");

            migrationBuilder.RenameColumn(
                name: "ThoiDiem",
                table: "LichSuTrangThaiSuCo",
                newName: "ThoiDiemChuyen");

            migrationBuilder.RenameColumn(
                name: "SangTrangThai",
                table: "LichSuTrangThaiSuCo",
                newName: "TrangThaiSau");

            migrationBuilder.RenameColumn(
                name: "GhiChu",
                table: "LichSuTrangThaiSuCo",
                newName: "GhiChuChuyen");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "LichSuTrangThaiSuCo",
                newName: "LichSuTrangThaiId");

            migrationBuilder.RenameIndex(
                name: "IX_LichSuTrangThaiSuCo_YeuCauBaoTriId_ThoiDiem",
                table: "LichSuTrangThaiSuCo",
                newName: "IX_LichSuTrangThaiSuCo_YeuCauBaoTriId_ThoiDiemChuyen");

            migrationBuilder.RenameColumn(
                name: "UrlAnh",
                table: "KhuVuc",
                newName: "AnhKhuVuc");

            migrationBuilder.RenameColumn(
                name: "Ten",
                table: "KhuVuc",
                newName: "TenKhuVuc");

            migrationBuilder.RenameColumn(
                name: "Slug",
                table: "KhuVuc",
                newName: "SlugKhuVuc");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "KhuVuc",
                newName: "KhuVucId");

            migrationBuilder.RenameIndex(
                name: "IX_KhuVuc_Slug",
                table: "KhuVuc",
                newName: "IX_KhuVuc_SlugKhuVuc");

            migrationBuilder.RenameColumn(
                name: "TrangThai",
                table: "HoaDon",
                newName: "TrangThaiHoaDon");

            migrationBuilder.RenameColumn(
                name: "ThangTinhPhi",
                table: "HoaDon",
                newName: "KyHoaDon");

            migrationBuilder.RenameColumn(
                name: "GhiChu",
                table: "HoaDon",
                newName: "GhiChuHoaDon");

            migrationBuilder.RenameColumn(
                name: "DinhKy",
                table: "HoaDon",
                newName: "LaHoaDonDinhKy");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "HoaDon",
                newName: "HoaDonId");

            migrationBuilder.RenameIndex(
                name: "IX_HoaDon_TrangThai",
                table: "HoaDon",
                newName: "IX_HoaDon_TrangThaiHoaDon");

            migrationBuilder.RenameIndex(
                name: "IX_HoaDon_CanHoId_ThangTinhPhi_LoaiHoaDon",
                table: "HoaDon",
                newName: "IX_HoaDon_CanHoId_KyHoaDon_LoaiHoaDon");

            migrationBuilder.RenameColumn(
                name: "UrlAnhDaiDien",
                table: "DuAn",
                newName: "AnhDaiDienDuAn");

            migrationBuilder.RenameColumn(
                name: "Ten",
                table: "DuAn",
                newName: "TenDuAn");

            migrationBuilder.RenameColumn(
                name: "Slug",
                table: "DuAn",
                newName: "SlugDuAn");

            migrationBuilder.RenameColumn(
                name: "MoTaNgan",
                table: "DuAn",
                newName: "MoTaDuAn");

            migrationBuilder.RenameColumn(
                name: "DiaChi",
                table: "DuAn",
                newName: "DiaChiDuAn");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "DuAn",
                newName: "DuAnId");

            migrationBuilder.RenameIndex(
                name: "IX_DuAn_Slug",
                table: "DuAn",
                newName: "IX_DuAn_SlugDuAn");

            migrationBuilder.RenameColumn(
                name: "ThuTu",
                table: "ChiTietHoaDon",
                newName: "ThuTuKhoan");

            migrationBuilder.RenameColumn(
                name: "ThanhTien",
                table: "ChiTietHoaDon",
                newName: "ThanhTienKhoan");

            migrationBuilder.RenameColumn(
                name: "SoLuong",
                table: "ChiTietHoaDon",
                newName: "SoLuongKhoan");

            migrationBuilder.RenameColumn(
                name: "MoTa",
                table: "ChiTietHoaDon",
                newName: "NoiDungKhoan");

            migrationBuilder.RenameColumn(
                name: "DonGia",
                table: "ChiTietHoaDon",
                newName: "DonGiaKhoan");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "ChiTietHoaDon",
                newName: "ChiTietHoaDonId");

            migrationBuilder.RenameColumn(
                name: "ThanhTien",
                table: "ChiSoDichVu",
                newName: "ThanhTienChiSo");

            migrationBuilder.RenameColumn(
                name: "ThangTinhPhi",
                table: "ChiSoDichVu",
                newName: "KyChiSo");

            migrationBuilder.RenameColumn(
                name: "GhiChu",
                table: "ChiSoDichVu",
                newName: "GhiChuChiSo");

            migrationBuilder.RenameColumn(
                name: "DonGia",
                table: "ChiSoDichVu",
                newName: "DonGiaChiSo");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "ChiSoDichVu",
                newName: "ChiSoDichVuId");

            migrationBuilder.RenameIndex(
                name: "IX_ChiSoDichVu_ThangTinhPhi",
                table: "ChiSoDichVu",
                newName: "IX_ChiSoDichVu_KyChiSo");

            migrationBuilder.RenameIndex(
                name: "IX_ChiSoDichVu_CanHoId_LoaiDichVuId_ThangTinhPhi",
                table: "ChiSoDichVu",
                newName: "IX_ChiSoDichVu_CanHoId_LoaiDichVuId_KyChiSo");

            migrationBuilder.RenameColumn(
                name: "MoTa",
                table: "CanHo",
                newName: "MoTaCanHo");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "CanHo",
                newName: "CanHoId");

            migrationBuilder.RenameColumn(
                name: "GiaiDoan",
                table: "AnhSuCo",
                newName: "GiaiDoanAnh");

            migrationBuilder.RenameColumn(
                name: "DuongDan",
                table: "AnhSuCo",
                newName: "DuongDanAnh");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "AnhSuCo",
                newName: "AnhSuCoId");

            migrationBuilder.RenameColumn(
                name: "MucDoUuTien",
                table: "MucUuTien",
                newName: "MucUuTienId");

            migrationBuilder.RenameColumn(
                name: "VaiTro",
                table: "CuTru",
                newName: "VaiTroTrongHo");

            migrationBuilder.RenameColumn(
                name: "GhiChu",
                table: "CuTru",
                newName: "GhiChuCuTru");

            migrationBuilder.RenameColumn(
                name: "DaiDienCanHo",
                table: "CuTru",
                newName: "LaDaiDien");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "CuTru",
                newName: "CuTruId");

            migrationBuilder.RenameIndex(
                name: "IX_CuDan_NguoiDungId",
                table: "CuTru",
                newName: "IX_CuTru_NguoiDungId");

            migrationBuilder.AlterColumn<int>(
                name: "DuAnId",
                table: "ToaNha",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoGiayTo",
                table: "NguoiDung",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ChiSoDichVuId",
                table: "ChiTietHoaDon",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LoaiKhoan",
                table: "ChiTietHoaDon",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PhiDinhKyId",
                table: "ChiTietHoaDon",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YeuCauBaoTriId",
                table: "ChiTietHoaDon",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenMucUuTien",
                table: "MucUuTien",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "NguoiDungId",
                table: "CuTru",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayXacNhan",
                table: "CuTru",
                type: "timestamp without time zone",
                nullable: true);

            // ── Sau khi có cột mới: số giấy tờ sang NguoiDung, ngày xác nhận cư trú, nguồn của dòng hoá đơn ──
            migrationBuilder.Sql("""
                UPDATE "NguoiDung" u SET "SoGiayTo" = t."SoGiayTo"
                FROM tmp_cu_tru t WHERE t.uid = u."Id" AND t."SoGiayTo" IS NOT NULL AND u."SoGiayTo" IS NULL;

                -- Cư trú đã gắn tài khoản từ trước (qua thư mời) coi như đã xác nhận.
                UPDATE "CuTru" c SET "NgayXacNhan" = c."NgayTao" FROM tmp_cu_tru t WHERE t.cudan_id = c."CuTruId" AND NOT t.moi;

                -- Loại khoản và nguồn của các dòng hoá đơn đã lập (trước đây chỉ có nội dung chữ).
                UPDATE "ChiTietHoaDon" SET "LoaiKhoan" = 4;
                UPDATE "ChiTietHoaDon" SET "LoaiKhoan" = 0 WHERE "NoiDungKhoan" LIKE 'Phí quản lý %';
                UPDATE "ChiTietHoaDon" ct SET "LoaiKhoan" = 1, "ChiSoDichVuId" = cs."ChiSoDichVuId"
                FROM "HoaDon" h, "ChiSoDichVu" cs, "LoaiDichVu" l
                WHERE h."HoaDonId" = ct."HoaDonId" AND cs."CanHoId" = h."CanHoId" AND cs."KyChiSo" = h."KyHoaDon"
                  AND l."LoaiDichVuId" = cs."LoaiDichVuId" AND ct."NoiDungKhoan" LIKE l."TenLoaiDichVu" || ' (%';
                UPDATE "ChiTietHoaDon" ct SET "LoaiKhoan" = 2, "PhiDinhKyId" = p."PhiDinhKyId"
                FROM "HoaDon" h, "PhiDinhKy" p
                WHERE h."HoaDonId" = ct."HoaDonId" AND p."CanHoId" = h."CanHoId" AND p."TenKhoanPhi" = ct."NoiDungKhoan"
                  AND ct."LoaiKhoan" = 4;
                """);

            migrationBuilder.AddPrimaryKey(
                name: "PK_MucUuTien",
                table: "MucUuTien",
                column: "MucUuTienId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CuTru",
                table: "CuTru",
                column: "CuTruId");

            migrationBuilder.UpdateData(
                table: "MucUuTien",
                keyColumn: "MucUuTienId",
                keyValue: 0,
                column: "TenMucUuTien",
                value: "Thấp");

            migrationBuilder.UpdateData(
                table: "MucUuTien",
                keyColumn: "MucUuTienId",
                keyValue: 1,
                column: "TenMucUuTien",
                value: "Trung bình");

            migrationBuilder.UpdateData(
                table: "MucUuTien",
                keyColumn: "MucUuTienId",
                keyValue: 2,
                column: "TenMucUuTien",
                value: "Cao");

            migrationBuilder.UpdateData(
                table: "MucUuTien",
                keyColumn: "MucUuTienId",
                keyValue: 3,
                column: "TenMucUuTien",
                value: "Khẩn cấp");

            migrationBuilder.AddCheckConstraint(
                name: "CK_YeuCauBaoTri_KetQua",
                table: "YeuCauBaoTri",
                sql: "\"TrangThaiYeuCau\" NOT IN (3, 4) OR coalesce(btrim(\"KetQuaXuLy\"), '') <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_YeuCauBaoTri_TrangThai",
                table: "YeuCauBaoTri",
                sql: "\"TrangThaiYeuCau\" BETWEEN 0 AND 7");

            migrationBuilder.CreateIndex(
                name: "IX_ToaNha_MaToaNha",
                table: "ToaNha",
                column: "MaToaNha",
                unique: true,
                filter: "\"DaXoa\" = false");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ToaNha_PhiTre",
                table: "ToaNha",
                sql: "\"SoNgayTreHanMacDinh\" >= 0 AND \"PhanTramPhiTreMacDinh\" BETWEEN 0 AND 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ToaNha_SoLuongTang",
                table: "ToaNha",
                sql: "\"SoLuongTang\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ToaNha_TrangThai",
                table: "ToaNha",
                sql: "\"TrangThaiToaNha\" BETWEEN 0 AND 2");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ThanhToan_HoanTra",
                table: "ThanhToan",
                sql: "\"SoTienHoanTra\" >= 0 AND \"SoTienHoanTra\" <= \"SoTienThanhToan\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ThanhToan_MaSo",
                table: "ThanhToan",
                sql: "\"PhuongThuc\" BETWEEN 0 AND 4 AND \"TrangThaiThanhToan\" BETWEEN 0 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ThanhToan_SoTien",
                table: "ThanhToan",
                sql: "\"SoTienThanhToan\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PhiDinhKy_SoTien",
                table: "PhiDinhKy",
                sql: "\"SoTienKhoanPhi\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LoaiDichVu_TinhPhi",
                table: "LoaiDichVu",
                sql: "\"CheDoTinhPhi\" IN (0, 1) AND \"DonGiaMacDinh\" >= 0");

            migrationBuilder.CreateIndex(
                name: "UX_HoaDon_PhiThang_MoiKy",
                table: "HoaDon",
                columns: new[] { "CanHoId", "KyHoaDon" },
                unique: true,
                filter: "\"LoaiHoaDon\" = 0 AND \"TrangThaiHoaDon\" <> 5 AND \"DaXoa\" = false");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HoaDon_CongThuc",
                table: "HoaDon",
                sql: "\"TongTien\" = \"TamTinh\" - \"GiamGia\" + \"Thue\" + \"PhiTreHan\" AND \"ConLai\" = \"TongTien\" - \"DaThanhToan\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HoaDon_HanThanhToan",
                table: "HoaDon",
                sql: "\"NgayDenHan\" >= \"NgayPhatHanh\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HoaDon_Ky",
                table: "HoaDon",
                sql: "\"KyHoaDon\" % 100 BETWEEN 1 AND 12");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HoaDon_SoTien",
                table: "HoaDon",
                sql: "\"TongTien\" >= 0 AND \"DaThanhToan\" >= 0 AND \"GiamGia\" >= 0 AND \"Thue\" >= 0 AND \"PhiTreHan\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HoaDon_TrangThai",
                table: "HoaDon",
                sql: "\"TrangThaiHoaDon\" BETWEEN 0 AND 6");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietHoaDon_ChiSoDichVuId",
                table: "ChiTietHoaDon",
                column: "ChiSoDichVuId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietHoaDon_PhiDinhKyId",
                table: "ChiTietHoaDon",
                column: "PhiDinhKyId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietHoaDon_YeuCauBaoTriId",
                table: "ChiTietHoaDon",
                column: "YeuCauBaoTriId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChiTietHoaDon_LoaiKhoan",
                table: "ChiTietHoaDon",
                sql: "\"LoaiKhoan\" BETWEEN 0 AND 4");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChiTietHoaDon_Nguon",
                table: "ChiTietHoaDon",
                sql: "(\"LoaiKhoan\" = 1) = (\"ChiSoDichVuId\" IS NOT NULL) AND (\"LoaiKhoan\" = 2) = (\"PhiDinhKyId\" IS NOT NULL) AND (\"YeuCauBaoTriId\" IS NULL OR \"LoaiKhoan\" = 3)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChiTietHoaDon_SoTien",
                table: "ChiTietHoaDon",
                sql: "\"SoLuongKhoan\" > 0 AND \"DonGiaKhoan\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChiSoDichVu_ChiSo",
                table: "ChiSoDichVu",
                sql: "\"ChiSoCu\" >= 0 AND \"ChiSoMoi\" >= \"ChiSoCu\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChiSoDichVu_Ky",
                table: "ChiSoDichVu",
                sql: "\"KyChiSo\" % 100 BETWEEN 1 AND 12");

            migrationBuilder.CreateIndex(
                name: "IX_CanHo_MaCanHo",
                table: "CanHo",
                column: "MaCanHo",
                unique: true,
                filter: "\"DaXoa\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_CanHo_TangId",
                table: "CanHo",
                column: "TangId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CanHo_HuongNha",
                table: "CanHo",
                sql: "\"HuongNha\" IS NULL OR \"HuongNha\" BETWEEN 0 AND 7");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CanHo_SoPhong",
                table: "CanHo",
                sql: "\"SoPhongNgu\" >= 0 AND \"SoPhongTam\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CanHo_TinhTrangThue",
                table: "CanHo",
                sql: "\"TinhTrangThue\" BETWEEN 0 AND 3");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AnhSuCo_GiaiDoan",
                table: "AnhSuCo",
                sql: "\"GiaiDoanAnh\" IN (0, 1)");

            migrationBuilder.CreateIndex(
                name: "IX_MucUuTien_TenMucUuTien",
                table: "MucUuTien",
                column: "TenMucUuTien",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_MucUuTien_Id",
                table: "MucUuTien",
                sql: "\"MucUuTienId\" BETWEEN 0 AND 3");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MucUuTien_ThoiHan",
                table: "MucUuTien",
                sql: "\"PhutPhanHoi\" > 0 AND \"PhutHoanTat\" >= \"PhutPhanHoi\"");

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
                name: "CK_CuTru_VaiTroTrongHo",
                table: "CuTru",
                sql: "\"VaiTroTrongHo\" BETWEEN 0 AND 2");

            migrationBuilder.AddForeignKey(
                name: "FK_AnhSuCo_NguoiDung_NguoiTaiLenId",
                table: "AnhSuCo",
                column: "NguoiTaiLenId",
                principalTable: "NguoiDung",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CanHo_Tang_TangId",
                table: "CanHo",
                column: "TangId",
                principalTable: "Tang",
                principalColumn: "TangId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChiTietHoaDon_ChiSoDichVu_ChiSoDichVuId",
                table: "ChiTietHoaDon",
                column: "ChiSoDichVuId",
                principalTable: "ChiSoDichVu",
                principalColumn: "ChiSoDichVuId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChiTietHoaDon_PhiDinhKy_PhiDinhKyId",
                table: "ChiTietHoaDon",
                column: "PhiDinhKyId",
                principalTable: "PhiDinhKy",
                principalColumn: "PhiDinhKyId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChiTietHoaDon_YeuCauBaoTri_YeuCauBaoTriId",
                table: "ChiTietHoaDon",
                column: "YeuCauBaoTriId",
                principalTable: "YeuCauBaoTri",
                principalColumn: "YeuCauBaoTriId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CuTru_CanHo_CanHoId",
                table: "CuTru",
                column: "CanHoId",
                principalTable: "CanHo",
                principalColumn: "CanHoId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CuTru_NguoiDung_NguoiDungId",
                table: "CuTru",
                column: "NguoiDungId",
                principalTable: "NguoiDung",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LichSuTrangThaiSuCo_NguoiDung_NguoiThucHienId",
                table: "LichSuTrangThaiSuCo",
                column: "NguoiThucHienId",
                principalTable: "NguoiDung",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Tang_ToaNha_ToaNhaId",
                table: "Tang",
                column: "ToaNhaId",
                principalTable: "ToaNha",
                principalColumn: "ToaNhaId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ToaNha_DuAn_DuAnId",
                table: "ToaNha",
                column: "DuAnId",
                principalTable: "DuAn",
                principalColumn: "DuAnId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_YeuCauBaoTri_MucUuTien_MucUuTienId",
                table: "YeuCauBaoTri",
                column: "MucUuTienId",
                principalTable: "MucUuTien",
                principalColumn: "MucUuTienId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_YeuCauBaoTri_NguoiDung_NguoiBaoCaoId",
                table: "YeuCauBaoTri",
                column: "NguoiBaoCaoId",
                principalTable: "NguoiDung",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_YeuCauBaoTri_NguoiDung_NguoiPhuTrachId",
                table: "YeuCauBaoTri",
                column: "NguoiPhuTrachId",
                principalTable: "NguoiDung",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // ── Trigger, hàm và khung nhìn đối chiếu theo tên cột mới (báo cáo mục 2.5.7, 2.7) ──
            migrationBuilder.Sql("""
                -- TG1: trạng thái yêu cầu chỉ chuyển theo bảng chuyển; bắt đầu xử lý phải có người phụ trách.
                CREATE OR REPLACE FUNCTION fn_kiem_tra_chuyen_trang_thai() RETURNS trigger AS $$
                BEGIN
                  IF NEW."TrangThaiYeuCau" = OLD."TrangThaiYeuCau" THEN RETURN NEW; END IF;
                  IF (OLD."TrangThaiYeuCau", NEW."TrangThaiYeuCau") NOT IN
                     ((0,1),(0,5),(0,7),(1,2),(1,5),(1,7),(2,6),(2,3),(6,2),(3,4),(3,2)) THEN
                    RAISE EXCEPTION 'Yêu cầu % không thể chuyển từ trạng thái % sang %',
                      OLD."SoYeuCau", OLD."TrangThaiYeuCau", NEW."TrangThaiYeuCau";
                  END IF;
                  IF NEW."TrangThaiYeuCau" = 2 AND NEW."NguoiPhuTrachId" IS NULL THEN
                    RAISE EXCEPTION 'Phải giao kỹ thuật viên trước khi bắt đầu xử lý';
                  END IF;
                  RETURN NEW;
                END $$ LANGUAGE plpgsql;

                CREATE TRIGGER trg_yeu_cau_chuyen_trang_thai
                  BEFORE UPDATE OF "TrangThaiYeuCau" ON "YeuCauBaoTri"
                  FOR EACH ROW EXECUTE FUNCTION fn_kiem_tra_chuyen_trang_thai();

                -- TG2: chỉ số đã đưa vào hoá đơn chưa huỷ thì không sửa, không xoá.
                CREATE OR REPLACE FUNCTION fn_khoa_chi_so_da_xuat() RETURNS trigger AS $$
                BEGIN
                  IF EXISTS (SELECT 1 FROM "ChiTietHoaDon" ct JOIN "HoaDon" h ON h."HoaDonId" = ct."HoaDonId"
                             WHERE ct."ChiSoDichVuId" = OLD."ChiSoDichVuId" AND NOT ct."DaXoa" AND h."TrangThaiHoaDon" <> 5) THEN
                    RAISE EXCEPTION 'Chỉ số % đã đưa vào hoá đơn, không thể sửa hoặc xoá', OLD."ChiSoDichVuId";
                  END IF;
                  IF TG_OP = 'DELETE' THEN RETURN OLD; END IF;
                  RETURN NEW;
                END $$ LANGUAGE plpgsql;

                CREATE TRIGGER trg_khoa_chi_so_da_xuat
                  BEFORE UPDATE OR DELETE ON "ChiSoDichVu"
                  FOR EACH ROW EXECUTE FUNCTION fn_khoa_chi_so_da_xuat();

                -- TG3: tổng tiền đã thu (thành công, trừ phần hoàn) của một hoá đơn không vượt tổng tiền hoá đơn.
                CREATE OR REPLACE FUNCTION fn_kiem_tra_tong_thanh_toan() RETURNS trigger AS $$
                DECLARE da_thu numeric; tong numeric;
                BEGIN
                  SELECT coalesce(sum("SoTienThanhToan" - "SoTienHoanTra"), 0) INTO da_thu
                  FROM "ThanhToan" WHERE "HoaDonId" = NEW."HoaDonId" AND NOT "DaXoa" AND "TrangThaiThanhToan" IN (1, 3, 4);
                  SELECT "TongTien" INTO tong FROM "HoaDon" WHERE "HoaDonId" = NEW."HoaDonId";
                  IF da_thu > tong THEN
                    RAISE EXCEPTION 'Tổng đã thu % vượt tổng tiền % của hoá đơn %', da_thu, tong, NEW."HoaDonId";
                  END IF;
                  RETURN NULL;
                END $$ LANGUAGE plpgsql;

                CREATE TRIGGER trg_kiem_tra_tong_thanh_toan
                  AFTER INSERT OR UPDATE ON "ThanhToan"
                  FOR EACH ROW EXECUTE FUNCTION fn_kiem_tra_tong_thanh_toan();

                -- FN1: hạn xử lý = ngày báo + thời hạn hoàn tất của mức ưu tiên.
                CREATE OR REPLACE FUNCTION fn_han_xu_ly(p_ngay_bao timestamp, p_muc int) RETURNS timestamp AS $$
                  SELECT p_ngay_bao + make_interval(mins => m."PhutHoanTat") FROM "MucUuTien" m WHERE m."MucUuTienId" = p_muc;
                $$ LANGUAGE sql STABLE;

                -- Khung nhìn đối chiếu: giá trị lưu ở HoaDon so với giá trị tính lại từ dòng chi tiết và thanh toán.
                CREATE OR REPLACE VIEW vw_doi_chieu_hoa_don AS
                SELECT h."HoaDonId", h."SoHoaDon", h."TamTinh", h."DaThanhToan", h."ConLai",
                       coalesce((SELECT sum(ct."ThanhTienKhoan") FROM "ChiTietHoaDon" ct
                                 WHERE ct."HoaDonId" = h."HoaDonId" AND NOT ct."DaXoa"), 0) AS "TamTinhTinhLai",
                       coalesce((SELECT sum(t."SoTienThanhToan" - t."SoTienHoanTra") FROM "ThanhToan" t
                                 WHERE t."HoaDonId" = h."HoaDonId" AND NOT t."DaXoa" AND t."TrangThaiThanhToan" IN (1, 3, 4)), 0) AS "DaThanhToanTinhLai"
                FROM "HoaDon" h WHERE NOT h."DaXoa";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Migration này chuyển dữ liệu (tạo tài khoản cho người cư trú, bỏ cột trùng) nên không quay lui tự động được.
            // Khôi phục bằng bản sao lưu db_backup/LuxeHavenBql_truoc_doi_luoc_do_20261124.dump.
            throw new NotSupportedException("Khôi phục từ db_backup/LuxeHavenBql_truoc_doi_luoc_do_20261124.dump");
#pragma warning disable CS0162
            migrationBuilder.DropForeignKey(
                name: "FK_AnhSuCo_NguoiDung_NguoiTaiLenId",
                table: "AnhSuCo");

            migrationBuilder.DropForeignKey(
                name: "FK_CanHo_Tang_TangId",
                table: "CanHo");

            migrationBuilder.DropForeignKey(
                name: "FK_ChiTietHoaDon_ChiSoDichVu_ChiSoDichVuId",
                table: "ChiTietHoaDon");

            migrationBuilder.DropForeignKey(
                name: "FK_ChiTietHoaDon_PhiDinhKy_PhiDinhKyId",
                table: "ChiTietHoaDon");

            migrationBuilder.DropForeignKey(
                name: "FK_ChiTietHoaDon_YeuCauBaoTri_YeuCauBaoTriId",
                table: "ChiTietHoaDon");

            migrationBuilder.DropForeignKey(
                name: "FK_CuTru_CanHo_CanHoId",
                table: "CuTru");

            migrationBuilder.DropForeignKey(
                name: "FK_CuTru_NguoiDung_NguoiDungId",
                table: "CuTru");

            migrationBuilder.DropForeignKey(
                name: "FK_LichSuTrangThaiSuCo_NguoiDung_NguoiThucHienId",
                table: "LichSuTrangThaiSuCo");

            migrationBuilder.DropForeignKey(
                name: "FK_Tang_ToaNha_ToaNhaId",
                table: "Tang");

            migrationBuilder.DropForeignKey(
                name: "FK_ToaNha_DuAn_DuAnId",
                table: "ToaNha");

            migrationBuilder.DropForeignKey(
                name: "FK_YeuCauBaoTri_MucUuTien_MucUuTienId",
                table: "YeuCauBaoTri");

            migrationBuilder.DropForeignKey(
                name: "FK_YeuCauBaoTri_NguoiDung_NguoiBaoCaoId",
                table: "YeuCauBaoTri");

            migrationBuilder.DropForeignKey(
                name: "FK_YeuCauBaoTri_NguoiDung_NguoiPhuTrachId",
                table: "YeuCauBaoTri");

            migrationBuilder.DropCheckConstraint(
                name: "CK_YeuCauBaoTri_KetQua",
                table: "YeuCauBaoTri");

            migrationBuilder.DropCheckConstraint(
                name: "CK_YeuCauBaoTri_TrangThai",
                table: "YeuCauBaoTri");

            migrationBuilder.DropIndex(
                name: "IX_ToaNha_MaToaNha",
                table: "ToaNha");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ToaNha_PhiTre",
                table: "ToaNha");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ToaNha_SoLuongTang",
                table: "ToaNha");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ToaNha_TrangThai",
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
                name: "CK_LoaiDichVu_TinhPhi",
                table: "LoaiDichVu");

            migrationBuilder.DropIndex(
                name: "UX_HoaDon_PhiThang_MoiKy",
                table: "HoaDon");

            migrationBuilder.DropCheckConstraint(
                name: "CK_HoaDon_CongThuc",
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

            migrationBuilder.DropCheckConstraint(
                name: "CK_HoaDon_TrangThai",
                table: "HoaDon");

            migrationBuilder.DropIndex(
                name: "IX_ChiTietHoaDon_ChiSoDichVuId",
                table: "ChiTietHoaDon");

            migrationBuilder.DropIndex(
                name: "IX_ChiTietHoaDon_PhiDinhKyId",
                table: "ChiTietHoaDon");

            migrationBuilder.DropIndex(
                name: "IX_ChiTietHoaDon_YeuCauBaoTriId",
                table: "ChiTietHoaDon");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ChiTietHoaDon_LoaiKhoan",
                table: "ChiTietHoaDon");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ChiTietHoaDon_Nguon",
                table: "ChiTietHoaDon");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ChiTietHoaDon_SoTien",
                table: "ChiTietHoaDon");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ChiSoDichVu_ChiSo",
                table: "ChiSoDichVu");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ChiSoDichVu_Ky",
                table: "ChiSoDichVu");

            migrationBuilder.DropIndex(
                name: "IX_CanHo_MaCanHo",
                table: "CanHo");

            migrationBuilder.DropIndex(
                name: "IX_CanHo_TangId",
                table: "CanHo");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CanHo_HuongNha",
                table: "CanHo");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CanHo_SoPhong",
                table: "CanHo");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CanHo_TinhTrangThue",
                table: "CanHo");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AnhSuCo_GiaiDoan",
                table: "AnhSuCo");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MucUuTien",
                table: "MucUuTien");

            migrationBuilder.DropIndex(
                name: "IX_MucUuTien_TenMucUuTien",
                table: "MucUuTien");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MucUuTien_Id",
                table: "MucUuTien");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MucUuTien_ThoiHan",
                table: "MucUuTien");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CuTru",
                table: "CuTru");

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
                name: "CK_CuTru_VaiTroTrongHo",
                table: "CuTru");

            migrationBuilder.DropColumn(
                name: "SoGiayTo",
                table: "NguoiDung");

            migrationBuilder.DropColumn(
                name: "ChiSoDichVuId",
                table: "ChiTietHoaDon");

            migrationBuilder.DropColumn(
                name: "LoaiKhoan",
                table: "ChiTietHoaDon");

            migrationBuilder.DropColumn(
                name: "PhiDinhKyId",
                table: "ChiTietHoaDon");

            migrationBuilder.DropColumn(
                name: "YeuCauBaoTriId",
                table: "ChiTietHoaDon");

            migrationBuilder.DropColumn(
                name: "TenMucUuTien",
                table: "MucUuTien");

            migrationBuilder.DropColumn(
                name: "NgayXacNhan",
                table: "CuTru");

            migrationBuilder.RenameTable(
                name: "MucUuTien",
                newName: "MucSla");

            migrationBuilder.RenameTable(
                name: "CuTru",
                newName: "CuDan");

            migrationBuilder.RenameColumn(
                name: "TrangThaiYeuCau",
                table: "YeuCauBaoTri",
                newName: "TrangThai");

            migrationBuilder.RenameColumn(
                name: "TieuDeYeuCau",
                table: "YeuCauBaoTri",
                newName: "TieuDe");

            migrationBuilder.RenameColumn(
                name: "NhanXetCuDan",
                table: "YeuCauBaoTri",
                newName: "NhanXet");

            migrationBuilder.RenameColumn(
                name: "NgayDong",
                table: "YeuCauBaoTri",
                newName: "NgayDongLai");

            migrationBuilder.RenameColumn(
                name: "MucUuTienId",
                table: "YeuCauBaoTri",
                newName: "MucDoUuTien");

            migrationBuilder.RenameColumn(
                name: "MoTaYeuCau",
                table: "YeuCauBaoTri",
                newName: "MoTa");

            migrationBuilder.RenameColumn(
                name: "KetQuaXuLy",
                table: "YeuCauBaoTri",
                newName: "GhiChuXuLy");

            migrationBuilder.RenameColumn(
                name: "KetQuaAiJson",
                table: "YeuCauBaoTri",
                newName: "AiTriageJson");

            migrationBuilder.RenameColumn(
                name: "YeuCauBaoTriId",
                table: "YeuCauBaoTri",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_YeuCauBaoTri_TrangThaiYeuCau",
                table: "YeuCauBaoTri",
                newName: "IX_YeuCauBaoTri_TrangThai");

            migrationBuilder.RenameIndex(
                name: "IX_YeuCauBaoTri_MucUuTienId",
                table: "YeuCauBaoTri",
                newName: "IX_YeuCauBaoTri_MucDoUuTien");

            migrationBuilder.RenameColumn(
                name: "TenVaiTro",
                table: "VaiTro",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "TrangThaiToaNha",
                table: "ToaNha",
                newName: "TrangThai");

            migrationBuilder.RenameColumn(
                name: "TenToaNha",
                table: "ToaNha",
                newName: "Ten");

            migrationBuilder.RenameColumn(
                name: "SlugToaNha",
                table: "ToaNha",
                newName: "Slug");

            migrationBuilder.RenameColumn(
                name: "MoTaToaNha",
                table: "ToaNha",
                newName: "MoTa");

            migrationBuilder.RenameColumn(
                name: "MaToaNha",
                table: "ToaNha",
                newName: "Ma");

            migrationBuilder.RenameColumn(
                name: "DiaChiToaNha",
                table: "ToaNha",
                newName: "DiaChi");

            migrationBuilder.RenameColumn(
                name: "AnhDaiDienToaNha",
                table: "ToaNha",
                newName: "UrlAnhDaiDien");

            migrationBuilder.RenameColumn(
                name: "ToaNhaId",
                table: "ToaNha",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_ToaNha_TrangThaiToaNha",
                table: "ToaNha",
                newName: "IX_ToaNha_TrangThai");

            migrationBuilder.RenameIndex(
                name: "IX_ToaNha_SlugToaNha",
                table: "ToaNha",
                newName: "IX_ToaNha_Slug");

            migrationBuilder.RenameColumn(
                name: "TrangThaiThanhToan",
                table: "ThanhToan",
                newName: "TrangThai");

            migrationBuilder.RenameColumn(
                name: "SoTienThanhToan",
                table: "ThanhToan",
                newName: "SoTien");

            migrationBuilder.RenameColumn(
                name: "GhiChuThanhToan",
                table: "ThanhToan",
                newName: "GhiChu");

            migrationBuilder.RenameColumn(
                name: "ThanhToanId",
                table: "ThanhToan",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_ThanhToan_TrangThaiThanhToan",
                table: "ThanhToan",
                newName: "IX_ThanhToan_TrangThai");

            migrationBuilder.RenameColumn(
                name: "TangId",
                table: "Tang",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "TenQuyen",
                table: "Quyen",
                newName: "TenHienThi");

            migrationBuilder.RenameColumn(
                name: "PhanHe",
                table: "Quyen",
                newName: "Module");

            migrationBuilder.RenameColumn(
                name: "MoTaQuyen",
                table: "Quyen",
                newName: "MoTa");

            migrationBuilder.RenameColumn(
                name: "MaQuyen",
                table: "Quyen",
                newName: "Ma");

            migrationBuilder.RenameColumn(
                name: "QuyenId",
                table: "Quyen",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_Quyen_PhanHe",
                table: "Quyen",
                newName: "IX_Quyen_Module");

            migrationBuilder.RenameIndex(
                name: "IX_Quyen_MaQuyen",
                table: "Quyen",
                newName: "IX_Quyen_Ma");

            migrationBuilder.RenameColumn(
                name: "ThuTuKhoanPhi",
                table: "PhiDinhKy",
                newName: "ThuTu");

            migrationBuilder.RenameColumn(
                name: "TenKhoanPhi",
                table: "PhiDinhKy",
                newName: "MoTa");

            migrationBuilder.RenameColumn(
                name: "SoTienKhoanPhi",
                table: "PhiDinhKy",
                newName: "SoTien");

            migrationBuilder.RenameColumn(
                name: "PhiDinhKyId",
                table: "PhiDinhKy",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "TrinhDuyet",
                table: "NhatKyHeThong",
                newName: "UserAgent");

            migrationBuilder.RenameColumn(
                name: "ThoiGianGhi",
                table: "NhatKyHeThong",
                newName: "ThoiGian");

            migrationBuilder.RenameColumn(
                name: "TenNguoiGhi",
                table: "NhatKyHeThong",
                newName: "TenNguoiDung");

            migrationBuilder.RenameColumn(
                name: "NguoiGhiId",
                table: "NhatKyHeThong",
                newName: "NguoiDungId");

            migrationBuilder.RenameColumn(
                name: "NhatKyId",
                table: "NhatKyHeThong",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_NhatKyHeThong_ThoiGianGhi",
                table: "NhatKyHeThong",
                newName: "IX_NhatKyHeThong_ThoiGian");

            migrationBuilder.RenameIndex(
                name: "IX_NhatKyHeThong_NguoiGhiId",
                table: "NhatKyHeThong",
                newName: "IX_NhatKyHeThong_NguoiDungId");

            migrationBuilder.RenameColumn(
                name: "TenDangNhap",
                table: "NguoiDung",
                newName: "UserName");

            migrationBuilder.RenameColumn(
                name: "MatKhauBam",
                table: "NguoiDung",
                newName: "PasswordHash");

            migrationBuilder.RenameColumn(
                name: "AnhDaiDien",
                table: "NguoiDung",
                newName: "UrlAvatar");

            migrationBuilder.RenameColumn(
                name: "TenLoaiDichVu",
                table: "LoaiDichVu",
                newName: "Ten");

            migrationBuilder.RenameColumn(
                name: "MaLoaiDichVu",
                table: "LoaiDichVu",
                newName: "Ma");

            migrationBuilder.RenameColumn(
                name: "BieuTuong",
                table: "LoaiDichVu",
                newName: "Icon");

            migrationBuilder.RenameColumn(
                name: "LoaiDichVuId",
                table: "LoaiDichVu",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_LoaiDichVu_MaLoaiDichVu",
                table: "LoaiDichVu",
                newName: "IX_LoaiDichVu_Ma");

            migrationBuilder.RenameColumn(
                name: "TrangThaiTruoc",
                table: "LichSuTrangThaiSuCo",
                newName: "TuTrangThai");

            migrationBuilder.RenameColumn(
                name: "TrangThaiSau",
                table: "LichSuTrangThaiSuCo",
                newName: "SangTrangThai");

            migrationBuilder.RenameColumn(
                name: "ThoiDiemChuyen",
                table: "LichSuTrangThaiSuCo",
                newName: "ThoiDiem");

            migrationBuilder.RenameColumn(
                name: "GhiChuChuyen",
                table: "LichSuTrangThaiSuCo",
                newName: "GhiChu");

            migrationBuilder.RenameColumn(
                name: "LichSuTrangThaiId",
                table: "LichSuTrangThaiSuCo",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_LichSuTrangThaiSuCo_YeuCauBaoTriId_ThoiDiemChuyen",
                table: "LichSuTrangThaiSuCo",
                newName: "IX_LichSuTrangThaiSuCo_YeuCauBaoTriId_ThoiDiem");

            migrationBuilder.RenameColumn(
                name: "TenKhuVuc",
                table: "KhuVuc",
                newName: "Ten");

            migrationBuilder.RenameColumn(
                name: "SlugKhuVuc",
                table: "KhuVuc",
                newName: "Slug");

            migrationBuilder.RenameColumn(
                name: "AnhKhuVuc",
                table: "KhuVuc",
                newName: "UrlAnh");

            migrationBuilder.RenameColumn(
                name: "KhuVucId",
                table: "KhuVuc",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_KhuVuc_SlugKhuVuc",
                table: "KhuVuc",
                newName: "IX_KhuVuc_Slug");

            migrationBuilder.RenameColumn(
                name: "TrangThaiHoaDon",
                table: "HoaDon",
                newName: "TrangThai");

            migrationBuilder.RenameColumn(
                name: "LaHoaDonDinhKy",
                table: "HoaDon",
                newName: "DinhKy");

            migrationBuilder.RenameColumn(
                name: "KyHoaDon",
                table: "HoaDon",
                newName: "ThangTinhPhi");

            migrationBuilder.RenameColumn(
                name: "GhiChuHoaDon",
                table: "HoaDon",
                newName: "GhiChu");

            migrationBuilder.RenameColumn(
                name: "HoaDonId",
                table: "HoaDon",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_HoaDon_TrangThaiHoaDon",
                table: "HoaDon",
                newName: "IX_HoaDon_TrangThai");

            migrationBuilder.RenameIndex(
                name: "IX_HoaDon_CanHoId_KyHoaDon_LoaiHoaDon",
                table: "HoaDon",
                newName: "IX_HoaDon_CanHoId_ThangTinhPhi_LoaiHoaDon");

            migrationBuilder.RenameColumn(
                name: "TenDuAn",
                table: "DuAn",
                newName: "Ten");

            migrationBuilder.RenameColumn(
                name: "SlugDuAn",
                table: "DuAn",
                newName: "Slug");

            migrationBuilder.RenameColumn(
                name: "MoTaDuAn",
                table: "DuAn",
                newName: "MoTaNgan");

            migrationBuilder.RenameColumn(
                name: "DiaChiDuAn",
                table: "DuAn",
                newName: "DiaChi");

            migrationBuilder.RenameColumn(
                name: "AnhDaiDienDuAn",
                table: "DuAn",
                newName: "UrlAnhDaiDien");

            migrationBuilder.RenameColumn(
                name: "DuAnId",
                table: "DuAn",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_DuAn_SlugDuAn",
                table: "DuAn",
                newName: "IX_DuAn_Slug");

            migrationBuilder.RenameColumn(
                name: "ThuTuKhoan",
                table: "ChiTietHoaDon",
                newName: "ThuTu");

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
                newName: "MoTa");

            migrationBuilder.RenameColumn(
                name: "DonGiaKhoan",
                table: "ChiTietHoaDon",
                newName: "DonGia");

            migrationBuilder.RenameColumn(
                name: "ChiTietHoaDonId",
                table: "ChiTietHoaDon",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "ThanhTienChiSo",
                table: "ChiSoDichVu",
                newName: "ThanhTien");

            migrationBuilder.RenameColumn(
                name: "KyChiSo",
                table: "ChiSoDichVu",
                newName: "ThangTinhPhi");

            migrationBuilder.RenameColumn(
                name: "GhiChuChiSo",
                table: "ChiSoDichVu",
                newName: "GhiChu");

            migrationBuilder.RenameColumn(
                name: "DonGiaChiSo",
                table: "ChiSoDichVu",
                newName: "DonGia");

            migrationBuilder.RenameColumn(
                name: "ChiSoDichVuId",
                table: "ChiSoDichVu",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_ChiSoDichVu_KyChiSo",
                table: "ChiSoDichVu",
                newName: "IX_ChiSoDichVu_ThangTinhPhi");

            migrationBuilder.RenameIndex(
                name: "IX_ChiSoDichVu_CanHoId_LoaiDichVuId_KyChiSo",
                table: "ChiSoDichVu",
                newName: "IX_ChiSoDichVu_CanHoId_LoaiDichVuId_ThangTinhPhi");

            migrationBuilder.RenameColumn(
                name: "MoTaCanHo",
                table: "CanHo",
                newName: "MoTa");

            migrationBuilder.RenameColumn(
                name: "CanHoId",
                table: "CanHo",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "GiaiDoanAnh",
                table: "AnhSuCo",
                newName: "GiaiDoan");

            migrationBuilder.RenameColumn(
                name: "DuongDanAnh",
                table: "AnhSuCo",
                newName: "DuongDan");

            migrationBuilder.RenameColumn(
                name: "AnhSuCoId",
                table: "AnhSuCo",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "MucUuTienId",
                table: "MucSla",
                newName: "MucDoUuTien");

            migrationBuilder.RenameColumn(
                name: "VaiTroTrongHo",
                table: "CuDan",
                newName: "VaiTro");

            migrationBuilder.RenameColumn(
                name: "LaDaiDien",
                table: "CuDan",
                newName: "DaiDienCanHo");

            migrationBuilder.RenameColumn(
                name: "GhiChuCuTru",
                table: "CuDan",
                newName: "GhiChu");

            migrationBuilder.RenameColumn(
                name: "CuTruId",
                table: "CuDan",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_CuTru_NguoiDungId",
                table: "CuDan",
                newName: "IX_CuDan_NguoiDungId");

            migrationBuilder.AlterColumn<int>(
                name: "DuAnId",
                table: "ToaNha",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "KhuVucId",
                table: "ToaNha",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DonViTienTe",
                table: "ThanhToan",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "DaXuatHoaDon",
                table: "ChiSoDichVu",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ToaNhaId",
                table: "CanHo",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "NguoiDungId",
                table: "CuDan",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "CuDan",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HoTen",
                table: "CuDan",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SoDienThoai",
                table: "CuDan",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoGiayTo",
                table: "CuDan",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "UQ_Tang_Id_ToaNha",
                table: "Tang",
                columns: new[] { "Id", "ToaNhaId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_MucSla",
                table: "MucSla",
                column: "MucDoUuTien");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CuDan",
                table: "CuDan",
                column: "Id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_YeuCauBaoTri_TrangThai",
                table: "YeuCauBaoTri",
                sql: "\"TrangThai\" BETWEEN 0 AND 7");

            migrationBuilder.CreateIndex(
                name: "IX_ToaNha_KhuVucId",
                table: "ToaNha",
                column: "KhuVucId");

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
                name: "CK_HoaDon_SoTien",
                table: "HoaDon",
                sql: "\"TongTien\" >= 0 AND \"DaThanhToan\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChiSoDichVu_ChiSo",
                table: "ChiSoDichVu",
                sql: "\"ChiSoMoi\" >= \"ChiSoCu\"");

            migrationBuilder.CreateIndex(
                name: "IX_CanHo_TangId_ToaNhaId",
                table: "CanHo",
                columns: new[] { "TangId", "ToaNhaId" });

            migrationBuilder.CreateIndex(
                name: "IX_CanHo_ToaNhaId_MaCanHo",
                table: "CanHo",
                columns: new[] { "ToaNhaId", "MaCanHo" },
                unique: true,
                filter: "\"DaXoa\" = false");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CanHo_SoPhongNgu",
                table: "CanHo",
                sql: "\"SoPhongNgu\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MucSla_ThoiHan",
                table: "MucSla",
                sql: "\"PhutPhanHoi\" > 0 AND \"PhutHoanTat\" >= \"PhutPhanHoi\"");

            migrationBuilder.CreateIndex(
                name: "IX_CuDan_CanHoId",
                table: "CuDan",
                column: "CanHoId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CuDan_NgayChuyenDi",
                table: "CuDan",
                sql: "\"NgayChuyenDi\" IS NULL OR \"NgayChuyenDi\" >= \"NgayVaoO\"");

            migrationBuilder.AddForeignKey(
                name: "FK_AnhSuCo_NguoiDung_NguoiTaiLenId",
                table: "AnhSuCo",
                column: "NguoiTaiLenId",
                principalTable: "NguoiDung",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CanHo_Tang_ToaNha",
                table: "CanHo",
                columns: new[] { "TangId", "ToaNhaId" },
                principalTable: "Tang",
                principalColumns: new[] { "Id", "ToaNhaId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CanHo_ToaNha_ToaNhaId",
                table: "CanHo",
                column: "ToaNhaId",
                principalTable: "ToaNha",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CuDan_CanHo_CanHoId",
                table: "CuDan",
                column: "CanHoId",
                principalTable: "CanHo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CuDan_NguoiDung_NguoiDungId",
                table: "CuDan",
                column: "NguoiDungId",
                principalTable: "NguoiDung",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_LichSuTrangThaiSuCo_NguoiDung_NguoiThucHienId",
                table: "LichSuTrangThaiSuCo",
                column: "NguoiThucHienId",
                principalTable: "NguoiDung",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tang_ToaNha_ToaNhaId",
                table: "Tang",
                column: "ToaNhaId",
                principalTable: "ToaNha",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ToaNha_DuAn_DuAnId",
                table: "ToaNha",
                column: "DuAnId",
                principalTable: "DuAn",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ToaNha_KhuVuc_KhuVucId",
                table: "ToaNha",
                column: "KhuVucId",
                principalTable: "KhuVuc",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_YeuCauBaoTri_MucSla_MucDoUuTien",
                table: "YeuCauBaoTri",
                column: "MucDoUuTien",
                principalTable: "MucSla",
                principalColumn: "MucDoUuTien",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_YeuCauBaoTri_NguoiDung_NguoiBaoCaoId",
                table: "YeuCauBaoTri",
                column: "NguoiBaoCaoId",
                principalTable: "NguoiDung",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_YeuCauBaoTri_NguoiDung_NguoiPhuTrachId",
                table: "YeuCauBaoTri",
                column: "NguoiPhuTrachId",
                principalTable: "NguoiDung",
                principalColumn: "Id");
#pragma warning restore CS0162
        }
    }
}
