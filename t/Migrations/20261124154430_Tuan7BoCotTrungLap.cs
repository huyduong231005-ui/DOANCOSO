using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace t.Migrations
{
    /// <inheritdoc />
    public partial class Tuan7BoCotTrungLap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ToaNha_SoLuongTang",
                table: "ToaNha");

            migrationBuilder.DropColumn(
                name: "NgayDong",
                table: "YeuCauBaoTri");

            migrationBuilder.DropColumn(
                name: "NgayGiaiQuyet",
                table: "YeuCauBaoTri");

            migrationBuilder.DropColumn(
                name: "NgayTiepNhan",
                table: "YeuCauBaoTri");

            migrationBuilder.DropColumn(
                name: "SoLuongTang",
                table: "ToaNha");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "NguoiDung");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChiTietHoaDon_ThanhTien",
                table: "ChiTietHoaDon",
                sql: "\"ThanhTienKhoan\" = round(\"SoLuongKhoan\" * \"DonGiaKhoan\")");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChiSoDichVu_TieuThu",
                table: "ChiSoDichVu",
                sql: "\"TieuThu\" = \"ChiSoMoi\" - \"ChiSoCu\" AND \"ThanhTienChiSo\" = round(\"TieuThu\" * \"DonGiaChiSo\")");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ChiTietHoaDon_ThanhTien",
                table: "ChiTietHoaDon");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ChiSoDichVu_TieuThu",
                table: "ChiSoDichVu");

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayDong",
                table: "YeuCauBaoTri",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayGiaiQuyet",
                table: "YeuCauBaoTri",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTiepNhan",
                table: "YeuCauBaoTri",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SoLuongTang",
                table: "ToaNha",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "NguoiDung",
                type: "text",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ToaNha_SoLuongTang",
                table: "ToaNha",
                sql: "\"SoLuongTang\" > 0");
        }
    }
}
