using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace t.Migrations
{
    /// <inheritdoc />
    public partial class Tuan9GopTangVaoCanHo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tầng không có thông tin riêng (chỉ toà + số tầng) nên gộp vào căn: mỗi căn ghi ToaNhaId, SoTang.
            migrationBuilder.DropForeignKey(name: "FK_CanHo_Tang_TangId", table: "CanHo");
            migrationBuilder.AddColumn<int>(name: "SoTang", table: "CanHo", type: "integer", nullable: false, defaultValue: 0);

            // Chuyển dữ liệu: số tầng và toà lấy từ dòng Tang của căn; sau đó cột TangId mang mã toà.
            migrationBuilder.Sql(@"
UPDATE ""CanHo"" c SET ""SoTang"" = t.""SoTang"", ""TangId"" = t.""ToaNhaId""
FROM ""Tang"" t WHERE t.""TangId"" = c.""TangId"";
DO $$ BEGIN
  IF EXISTS (SELECT 1 FROM ""CanHo"" WHERE ""SoTang"" < 1) THEN
    RAISE EXCEPTION 'Còn căn chưa chuyển được số tầng';
  END IF;
END $$;");

            migrationBuilder.DropIndex(name: "IX_CanHo_TangId", table: "CanHo");
            migrationBuilder.RenameColumn(name: "TangId", table: "CanHo", newName: "ToaNhaId");
            migrationBuilder.DropTable(name: "Tang");

            migrationBuilder.CreateIndex(name: "IX_CanHo_ToaNhaId_SoTang", table: "CanHo", columns: new[] { "ToaNhaId", "SoTang" });
            migrationBuilder.AddCheckConstraint(name: "CK_CanHo_SoTang", table: "CanHo", sql: "\"SoTang\" >= 1");
            migrationBuilder.AddForeignKey(name: "FK_CanHo_ToaNha_ToaNhaId", table: "CanHo", column: "ToaNhaId",
                principalTable: "ToaNha", principalColumn: "ToaNhaId", onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Khôi phục bằng bản sao lưu db_backup/LuxeHavenBql_truoc_gop_tang_20261126.dump");
        }
    }
}
