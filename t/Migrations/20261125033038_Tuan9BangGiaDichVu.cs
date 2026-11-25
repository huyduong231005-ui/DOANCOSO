using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace t.Migrations
{
    /// <summary>
    /// Bảng giá dịch vụ duy nhất và bảng chi tiết hoá đơn chuẩn:
    /// LoaiDichVu thêm CachTinh và các dòng phí quản lý, gửi xe, sửa chữa (bỏ PhiQuanLyM2 của ToaNha);
    /// PhiDinhKy (lặp tên, số tiền) thành DangKyDichVu(CanHoId, LoaiDichVuId, SoLuong);
    /// ChiTietHoaDon bỏ NoiDung, LoaiKhoan và ba khoá ngoại nguồn, khoá chính (HoaDonId, LoaiDichVuId).
    /// Dữ liệu cũ được chuyển sang, các dòng cùng dịch vụ trên một hoá đơn gộp số lượng.
    /// </summary>
    public partial class Tuan9BangGiaDichVu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
DROP VIEW IF EXISTS vw_doi_chieu_hoa_don;

-- 1. Bảng giá: thêm cách tính và các loại dịch vụ trước đây nằm rải ở ToaNha, PhiDinhKy.
ALTER TABLE "LoaiDichVu" ADD "CachTinh" integer NOT NULL DEFAULT 1;
ALTER TABLE "LoaiDichVu" ALTER "CachTinh" DROP DEFAULT;
INSERT INTO "LoaiDichVu" ("MaLoaiDichVu", "TenLoaiDichVu", "DonViTinh", "DonGia", "CachTinh", "NgayTao", "DaXoa") VALUES
  ('QL', 'Phí quản lý', 'm²', 15000, 0, now() at time zone 'utc', false),
  ('XEMAY', 'Gửi xe máy', 'xe', 120000, 2, now() at time zone 'utc', false),
  ('OTO', 'Gửi ô tô', 'xe', 1500000, 2, now() at time zone 'utc', false),
  ('SC', 'Sửa chữa', 'lần', 0, 3, now() at time zone 'utc', false);
ALTER TABLE "LoaiDichVu" ADD CONSTRAINT "CK_LoaiDichVu_CachTinh" CHECK ("CachTinh" BETWEEN 0 AND 3);

-- Tên phí cũ → mã dịch vụ. Dừng nếu còn tên không nhận ra, để không mất dòng nào.
CREATE TEMP TABLE map_phi ON COMMIT DROP AS
  SELECT p."PhiDinhKyId", p."CanHoId", p."DaXoa", p."DangApDung", d."LoaiDichVuId"
  FROM "PhiDinhKy" p
  LEFT JOIN "LoaiDichVu" d ON d."MaLoaiDichVu" = CASE
       WHEN p."TenPhi" LIKE 'Gửi xe máy%' THEN 'XEMAY'
       WHEN p."TenPhi" LIKE 'Gửi ô tô%' THEN 'OTO' END;
DO $$ BEGIN
  IF EXISTS (SELECT 1 FROM map_phi WHERE "LoaiDichVuId" IS NULL) THEN
    RAISE EXCEPTION 'Còn khoản phí định kỳ không ánh xạ được sang loại dịch vụ';
  END IF;
  IF EXISTS (SELECT 1 FROM "ChiTietHoaDon" WHERE "LoaiKhoan" NOT IN (0, 1, 2) AND NOT "DaXoa") THEN
    RAISE EXCEPTION 'Còn dòng hoá đơn loại sửa chữa/khác, cần ánh xạ tay';
  END IF;
END $$;

-- 2. Đăng ký dịch vụ: mỗi căn một dòng cho mỗi dịch vụ, số lượng = số khoản cũ đang áp dụng.
CREATE TABLE "DangKyDichVu" (
  "CanHoId" integer NOT NULL,
  "LoaiDichVuId" integer NOT NULL,
  "SoLuong" integer NOT NULL,
  CONSTRAINT "PK_DangKyDichVu" PRIMARY KEY ("CanHoId", "LoaiDichVuId"),
  CONSTRAINT "FK_DangKyDichVu_CanHo_CanHoId" FOREIGN KEY ("CanHoId") REFERENCES "CanHo" ("CanHoId") ON DELETE CASCADE,
  CONSTRAINT "FK_DangKyDichVu_LoaiDichVu_LoaiDichVuId" FOREIGN KEY ("LoaiDichVuId") REFERENCES "LoaiDichVu" ("LoaiDichVuId") ON DELETE RESTRICT,
  CONSTRAINT "CK_DangKyDichVu_SoLuong" CHECK ("SoLuong" > 0));
CREATE INDEX "IX_DangKyDichVu_LoaiDichVuId" ON "DangKyDichVu" ("LoaiDichVuId");
INSERT INTO "DangKyDichVu" ("CanHoId", "LoaiDichVuId", "SoLuong")
  SELECT "CanHoId", "LoaiDichVuId", count(*) FROM map_phi WHERE NOT "DaXoa" AND "DangApDung"
  GROUP BY "CanHoId", "LoaiDichVuId";

-- 3. Chi tiết hoá đơn: dòng cũ → (hoá đơn, loại dịch vụ). Cùng dịch vụ phải cùng đơn giá thì mới gộp.
CREATE TEMP TABLE dong_cu ON COMMIT DROP AS
  SELECT ct."HoaDonId",
         CASE ct."LoaiKhoan"
           WHEN 0 THEN (SELECT "LoaiDichVuId" FROM "LoaiDichVu" WHERE "MaLoaiDichVu" = 'QL')
           WHEN 1 THEN cs."LoaiDichVuId"
           WHEN 2 THEN mp."LoaiDichVuId" END AS "LoaiDichVuId",
         ct."SoLuong", ct."DonGia"
  FROM "ChiTietHoaDon" ct
  LEFT JOIN "ChiSoDichVu" cs ON cs."ChiSoDichVuId" = ct."ChiSoDichVuId"
  LEFT JOIN map_phi mp ON mp."PhiDinhKyId" = ct."PhiDinhKyId"
  WHERE NOT ct."DaXoa";
DO $$ BEGIN
  IF EXISTS (SELECT 1 FROM dong_cu WHERE "LoaiDichVuId" IS NULL) THEN
    RAISE EXCEPTION 'Có dòng hoá đơn không xác định được loại dịch vụ';
  END IF;
  IF EXISTS (SELECT 1 FROM dong_cu GROUP BY "HoaDonId", "LoaiDichVuId" HAVING count(DISTINCT "DonGia") > 1) THEN
    RAISE EXCEPTION 'Có hoá đơn mang cùng dịch vụ với hai đơn giá khác nhau';
  END IF;
END $$;

DROP TABLE "ChiTietHoaDon";
DROP TABLE "PhiDinhKy";
CREATE TABLE "ChiTietHoaDon" (
  "HoaDonId" integer NOT NULL,
  "LoaiDichVuId" integer NOT NULL,
  "SoLuong" numeric(18,2) NOT NULL,
  "DonGia" numeric(18,0) NOT NULL,
  "ThanhTien" numeric(18,0) GENERATED ALWAYS AS (round("SoLuong" * "DonGia")) STORED NOT NULL,
  CONSTRAINT "PK_ChiTietHoaDon" PRIMARY KEY ("HoaDonId", "LoaiDichVuId"),
  CONSTRAINT "FK_ChiTietHoaDon_HoaDon_HoaDonId" FOREIGN KEY ("HoaDonId") REFERENCES "HoaDon" ("HoaDonId") ON DELETE CASCADE,
  CONSTRAINT "FK_ChiTietHoaDon_LoaiDichVu_LoaiDichVuId" FOREIGN KEY ("LoaiDichVuId") REFERENCES "LoaiDichVu" ("LoaiDichVuId") ON DELETE RESTRICT,
  CONSTRAINT "CK_ChiTietHoaDon_SoTien" CHECK ("SoLuong" > 0 AND "DonGia" >= 0));
CREATE INDEX "IX_ChiTietHoaDon_LoaiDichVuId" ON "ChiTietHoaDon" ("LoaiDichVuId");
INSERT INTO "ChiTietHoaDon" ("HoaDonId", "LoaiDichVuId", "SoLuong", "DonGia")
  SELECT "HoaDonId", "LoaiDichVuId", sum("SoLuong"), min("DonGia") FROM dong_cu GROUP BY "HoaDonId", "LoaiDichVuId";

-- 4. Đơn giá phí quản lý chỉ còn ở bảng giá.
ALTER TABLE "ToaNha" DROP CONSTRAINT "CK_ToaNha_PhiQuanLyM2";
ALTER TABLE "ToaNha" DROP COLUMN "PhiQuanLyM2";

-- 5. Chỉ số đã tính tiền = căn đã có hoá đơn phí tháng chưa huỷ cùng kỳ: không sửa, không xoá.
CREATE OR REPLACE FUNCTION fn_khoa_chi_so_da_xuat() RETURNS trigger LANGUAGE plpgsql AS $f$
BEGIN
  IF EXISTS (SELECT 1 FROM "HoaDon" h
             WHERE h."CanHoId" = OLD."CanHoId" AND h."Ky" = OLD."Ky" AND h."LoaiHoaDon" = 0
               AND h."TrangThaiHoaDon" <> 5 AND NOT h."DaXoa") THEN
    RAISE EXCEPTION 'Chỉ số % đã đưa vào hoá đơn, không thể sửa hoặc xoá', OLD."ChiSoDichVuId";
  END IF;
  IF TG_OP = 'DELETE' THEN RETURN OLD; END IF;
  RETURN NEW;
END $f$;

-- 6. Khung nhìn đối chiếu: tiền phí ghi ở hoá đơn so với tổng thành tiền các dòng, đã thu so với các lần thu.
CREATE VIEW vw_doi_chieu_hoa_don AS
SELECT h."HoaDonId", h."SoHoaDon", h."TienPhi", h."DaThu",
       COALESCE((SELECT sum(ct."ThanhTien") FROM "ChiTietHoaDon" ct WHERE ct."HoaDonId" = h."HoaDonId"), 0) AS "TienPhiTinhLai",
       COALESCE((SELECT sum(t."SoTien" - t."SoTienHoanTra") FROM "ThanhToan" t
                 WHERE t."HoaDonId" = h."HoaDonId" AND NOT t."DaXoa" AND t."TrangThaiThanhToan" IN (1, 3, 4)), 0) AS "DaThuTinhLai"
FROM "HoaDon" h WHERE NOT h."DaXoa";
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ponytail: không viết bước lùi cho lần đổi lược đồ này; khôi phục bằng bản sao lưu trước khi chạy.
            throw new NotSupportedException("Khôi phục bằng bản sao lưu db_backup/LuxeHavenBql_truoc_bang_gia_20261125.dump");
        }
    }
}
