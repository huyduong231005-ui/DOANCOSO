using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace t.Migrations
{
    /// <inheritdoc />
    public partial class InitPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DanhMuc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ten = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Icon = table.Column<string>(type: "text", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false),
                    NgayXoa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXoa = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    PhienBan = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhMuc", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KhuVuc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ten = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    UrlAnh = table.Column<string>(type: "text", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false),
                    NgayXoa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXoa = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    PhienBan = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhuVuc", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoaiDichVu",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ma = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Ten = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DonViTinh = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CheDoTinhPhi = table.Column<int>(type: "integer", nullable: false),
                    DonGiaMacDinh = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Icon = table.Column<string>(type: "text", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false),
                    NgayXoa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXoa = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    PhienBan = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoaiDichVu", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NguoiDung",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    HoTen = table.Column<string>(type: "text", nullable: false),
                    UrlAvatar = table.Column<string>(type: "text", nullable: true),
                    SoDienThoai = table.Column<string>(type: "text", nullable: true),
                    LaChuNha = table.Column<bool>(type: "boolean", nullable: false),
                    DanhXungChuNha = table.Column<string>(type: "text", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false),
                    NgayXoa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXoa = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayDangNhapCuoi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NguoiDung", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NhatKyHeThong",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenBang = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    KhoaBang = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    HanhDong = table.Column<int>(type: "integer", nullable: false),
                    NguoiDungId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    TenNguoiDung = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ThoiGian = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GiaTriCu = table.Column<string>(type: "text", nullable: true),
                    GiaTriMoi = table.Column<string>(type: "text", nullable: true),
                    CotThayDoi = table.Column<string>(type: "text", nullable: true),
                    DiaChiIp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhatKyHeThong", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Quyen",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ma = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Module = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    TenHienThi = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MoTa = table.Column<string>(type: "text", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false),
                    NgayXoa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXoa = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    PhienBan = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quyen", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TienIch",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ten = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Icon = table.Column<string>(type: "text", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false),
                    NgayXoa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXoa = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    PhienBan = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TienIch", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VaiTro",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaiTro", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DuAn",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ten = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    KhuVucId = table.Column<int>(type: "integer", nullable: false),
                    DiaChi = table.Column<string>(type: "text", nullable: false),
                    UrlAnhDaiDien = table.Column<string>(type: "text", nullable: false),
                    GiaTu = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    TrangThai = table.Column<int>(type: "integer", nullable: false),
                    MoTaNgan = table.Column<string>(type: "text", nullable: false),
                    MoTaDayDu = table.Column<string>(type: "text", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false),
                    NgayXoa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXoa = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    PhienBan = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DuAn", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DuAn_KhuVuc_KhuVucId",
                        column: x => x.KhuVucId,
                        principalTable: "KhuVuc",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NguoiDung_Claim",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NguoiDungId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NguoiDung_Claim", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NguoiDung_Claim_NguoiDung_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NguoiDung_DangNhap",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    NguoiDungId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NguoiDung_DangNhap", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_NguoiDung_DangNhap_NguoiDung_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NguoiDung_Token",
                columns: table => new
                {
                    NguoiDungId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NguoiDung_Token", x => new { x.NguoiDungId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_NguoiDung_Token_NguoiDung_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NguoiDung_VaiTro",
                columns: table => new
                {
                    NguoiDungId = table.Column<string>(type: "text", nullable: false),
                    VaiTroId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NguoiDung_VaiTro", x => new { x.NguoiDungId, x.VaiTroId });
                    table.ForeignKey(
                        name: "FK_NguoiDung_VaiTro_NguoiDung_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NguoiDung_VaiTro_VaiTro_VaiTroId",
                        column: x => x.VaiTroId,
                        principalTable: "VaiTro",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaiTro_Claim",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VaiTroId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaiTro_Claim", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VaiTro_Claim_VaiTro_VaiTroId",
                        column: x => x.VaiTroId,
                        principalTable: "VaiTro",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaiTro_Quyen",
                columns: table => new
                {
                    VaiTroId = table.Column<string>(type: "text", nullable: false),
                    QuyenId = table.Column<int>(type: "integer", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaiTro_Quyen", x => new { x.VaiTroId, x.QuyenId });
                    table.ForeignKey(
                        name: "FK_VaiTro_Quyen_Quyen_QuyenId",
                        column: x => x.QuyenId,
                        principalTable: "Quyen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VaiTro_Quyen_VaiTro_VaiTroId",
                        column: x => x.VaiTroId,
                        principalTable: "VaiTro",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnhDuAn",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DuAnId = table.Column<int>(type: "integer", nullable: false),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ChuThich = table.Column<string>(type: "text", nullable: true),
                    ThuTu = table.Column<int>(type: "integer", nullable: false),
                    AnhBia = table.Column<bool>(type: "boolean", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false),
                    NgayXoa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXoa = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    PhienBan = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnhDuAn", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnhDuAn_DuAn_DuAnId",
                        column: x => x.DuAnId,
                        principalTable: "DuAn",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ToaNha",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ten = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    Ma = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DuAnId = table.Column<int>(type: "integer", nullable: true),
                    KhuVucId = table.Column<int>(type: "integer", nullable: false),
                    DiaChi = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SoLuongTang = table.Column<int>(type: "integer", nullable: false),
                    UrlAnhDaiDien = table.Column<string>(type: "text", nullable: true),
                    MoTa = table.Column<string>(type: "text", nullable: true),
                    QuanLyId = table.Column<string>(type: "text", nullable: true),
                    TrangThai = table.Column<int>(type: "integer", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false),
                    NgayXoa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXoa = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    PhienBan = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToaNha", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToaNha_DuAn_DuAnId",
                        column: x => x.DuAnId,
                        principalTable: "DuAn",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ToaNha_KhuVuc_KhuVucId",
                        column: x => x.KhuVucId,
                        principalTable: "KhuVuc",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ToaNha_NguoiDung_QuanLyId",
                        column: x => x.QuanLyId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Tang",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ToaNhaId = table.Column<int>(type: "integer", nullable: false),
                    SoTang = table.Column<int>(type: "integer", nullable: false),
                    NhanTang = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false),
                    NgayXoa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXoa = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    PhienBan = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tang", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tang_ToaNha_ToaNhaId",
                        column: x => x.ToaNhaId,
                        principalTable: "ToaNha",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CanHo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TieuDe = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Slug = table.Column<string>(type: "character varying(280)", maxLength: 280, nullable: false),
                    MaCanHo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    MoTa = table.Column<string>(type: "text", nullable: true),
                    MoTaThem = table.Column<string>(type: "text", nullable: true),
                    Gia = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    TienDatCocMacDinh = table.Column<decimal>(type: "numeric(18,0)", nullable: true),
                    GhiChuPhi = table.Column<string>(type: "text", nullable: true),
                    DienTich = table.Column<double>(type: "double precision", nullable: false),
                    SoPhongNgu = table.Column<int>(type: "integer", nullable: false),
                    SoPhongTam = table.Column<int>(type: "integer", nullable: false),
                    DiaChi = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ViDo = table.Column<double>(type: "double precision", nullable: true),
                    KinhDo = table.Column<double>(type: "double precision", nullable: true),
                    TrangThai = table.Column<int>(type: "integer", nullable: false),
                    TinhTrangThue = table.Column<int>(type: "integer", nullable: false),
                    NoiBat = table.Column<bool>(type: "boolean", nullable: false),
                    LuotXem = table.Column<int>(type: "integer", nullable: false),
                    GhiChuKiemDuyet = table.Column<string>(type: "text", nullable: true),
                    NgayDuyet = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiDuyet = table.Column<string>(type: "text", nullable: true),
                    ChuNhaId = table.Column<string>(type: "text", nullable: false),
                    KhuVucId = table.Column<int>(type: "integer", nullable: false),
                    DanhMucId = table.Column<int>(type: "integer", nullable: false),
                    DuAnId = table.Column<int>(type: "integer", nullable: true),
                    ToaNhaId = table.Column<int>(type: "integer", nullable: true),
                    TangId = table.Column<int>(type: "integer", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false),
                    NgayXoa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXoa = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    PhienBan = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanHo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CanHo_DanhMuc_DanhMucId",
                        column: x => x.DanhMucId,
                        principalTable: "DanhMuc",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CanHo_DuAn_DuAnId",
                        column: x => x.DuAnId,
                        principalTable: "DuAn",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CanHo_KhuVuc_KhuVucId",
                        column: x => x.KhuVucId,
                        principalTable: "KhuVuc",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CanHo_NguoiDung_ChuNhaId",
                        column: x => x.ChuNhaId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CanHo_Tang_TangId",
                        column: x => x.TangId,
                        principalTable: "Tang",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CanHo_ToaNha_ToaNhaId",
                        column: x => x.ToaNhaId,
                        principalTable: "ToaNha",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AnhCanHo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ChuThich = table.Column<string>(type: "text", nullable: true),
                    AnhBia = table.Column<bool>(type: "boolean", nullable: false),
                    ThuTu = table.Column<int>(type: "integer", nullable: false),
                    CanHoId = table.Column<int>(type: "integer", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false),
                    NgayXoa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXoa = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    PhienBan = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnhCanHo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnhCanHo_CanHo_CanHoId",
                        column: x => x.CanHoId,
                        principalTable: "CanHo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CanHo_TienIch",
                columns: table => new
                {
                    CanHoId = table.Column<int>(type: "integer", nullable: false),
                    TienIchId = table.Column<int>(type: "integer", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanHo_TienIch", x => new { x.CanHoId, x.TienIchId });
                    table.ForeignKey(
                        name: "FK_CanHo_TienIch_CanHo_CanHoId",
                        column: x => x.CanHoId,
                        principalTable: "CanHo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CanHo_TienIch_TienIch_TienIchId",
                        column: x => x.TienIchId,
                        principalTable: "TienIch",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DanhGia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DiemSao = table.Column<int>(type: "integer", nullable: false),
                    NoiDung = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    GhiChuNguoiThue = table.Column<string>(type: "text", nullable: true),
                    TrangThai = table.Column<int>(type: "integer", nullable: false),
                    NgayDuyet = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiDuyet = table.Column<string>(type: "text", nullable: true),
                    CanHoId = table.Column<int>(type: "integer", nullable: false),
                    NguoiDungId = table.Column<string>(type: "text", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false),
                    NgayXoa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXoa = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    PhienBan = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhGia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DanhGia_CanHo_CanHoId",
                        column: x => x.CanHoId,
                        principalTable: "CanHo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DanhGia_NguoiDung_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HopDongThue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SoHopDong = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CanHoId = table.Column<int>(type: "integer", nullable: false),
                    NguoiThueChinhId = table.Column<string>(type: "text", nullable: false),
                    NgayBatDau = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NgayKetThuc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TienThueThang = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    TienDatCoc = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    TienDatCocDangGiu = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    TienDatCocDaHoanTra = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    NgayChotKy = table.Column<int>(type: "integer", nullable: false),
                    PhanTramPhiTre = table.Column<int>(type: "integer", nullable: false),
                    SoNgayTreHan = table.Column<int>(type: "integer", nullable: false),
                    TrangThai = table.Column<int>(type: "integer", nullable: false),
                    NgayKichHoat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NgayChamDut = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LyDoChamDut = table.Column<string>(type: "text", nullable: true),
                    UrlHopDong = table.Column<string>(type: "text", nullable: true),
                    GhiChu = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false),
                    NgayXoa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXoa = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    PhienBan = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HopDongThue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HopDongThue_CanHo_CanHoId",
                        column: x => x.CanHoId,
                        principalTable: "CanHo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HopDongThue_NguoiDung_NguoiThueChinhId",
                        column: x => x.NguoiThueChinhId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LichXemNha",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CanHoId = table.Column<int>(type: "integer", nullable: false),
                    NguoiDungId = table.Column<string>(type: "text", nullable: true),
                    TenLienHe = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SoDienThoaiLienHe = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EmailLienHe = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    NgayHen = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GioHen = table.Column<int>(type: "integer", nullable: false),
                    GhiChu = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TrangThai = table.Column<int>(type: "integer", nullable: false),
                    NgayXacNhan = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXacNhan = table.Column<string>(type: "text", nullable: true),
                    NgayHuy = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiHuy = table.Column<string>(type: "text", nullable: true),
                    LyDoHuy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false),
                    NgayXoa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXoa = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    PhienBan = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichXemNha", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LichXemNha_CanHo_CanHoId",
                        column: x => x.CanHoId,
                        principalTable: "CanHo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LichXemNha_NguoiDung_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "YeuThich",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NguoiDungId = table.Column<string>(type: "text", nullable: false),
                    CanHoId = table.Column<int>(type: "integer", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false),
                    NgayXoa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXoa = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    PhienBan = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YeuThich", x => x.Id);
                    table.ForeignKey(
                        name: "FK_YeuThich_CanHo_CanHoId",
                        column: x => x.CanHoId,
                        principalTable: "CanHo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_YeuThich_NguoiDung_NguoiDungId",
                        column: x => x.NguoiDungId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChiSoDichVu",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HopDongId = table.Column<int>(type: "integer", nullable: false),
                    LoaiDichVuId = table.Column<int>(type: "integer", nullable: false),
                    ThangTinhPhi = table.Column<int>(type: "integer", nullable: false),
                    ChiSoCu = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ChiSoMoi = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TieuThu = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DonGia = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ThanhTien = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    DaXuatHoaDon = table.Column<bool>(type: "boolean", nullable: false),
                    NgayDocChiSo = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GhiChu = table.Column<string>(type: "text", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false),
                    NgayXoa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXoa = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    PhienBan = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiSoDichVu", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChiSoDichVu_HopDongThue_HopDongId",
                        column: x => x.HopDongId,
                        principalTable: "HopDongThue",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiSoDichVu_LoaiDichVu_LoaiDichVuId",
                        column: x => x.LoaiDichVuId,
                        principalTable: "LoaiDichVu",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HoaDon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SoHoaDon = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    HopDongId = table.Column<int>(type: "integer", nullable: false),
                    LoaiHoaDon = table.Column<int>(type: "integer", nullable: false),
                    ThangTinhPhi = table.Column<int>(type: "integer", nullable: false),
                    DinhKy = table.Column<bool>(type: "boolean", nullable: false),
                    NgayPhatHanh = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NgayDenHan = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TamTinh = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    GiamGia = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    Thue = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    PhiTreHan = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    TongTien = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    DaThanhToan = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    ConLai = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    TrangThai = table.Column<int>(type: "integer", nullable: false),
                    DonViTienTe = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    GhiChu = table.Column<string>(type: "text", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false),
                    NgayXoa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXoa = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    PhienBan = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoaDon", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HoaDon_HopDongThue_HopDongId",
                        column: x => x.HopDongId,
                        principalTable: "HopDongThue",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HopDong_NguoiThue",
                columns: table => new
                {
                    HopDongId = table.Column<int>(type: "integer", nullable: false),
                    NguoiThueId = table.Column<string>(type: "text", nullable: false),
                    MoiQuanHe = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HopDong_NguoiThue", x => new { x.HopDongId, x.NguoiThueId });
                    table.ForeignKey(
                        name: "FK_HopDong_NguoiThue_HopDongThue_HopDongId",
                        column: x => x.HopDongId,
                        principalTable: "HopDongThue",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HopDong_NguoiThue_NguoiDung_NguoiThueId",
                        column: x => x.NguoiThueId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KiemKeCanHo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HopDongId = table.Column<int>(type: "integer", nullable: false),
                    Loai = table.Column<int>(type: "integer", nullable: false),
                    NgayKiemKe = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiKiemTraId = table.Column<string>(type: "text", nullable: true),
                    TinhTrangChung = table.Column<int>(type: "integer", nullable: false),
                    TomTat = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    GhiChuHuHong = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DanhSachAnh = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    KhauTruDatCoc = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    KhachThueDaKy = table.Column<bool>(type: "boolean", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false),
                    NgayXoa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXoa = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    PhienBan = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KiemKeCanHo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KiemKeCanHo_HopDongThue_HopDongId",
                        column: x => x.HopDongId,
                        principalTable: "HopDongThue",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KiemKeCanHo_NguoiDung_NguoiKiemTraId",
                        column: x => x.NguoiKiemTraId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "YeuCauBaoTri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SoYeuCau = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CanHoId = table.Column<int>(type: "integer", nullable: false),
                    HopDongId = table.Column<int>(type: "integer", nullable: true),
                    NguoiBaoCaoId = table.Column<string>(type: "text", nullable: true),
                    TieuDe = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    MoTa = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    LoaiBaoTri = table.Column<int>(type: "integer", nullable: false),
                    MucDoUuTien = table.Column<int>(type: "integer", nullable: false),
                    TrangThai = table.Column<int>(type: "integer", nullable: false),
                    NguoiPhuTrachId = table.Column<string>(type: "text", nullable: true),
                    GhiChuXuLy = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DanhSachAnh = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ChiPhiUocTinh = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    ChiPhiThucTe = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    TinhPhiKhachThue = table.Column<bool>(type: "boolean", nullable: false),
                    NgayBaoCao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NgayTiepNhan = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NgayGiaiQuyet = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NgayDongLai = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false),
                    NgayXoa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXoa = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    PhienBan = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YeuCauBaoTri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_YeuCauBaoTri_CanHo_CanHoId",
                        column: x => x.CanHoId,
                        principalTable: "CanHo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_YeuCauBaoTri_HopDongThue_HopDongId",
                        column: x => x.HopDongId,
                        principalTable: "HopDongThue",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_YeuCauBaoTri_NguoiDung_NguoiBaoCaoId",
                        column: x => x.NguoiBaoCaoId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_YeuCauBaoTri_NguoiDung_NguoiPhuTrachId",
                        column: x => x.NguoiPhuTrachId,
                        principalTable: "NguoiDung",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ChiTietHoaDon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HoaDonId = table.Column<int>(type: "integer", nullable: false),
                    MoTa = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SoLuong = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DonGia = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    ThanhTien = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    ThuTu = table.Column<int>(type: "integer", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false),
                    NgayXoa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXoa = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    PhienBan = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietHoaDon", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChiTietHoaDon_HoaDon_HoaDonId",
                        column: x => x.HoaDonId,
                        principalTable: "HoaDon",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ThanhToan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SoThanhToan = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    HoaDonId = table.Column<int>(type: "integer", nullable: false),
                    SoTien = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    DonViTienTe = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    PhuongThuc = table.Column<int>(type: "integer", nullable: false),
                    TrangThai = table.Column<int>(type: "integer", nullable: false),
                    MaGiaoDich = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    NhaCungCap = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    NgayThanhToan = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NgayHoanTien = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SoTienHoanTra = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    GhiChu = table.Column<string>(type: "text", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false),
                    NgayXoa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXoa = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    PhienBan = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThanhToan", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThanhToan_HoaDon_HoaDonId",
                        column: x => x.HoaDonId,
                        principalTable: "HoaDon",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GiaoDichDatCoc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HopDongId = table.Column<int>(type: "integer", nullable: false),
                    Loai = table.Column<int>(type: "integer", nullable: false),
                    SoTien = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    LyDo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    KiemKeLienQuanId = table.Column<int>(type: "integer", nullable: true),
                    YeuCauLienQuanId = table.Column<int>(type: "integer", nullable: true),
                    NgayGhiNhan = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiGhiNhan = table.Column<string>(type: "text", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    DaXoa = table.Column<bool>(type: "boolean", nullable: false),
                    NgayXoa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NguoiXoa = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    PhienBan = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiaoDichDatCoc", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GiaoDichDatCoc_HopDongThue_HopDongId",
                        column: x => x.HopDongId,
                        principalTable: "HopDongThue",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GiaoDichDatCoc_KiemKeCanHo_KiemKeLienQuanId",
                        column: x => x.KiemKeLienQuanId,
                        principalTable: "KiemKeCanHo",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GiaoDichDatCoc_YeuCauBaoTri_YeuCauLienQuanId",
                        column: x => x.YeuCauLienQuanId,
                        principalTable: "YeuCauBaoTri",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnhCanHo_CanHoId",
                table: "AnhCanHo",
                column: "CanHoId");

            migrationBuilder.CreateIndex(
                name: "IX_AnhDuAn_DuAnId",
                table: "AnhDuAn",
                column: "DuAnId");

            migrationBuilder.CreateIndex(
                name: "IX_CanHo_ChuNhaId",
                table: "CanHo",
                column: "ChuNhaId");

            migrationBuilder.CreateIndex(
                name: "IX_CanHo_DanhMucId",
                table: "CanHo",
                column: "DanhMucId");

            migrationBuilder.CreateIndex(
                name: "IX_CanHo_DuAnId",
                table: "CanHo",
                column: "DuAnId");

            migrationBuilder.CreateIndex(
                name: "IX_CanHo_Gia",
                table: "CanHo",
                column: "Gia");

            migrationBuilder.CreateIndex(
                name: "IX_CanHo_KhuVucId",
                table: "CanHo",
                column: "KhuVucId");

            migrationBuilder.CreateIndex(
                name: "IX_CanHo_NoiBat",
                table: "CanHo",
                column: "NoiBat");

            migrationBuilder.CreateIndex(
                name: "IX_CanHo_Slug",
                table: "CanHo",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CanHo_TangId",
                table: "CanHo",
                column: "TangId");

            migrationBuilder.CreateIndex(
                name: "IX_CanHo_TinhTrangThue",
                table: "CanHo",
                column: "TinhTrangThue");

            migrationBuilder.CreateIndex(
                name: "IX_CanHo_ToaNhaId",
                table: "CanHo",
                column: "ToaNhaId");

            migrationBuilder.CreateIndex(
                name: "IX_CanHo_ToaNhaId_MaCanHo",
                table: "CanHo",
                columns: new[] { "ToaNhaId", "MaCanHo" },
                unique: true,
                filter: "\"ToaNhaId\" IS NOT NULL AND \"MaCanHo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CanHo_TrangThai",
                table: "CanHo",
                column: "TrangThai");

            migrationBuilder.CreateIndex(
                name: "IX_CanHo_TienIch_TienIchId",
                table: "CanHo_TienIch",
                column: "TienIchId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiSoDichVu_HopDongId_LoaiDichVuId_ThangTinhPhi",
                table: "ChiSoDichVu",
                columns: new[] { "HopDongId", "LoaiDichVuId", "ThangTinhPhi" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChiSoDichVu_LoaiDichVuId",
                table: "ChiSoDichVu",
                column: "LoaiDichVuId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiSoDichVu_ThangTinhPhi",
                table: "ChiSoDichVu",
                column: "ThangTinhPhi");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietHoaDon_HoaDonId",
                table: "ChiTietHoaDon",
                column: "HoaDonId");

            migrationBuilder.CreateIndex(
                name: "IX_DanhGia_CanHoId",
                table: "DanhGia",
                column: "CanHoId");

            migrationBuilder.CreateIndex(
                name: "IX_DanhGia_NguoiDungId",
                table: "DanhGia",
                column: "NguoiDungId");

            migrationBuilder.CreateIndex(
                name: "IX_DanhGia_TrangThai",
                table: "DanhGia",
                column: "TrangThai");

            migrationBuilder.CreateIndex(
                name: "IX_DanhMuc_Slug",
                table: "DanhMuc",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DuAn_KhuVucId",
                table: "DuAn",
                column: "KhuVucId");

            migrationBuilder.CreateIndex(
                name: "IX_DuAn_Slug",
                table: "DuAn",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DuAn_TrangThai",
                table: "DuAn",
                column: "TrangThai");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichDatCoc_HopDongId",
                table: "GiaoDichDatCoc",
                column: "HopDongId");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichDatCoc_KiemKeLienQuanId",
                table: "GiaoDichDatCoc",
                column: "KiemKeLienQuanId");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichDatCoc_Loai",
                table: "GiaoDichDatCoc",
                column: "Loai");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichDatCoc_NgayGhiNhan",
                table: "GiaoDichDatCoc",
                column: "NgayGhiNhan");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichDatCoc_YeuCauLienQuanId",
                table: "GiaoDichDatCoc",
                column: "YeuCauLienQuanId");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDon_HopDongId",
                table: "HoaDon",
                column: "HopDongId");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDon_HopDongId_ThangTinhPhi_LoaiHoaDon",
                table: "HoaDon",
                columns: new[] { "HopDongId", "ThangTinhPhi", "LoaiHoaDon" });

            migrationBuilder.CreateIndex(
                name: "IX_HoaDon_NgayDenHan",
                table: "HoaDon",
                column: "NgayDenHan");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDon_SoHoaDon",
                table: "HoaDon",
                column: "SoHoaDon",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HoaDon_TrangThai",
                table: "HoaDon",
                column: "TrangThai");

            migrationBuilder.CreateIndex(
                name: "IX_HopDong_NguoiThue_NguoiThueId",
                table: "HopDong_NguoiThue",
                column: "NguoiThueId");

            migrationBuilder.CreateIndex(
                name: "IX_HopDongThue_CanHoId",
                table: "HopDongThue",
                column: "CanHoId");

            migrationBuilder.CreateIndex(
                name: "IX_HopDongThue_NgayBatDau_NgayKetThuc",
                table: "HopDongThue",
                columns: new[] { "NgayBatDau", "NgayKetThuc" });

            migrationBuilder.CreateIndex(
                name: "IX_HopDongThue_NguoiThueChinhId",
                table: "HopDongThue",
                column: "NguoiThueChinhId");

            migrationBuilder.CreateIndex(
                name: "IX_HopDongThue_SoHopDong",
                table: "HopDongThue",
                column: "SoHopDong",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HopDongThue_TrangThai",
                table: "HopDongThue",
                column: "TrangThai");

            migrationBuilder.CreateIndex(
                name: "IX_KhuVuc_Slug",
                table: "KhuVuc",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KiemKeCanHo_HopDongId",
                table: "KiemKeCanHo",
                column: "HopDongId");

            migrationBuilder.CreateIndex(
                name: "IX_KiemKeCanHo_HopDongId_Loai",
                table: "KiemKeCanHo",
                columns: new[] { "HopDongId", "Loai" });

            migrationBuilder.CreateIndex(
                name: "IX_KiemKeCanHo_NguoiKiemTraId",
                table: "KiemKeCanHo",
                column: "NguoiKiemTraId");

            migrationBuilder.CreateIndex(
                name: "IX_LichXemNha_CanHoId",
                table: "LichXemNha",
                column: "CanHoId");

            migrationBuilder.CreateIndex(
                name: "IX_LichXemNha_NgayHen",
                table: "LichXemNha",
                column: "NgayHen");

            migrationBuilder.CreateIndex(
                name: "IX_LichXemNha_NguoiDungId",
                table: "LichXemNha",
                column: "NguoiDungId");

            migrationBuilder.CreateIndex(
                name: "IX_LichXemNha_TrangThai",
                table: "LichXemNha",
                column: "TrangThai");

            migrationBuilder.CreateIndex(
                name: "IX_LoaiDichVu_Ma",
                table: "LoaiDichVu",
                column: "Ma",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "NguoiDung",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_NguoiDung_DaXoa",
                table: "NguoiDung",
                column: "DaXoa");

            migrationBuilder.CreateIndex(
                name: "IX_NguoiDung_SoDienThoai",
                table: "NguoiDung",
                column: "SoDienThoai");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "NguoiDung",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NguoiDung_Claim_NguoiDungId",
                table: "NguoiDung_Claim",
                column: "NguoiDungId");

            migrationBuilder.CreateIndex(
                name: "IX_NguoiDung_DangNhap_NguoiDungId",
                table: "NguoiDung_DangNhap",
                column: "NguoiDungId");

            migrationBuilder.CreateIndex(
                name: "IX_NguoiDung_VaiTro_VaiTroId",
                table: "NguoiDung_VaiTro",
                column: "VaiTroId");

            migrationBuilder.CreateIndex(
                name: "IX_NhatKyHeThong_NguoiDungId",
                table: "NhatKyHeThong",
                column: "NguoiDungId");

            migrationBuilder.CreateIndex(
                name: "IX_NhatKyHeThong_TenBang_KhoaBang",
                table: "NhatKyHeThong",
                columns: new[] { "TenBang", "KhoaBang" });

            migrationBuilder.CreateIndex(
                name: "IX_NhatKyHeThong_ThoiGian",
                table: "NhatKyHeThong",
                column: "ThoiGian");

            migrationBuilder.CreateIndex(
                name: "IX_Quyen_Ma",
                table: "Quyen",
                column: "Ma",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quyen_Module",
                table: "Quyen",
                column: "Module");

            migrationBuilder.CreateIndex(
                name: "IX_Tang_ToaNhaId_SoTang",
                table: "Tang",
                columns: new[] { "ToaNhaId", "SoTang" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThanhToan_HoaDonId",
                table: "ThanhToan",
                column: "HoaDonId");

            migrationBuilder.CreateIndex(
                name: "IX_ThanhToan_MaGiaoDich",
                table: "ThanhToan",
                column: "MaGiaoDich");

            migrationBuilder.CreateIndex(
                name: "IX_ThanhToan_SoThanhToan",
                table: "ThanhToan",
                column: "SoThanhToan",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThanhToan_TrangThai",
                table: "ThanhToan",
                column: "TrangThai");

            migrationBuilder.CreateIndex(
                name: "IX_TienIch_Slug",
                table: "TienIch",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToaNha_DuAnId",
                table: "ToaNha",
                column: "DuAnId");

            migrationBuilder.CreateIndex(
                name: "IX_ToaNha_KhuVucId",
                table: "ToaNha",
                column: "KhuVucId");

            migrationBuilder.CreateIndex(
                name: "IX_ToaNha_QuanLyId",
                table: "ToaNha",
                column: "QuanLyId");

            migrationBuilder.CreateIndex(
                name: "IX_ToaNha_Slug",
                table: "ToaNha",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToaNha_TrangThai",
                table: "ToaNha",
                column: "TrangThai");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "VaiTro",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaiTro_Claim_VaiTroId",
                table: "VaiTro_Claim",
                column: "VaiTroId");

            migrationBuilder.CreateIndex(
                name: "IX_VaiTro_Quyen_QuyenId",
                table: "VaiTro_Quyen",
                column: "QuyenId");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauBaoTri_CanHoId",
                table: "YeuCauBaoTri",
                column: "CanHoId");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauBaoTri_HopDongId",
                table: "YeuCauBaoTri",
                column: "HopDongId");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauBaoTri_MucDoUuTien",
                table: "YeuCauBaoTri",
                column: "MucDoUuTien");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauBaoTri_NguoiBaoCaoId",
                table: "YeuCauBaoTri",
                column: "NguoiBaoCaoId");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauBaoTri_NguoiPhuTrachId",
                table: "YeuCauBaoTri",
                column: "NguoiPhuTrachId");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauBaoTri_SoYeuCau",
                table: "YeuCauBaoTri",
                column: "SoYeuCau",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauBaoTri_TrangThai",
                table: "YeuCauBaoTri",
                column: "TrangThai");

            migrationBuilder.CreateIndex(
                name: "IX_YeuThich_CanHoId",
                table: "YeuThich",
                column: "CanHoId");

            migrationBuilder.CreateIndex(
                name: "IX_YeuThich_NguoiDungId_CanHoId",
                table: "YeuThich",
                columns: new[] { "NguoiDungId", "CanHoId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnhCanHo");

            migrationBuilder.DropTable(
                name: "AnhDuAn");

            migrationBuilder.DropTable(
                name: "CanHo_TienIch");

            migrationBuilder.DropTable(
                name: "ChiSoDichVu");

            migrationBuilder.DropTable(
                name: "ChiTietHoaDon");

            migrationBuilder.DropTable(
                name: "DanhGia");

            migrationBuilder.DropTable(
                name: "GiaoDichDatCoc");

            migrationBuilder.DropTable(
                name: "HopDong_NguoiThue");

            migrationBuilder.DropTable(
                name: "LichXemNha");

            migrationBuilder.DropTable(
                name: "NguoiDung_Claim");

            migrationBuilder.DropTable(
                name: "NguoiDung_DangNhap");

            migrationBuilder.DropTable(
                name: "NguoiDung_Token");

            migrationBuilder.DropTable(
                name: "NguoiDung_VaiTro");

            migrationBuilder.DropTable(
                name: "NhatKyHeThong");

            migrationBuilder.DropTable(
                name: "ThanhToan");

            migrationBuilder.DropTable(
                name: "VaiTro_Claim");

            migrationBuilder.DropTable(
                name: "VaiTro_Quyen");

            migrationBuilder.DropTable(
                name: "YeuThich");

            migrationBuilder.DropTable(
                name: "TienIch");

            migrationBuilder.DropTable(
                name: "LoaiDichVu");

            migrationBuilder.DropTable(
                name: "KiemKeCanHo");

            migrationBuilder.DropTable(
                name: "YeuCauBaoTri");

            migrationBuilder.DropTable(
                name: "HoaDon");

            migrationBuilder.DropTable(
                name: "Quyen");

            migrationBuilder.DropTable(
                name: "VaiTro");

            migrationBuilder.DropTable(
                name: "HopDongThue");

            migrationBuilder.DropTable(
                name: "CanHo");

            migrationBuilder.DropTable(
                name: "DanhMuc");

            migrationBuilder.DropTable(
                name: "Tang");

            migrationBuilder.DropTable(
                name: "ToaNha");

            migrationBuilder.DropTable(
                name: "DuAn");

            migrationBuilder.DropTable(
                name: "NguoiDung");

            migrationBuilder.DropTable(
                name: "KhuVuc");
        }
    }
}
