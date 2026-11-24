using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace t.Migrations
{
    /// <inheritdoc />
    public partial class Tuan8TenCotDeHieu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_YeuCauBaoTri_ToaNha_ToaNhaKhuVucChungId",
                table: "YeuCauBaoTri");

            migrationBuilder.DropCheckConstraint(
                name: "CK_YeuCauBaoTri_DanhGia",
                table: "YeuCauBaoTri");

            migrationBuilder.DropCheckConstraint(
                name: "CK_YeuCauBaoTri_LoaiBaoTri",
                table: "YeuCauBaoTri");

            migrationBuilder.DropCheckConstraint(
                name: "CK_YeuCauBaoTri_NoiXayRa",
                table: "YeuCauBaoTri");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ToaNha_PhiTre",
                table: "ToaNha");

            migrationBuilder.RenameColumn(
                name: "ToaNhaKhuVucChungId",
                table: "YeuCauBaoTri",
                newName: "ToaNhaId");

            migrationBuilder.RenameColumn(
                name: "TinhPhiKhachThue",
                table: "YeuCauBaoTri",
                newName: "TinhPhiCuDan");

            migrationBuilder.RenameColumn(
                name: "LoaiBaoTri",
                table: "YeuCauBaoTri",
                newName: "LoaiSuCo");

            migrationBuilder.RenameColumn(
                name: "KetQuaAiJson",
                table: "YeuCauBaoTri",
                newName: "PhanLoaiAi");

            migrationBuilder.RenameColumn(
                name: "DanhGia",
                table: "YeuCauBaoTri",
                newName: "DiemDanhGia");

            migrationBuilder.RenameIndex(
                name: "IX_YeuCauBaoTri_ToaNhaKhuVucChungId",
                table: "YeuCauBaoTri",
                newName: "IX_YeuCauBaoTri_ToaNhaId");

            migrationBuilder.RenameColumn(
                name: "SoNgayTreHan",
                table: "ToaNha",
                newName: "SoNgayAnHan");

            migrationBuilder.RenameColumn(
                name: "KhoaBang",
                table: "NhatKyHeThong",
                newName: "KhoaDong");

            migrationBuilder.RenameIndex(
                name: "IX_NhatKyHeThong_TenBang_KhoaBang",
                table: "NhatKyHeThong",
                newName: "IX_NhatKyHeThong_TenBang_KhoaDong");

            // Mã loại dịch vụ bằng tiếng Việt cho người đọc.
            migrationBuilder.Sql("UPDATE \"LoaiDichVu\" SET \"MaLoaiDichVu\" = CASE \"MaLoaiDichVu\" WHEN 'ELEC' THEN 'DIEN' WHEN 'WATER' THEN 'NUOC' ELSE \"MaLoaiDichVu\" END;");

            migrationBuilder.AddCheckConstraint(
                name: "CK_YeuCauBaoTri_DanhGia",
                table: "YeuCauBaoTri",
                sql: "\"DiemDanhGia\" IS NULL OR \"DiemDanhGia\" BETWEEN 1 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "CK_YeuCauBaoTri_LoaiSuCo",
                table: "YeuCauBaoTri",
                sql: "\"LoaiSuCo\" IN (0, 1, 2, 3, 4, 5, 6, 99)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_YeuCauBaoTri_NoiXayRa",
                table: "YeuCauBaoTri",
                sql: "(\"CanHoId\" IS NOT NULL AND \"ToaNhaId\" IS NULL) OR (\"CanHoId\" IS NULL AND \"ToaNhaId\" IS NOT NULL AND \"ViTri\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ToaNha_PhiTre",
                table: "ToaNha",
                sql: "\"SoNgayAnHan\" >= 0 AND \"PhanTramPhiTre\" BETWEEN 0 AND 100");

            migrationBuilder.AddForeignKey(
                name: "FK_YeuCauBaoTri_ToaNha_ToaNhaId",
                table: "YeuCauBaoTri",
                column: "ToaNhaId",
                principalTable: "ToaNha",
                principalColumn: "ToaNhaId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"LoaiDichVu\" SET \"MaLoaiDichVu\" = CASE \"MaLoaiDichVu\" WHEN 'DIEN' THEN 'ELEC' WHEN 'NUOC' THEN 'WATER' ELSE \"MaLoaiDichVu\" END;");
            migrationBuilder.DropForeignKey(
                name: "FK_YeuCauBaoTri_ToaNha_ToaNhaId",
                table: "YeuCauBaoTri");

            migrationBuilder.DropCheckConstraint(
                name: "CK_YeuCauBaoTri_DanhGia",
                table: "YeuCauBaoTri");

            migrationBuilder.DropCheckConstraint(
                name: "CK_YeuCauBaoTri_LoaiSuCo",
                table: "YeuCauBaoTri");

            migrationBuilder.DropCheckConstraint(
                name: "CK_YeuCauBaoTri_NoiXayRa",
                table: "YeuCauBaoTri");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ToaNha_PhiTre",
                table: "ToaNha");

            migrationBuilder.RenameColumn(
                name: "ToaNhaId",
                table: "YeuCauBaoTri",
                newName: "ToaNhaKhuVucChungId");

            migrationBuilder.RenameColumn(
                name: "TinhPhiCuDan",
                table: "YeuCauBaoTri",
                newName: "TinhPhiKhachThue");

            migrationBuilder.RenameColumn(
                name: "PhanLoaiAi",
                table: "YeuCauBaoTri",
                newName: "KetQuaAiJson");

            migrationBuilder.RenameColumn(
                name: "LoaiSuCo",
                table: "YeuCauBaoTri",
                newName: "LoaiBaoTri");

            migrationBuilder.RenameColumn(
                name: "DiemDanhGia",
                table: "YeuCauBaoTri",
                newName: "DanhGia");

            migrationBuilder.RenameIndex(
                name: "IX_YeuCauBaoTri_ToaNhaId",
                table: "YeuCauBaoTri",
                newName: "IX_YeuCauBaoTri_ToaNhaKhuVucChungId");

            migrationBuilder.RenameColumn(
                name: "SoNgayAnHan",
                table: "ToaNha",
                newName: "SoNgayTreHan");

            migrationBuilder.RenameColumn(
                name: "KhoaDong",
                table: "NhatKyHeThong",
                newName: "KhoaBang");

            migrationBuilder.RenameIndex(
                name: "IX_NhatKyHeThong_TenBang_KhoaDong",
                table: "NhatKyHeThong",
                newName: "IX_NhatKyHeThong_TenBang_KhoaBang");

            migrationBuilder.AddCheckConstraint(
                name: "CK_YeuCauBaoTri_DanhGia",
                table: "YeuCauBaoTri",
                sql: "\"DanhGia\" IS NULL OR \"DanhGia\" BETWEEN 1 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "CK_YeuCauBaoTri_LoaiBaoTri",
                table: "YeuCauBaoTri",
                sql: "\"LoaiBaoTri\" IN (0, 1, 2, 3, 4, 5, 6, 99)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_YeuCauBaoTri_NoiXayRa",
                table: "YeuCauBaoTri",
                sql: "(\"CanHoId\" IS NOT NULL AND \"ToaNhaKhuVucChungId\" IS NULL) OR (\"CanHoId\" IS NULL AND \"ToaNhaKhuVucChungId\" IS NOT NULL AND \"ViTri\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ToaNha_PhiTre",
                table: "ToaNha",
                sql: "\"SoNgayTreHan\" >= 0 AND \"PhanTramPhiTre\" BETWEEN 0 AND 100");

            migrationBuilder.AddForeignKey(
                name: "FK_YeuCauBaoTri_ToaNha_ToaNhaKhuVucChungId",
                table: "YeuCauBaoTri",
                column: "ToaNhaKhuVucChungId",
                principalTable: "ToaNha",
                principalColumn: "ToaNhaId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
