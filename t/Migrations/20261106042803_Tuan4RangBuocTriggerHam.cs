using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace t.Migrations
{
    /// <inheritdoc />
    public partial class Tuan4RangBuocTriggerHam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CanHo_Tang_TangId",
                table: "CanHo");

            migrationBuilder.DropIndex(
                name: "IX_CanHo_TangId",
                table: "CanHo");

            migrationBuilder.AddUniqueConstraint(
                name: "UQ_Tang_Id_ToaNha",
                table: "Tang",
                columns: new[] { "Id", "ToaNhaId" });

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauBaoTri_NgayBaoCao",
                table: "YeuCauBaoTri",
                column: "NgayBaoCao");

            migrationBuilder.AddCheckConstraint(
                name: "CK_YeuCauBaoTri_LoaiBaoTri",
                table: "YeuCauBaoTri",
                sql: "\"LoaiBaoTri\" IN (0, 1, 2, 3, 4, 5, 6, 99)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_YeuCauBaoTri_TrangThai",
                table: "YeuCauBaoTri",
                sql: "\"TrangThai\" BETWEEN 0 AND 7");

            migrationBuilder.CreateIndex(
                name: "IX_CanHo_TangId_ToaNhaId",
                table: "CanHo",
                columns: new[] { "TangId", "ToaNhaId" });

            migrationBuilder.AddForeignKey(
                name: "FK_CanHo_Tang_ToaNha",
                table: "CanHo",
                columns: new[] { "TangId", "ToaNhaId" },
                principalTable: "Tang",
                principalColumns: new[] { "Id", "ToaNhaId" },
                onDelete: ReferentialAction.Restrict);

            // Lớp bảo vệ thứ hai ở CSDL (báo cáo mục 2.5.3): chặn cặp trạng thái ngoài bảng Rules của
            // MaintenanceWorkflow khi có người sửa thẳng trong CSDL. Chỉ kiểm tra, không ghi dữ liệu; lịch sử
            // giao việc vẫn chỉ do MaintenanceWorkflow ghi nên không cài sp_giao_viec (tránh ghi trùng).
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION fn_kiem_tra_chuyen_trang_thai() RETURNS trigger AS $$
                BEGIN
                  IF NEW."TrangThai" = OLD."TrangThai" THEN RETURN NEW; END IF;
                  IF (OLD."TrangThai", NEW."TrangThai") NOT IN
                     ((0,1),(0,5),(0,7),(1,2),(1,5),(1,7),(2,6),(2,3),(6,2),(3,4),(3,2)) THEN
                    RAISE EXCEPTION 'Yêu cầu % không thể chuyển từ trạng thái % sang %',
                      OLD."SoYeuCau", OLD."TrangThai", NEW."TrangThai";
                  END IF;
                  IF NEW."TrangThai" = 2 AND NEW."NguoiPhuTrachId" IS NULL THEN
                    RAISE EXCEPTION 'Phải giao kỹ thuật viên trước khi bắt đầu xử lý';
                  END IF;
                  IF NEW."TrangThai" = 3 AND coalesce(trim(NEW."GhiChuXuLy"), '') = '' THEN
                    RAISE EXCEPTION 'Phải ghi kết quả xử lý trước khi hoàn tất';
                  END IF;
                  RETURN NEW;
                END $$ LANGUAGE plpgsql;

                CREATE TRIGGER trg_yeu_cau_chuyen_trang_thai
                  BEFORE UPDATE OF "TrangThai" ON "YeuCauBaoTri"
                  FOR EACH ROW EXECUTE FUNCTION fn_kiem_tra_chuyen_trang_thai();

                CREATE OR REPLACE FUNCTION fn_han_xu_ly(p_ngay_bao timestamp, p_muc int) RETURNS timestamp AS $$
                  SELECT p_ngay_bao + make_interval(mins => s."PhutHoanTat") FROM "MucSla" s WHERE s."MucDoUuTien" = p_muc;
                $$ LANGUAGE sql STABLE;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS trg_yeu_cau_chuyen_trang_thai ON "YeuCauBaoTri";
                DROP FUNCTION IF EXISTS fn_kiem_tra_chuyen_trang_thai();
                DROP FUNCTION IF EXISTS fn_han_xu_ly(timestamp, int);
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_CanHo_Tang_ToaNha",
                table: "CanHo");

            migrationBuilder.DropIndex(
                name: "IX_YeuCauBaoTri_NgayBaoCao",
                table: "YeuCauBaoTri");

            migrationBuilder.DropCheckConstraint(
                name: "CK_YeuCauBaoTri_LoaiBaoTri",
                table: "YeuCauBaoTri");

            migrationBuilder.DropCheckConstraint(
                name: "CK_YeuCauBaoTri_TrangThai",
                table: "YeuCauBaoTri");

            migrationBuilder.DropUniqueConstraint(
                name: "UQ_Tang_Id_ToaNha",
                table: "Tang");

            migrationBuilder.DropIndex(
                name: "IX_CanHo_TangId_ToaNhaId",
                table: "CanHo");

            migrationBuilder.CreateIndex(
                name: "IX_CanHo_TangId",
                table: "CanHo",
                column: "TangId");

            migrationBuilder.AddForeignKey(
                name: "FK_CanHo_Tang_TangId",
                table: "CanHo",
                column: "TangId",
                principalTable: "Tang",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
