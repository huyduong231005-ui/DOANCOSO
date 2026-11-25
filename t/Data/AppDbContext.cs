using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using t.Infrastructure.Audit;
using t.Models.Entities;
using t.Models.Entities.Common;

namespace t.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    private readonly ICurrentUserService? _currentUser;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService? currentUser = null)
        : base(options)
    {
        _currentUser = currentUser;
    }

    public DbSet<Apartment> Apartments => Set<Apartment>();
    public DbSet<ApartmentResident> ApartmentResidents => Set<ApartmentResident>();
    public DbSet<Project> Projects => Set<Project>();

    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<Floor> Floors => Set<Floor>();
    public DbSet<UtilityType> UtilityTypes => Set<UtilityType>();
    public DbSet<UtilityReading> UtilityReadings => Set<UtilityReading>();

    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<RecurringCharge> RecurringCharges => Set<RecurringCharge>();

    public DbSet<MaintenanceRequest> MaintenanceRequests => Set<MaintenanceRequest>();
    public DbSet<MaintenancePhoto> MaintenancePhotos => Set<MaintenancePhoto>();
    public DbSet<MaintenanceStatusChange> MaintenanceStatusChanges => Set<MaintenanceStatusChange>();
    public DbSet<SlaPolicy> SlaPolicies => Set<SlaPolicy>();

    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // ================== Identity ==================
        b.Entity<AppUser>(e =>
        {
            e.ToTable("NguoiDung");
            e.Property(x => x.Id).HasColumnName("NguoiDungId");
            e.HasIndex(u => u.IsDeleted);
            // Một người một tài khoản: email (nếu có) không trùng.
            e.HasIndex(u => u.NormalizedEmail).HasDatabaseName("EmailIndex").IsUnique().HasFilter("\"NormalizedEmail\" IS NOT NULL");
            e.HasIndex(u => u.PhoneNumber);

            // Custom columns
            e.Property(x => x.FullName).HasColumnName("HoTen");
            e.Property(x => x.IdNumber).HasColumnName("SoGiayTo").HasMaxLength(30);
            e.Property(x => x.UserName).HasColumnName("TenDangNhap");
            e.Property(x => x.PasswordHash).HasColumnName("MatKhauBam");
            e.Ignore(x => x.IsActivated);
            // Một cột số điện thoại duy nhất: dùng PhoneNumber của Identity, đặt tên SoDienThoai.
            e.Property(x => x.PhoneNumber).HasColumnName("SoDienThoai");

            // Audit / soft-delete columns (AppUser triển khai IAuditable + ISoftDeletable)
            e.Property(x => x.CreatedAt).HasColumnName("NgayTao");
            e.Property(x => x.CreatedBy).HasColumnName("NguoiTao");
            e.Property(x => x.UpdatedAt).HasColumnName("NgayCapNhat");
            e.Property(x => x.UpdatedBy).HasColumnName("NguoiCapNhat");
            e.Property(x => x.IsDeleted).HasColumnName("DaXoa");
            e.Property(x => x.DeletedAt).HasColumnName("NgayXoa");
            e.Property(x => x.DeletedBy).HasColumnName("NguoiXoa");
        });

        b.Entity<IdentityRole>(e =>
        {
            e.ToTable("VaiTro");
            e.Property(x => x.Id).HasColumnName("VaiTroId");
            e.Property(x => x.Name).HasColumnName("TenVaiTro");
        });

        b.Entity<IdentityUserRole<string>>(e =>
        {
            e.ToTable("NguoiDung_VaiTro");
            e.Property(x => x.UserId).HasColumnName("NguoiDungId");
            e.Property(x => x.RoleId).HasColumnName("VaiTroId");
        });

        b.Entity<IdentityUserClaim<string>>(e =>
        {
            e.ToTable("NguoiDung_Claim");
            e.Property(x => x.UserId).HasColumnName("NguoiDungId");
        });

        b.Entity<IdentityUserLogin<string>>(e =>
        {
            e.ToTable("NguoiDung_DangNhap");
            e.Property(x => x.UserId).HasColumnName("NguoiDungId");
        });

        b.Entity<IdentityRoleClaim<string>>(e =>
        {
            e.ToTable("VaiTro_Claim");
            e.Property(x => x.RoleId).HasColumnName("VaiTroId");
        });

        b.Entity<IdentityUserToken<string>>(e =>
        {
            e.ToTable("NguoiDung_Token");
            e.Property(x => x.UserId).HasColumnName("NguoiDungId");
        });

        // ================== Apartment ==================
        b.Entity<Apartment>(e =>
        {
            e.ToTable("CanHo", t =>
            {
                t.HasCheckConstraint("CK_CanHo_DienTich", "\"DienTich\" > 0");
                t.HasCheckConstraint("CK_CanHo_SoPhongNgu", "\"SoPhongNgu\" >= 0");
                t.HasCheckConstraint("CK_CanHo_TinhTrangCanHo", "\"TinhTrangCanHo\" IN (0, 1, 3)");
            });
            MapBaseEntityColumns(e, "CanHoId");

            e.Property(a => a.UnitCode).HasColumnName("MaCanHo").HasMaxLength(40).IsRequired();
            e.Property(a => a.Area).HasColumnName("DienTich");
            e.Property(a => a.Bedrooms).HasColumnName("SoPhongNgu");
            e.Property(a => a.Condition).HasColumnName("TinhTrangCanHo");
            e.Property(a => a.FloorId).HasColumnName("TangId");

            e.HasIndex(a => a.Condition);
            e.HasIndex(a => a.FloorId);
            // Mã căn bắt đầu bằng mã toà (duy nhất) nên duy nhất toàn hệ thống.
            e.HasIndex(a => a.UnitCode).IsUnique().HasFilter("\"DaXoa\" = false");

            // Toà của căn suy ra qua tầng; căn không lưu ToaNhaId. Tầng và toà luôn được nạp kèm căn.
            e.Navigation(a => a.Floor).AutoInclude();
            e.HasOne(a => a.Floor)
             .WithMany(f => f.Apartments)
             .HasForeignKey(a => a.FloorId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ================== ApartmentResident (bảng CuTru: người dùng cư trú tại căn) ==================
        b.Entity<ApartmentResident>(e =>
        {
            e.ToTable("CuTru", t =>
            {
                t.HasCheckConstraint("CK_CuTru_NgayChuyenDi", "\"NgayChuyenDi\" IS NULL OR \"NgayChuyenDi\" >= \"NgayVaoO\"");
                t.HasCheckConstraint("CK_CuTru_VaiTroTrongHo", "\"VaiTroTrongHo\" BETWEEN 0 AND 2");
            });
            MapBaseEntityColumns(e, "CuTruId");

            e.Property(x => x.ApartmentId).HasColumnName("CanHoId");
            e.Property(x => x.UserId).HasColumnName("NguoiDungId").IsRequired();
            e.Property(x => x.Role).HasColumnName("VaiTroTrongHo");
            e.Property(x => x.IsPrimary).HasColumnName("LaDaiDien");
            e.Property(x => x.MoveInDate).HasColumnName("NgayVaoO");
            e.Property(x => x.MoveOutDate).HasColumnName("NgayChuyenDi");
            e.Property(x => x.ConfirmedAt).HasColumnName("NgayXacNhan");
            e.Ignore(x => x.IsActive);
            // Họ tên, liên hệ của người cư trú nằm ở NguoiDung: luôn nạp kèm.
            e.Navigation(x => x.User).AutoInclude();

            e.HasIndex(x => x.ApartmentId);
            e.HasIndex(x => x.UserId);
            // Một người không cư trú cùng một căn hai lần cùng lúc; mỗi căn một người đại diện đang ở.
            e.HasIndex(x => new { x.ApartmentId, x.UserId }).IsUnique()
             .HasDatabaseName("UX_CuTru_DangO").HasFilter("\"NgayChuyenDi\" IS NULL AND \"DaXoa\" = false");
            e.HasIndex(x => x.ApartmentId).IsUnique()
             .HasDatabaseName("UX_CuTru_DaiDien").HasFilter("\"LaDaiDien\" AND \"NgayChuyenDi\" IS NULL AND \"DaXoa\" = false");

            // Liên kết thường, không hợp thành: căn còn người cư trú thì không xoá được, xoá căn không xoá người.
            e.HasOne(x => x.Apartment).WithMany(a => a.Residents)
             .HasForeignKey(x => x.ApartmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.User).WithMany(u => u.Residencies)
             .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        // ================== Project ==================
        b.Entity<Project>(e =>
        {
            e.ToTable("Khu");
            MapBaseEntityColumns(e, "KhuId");

            e.Property(p => p.Name).HasColumnName("TenKhu").HasMaxLength(200).IsRequired();
            e.Property(p => p.Address).HasColumnName("DiaChi");


        });

        // ================== Building ==================
        b.Entity<Building>(e =>
        {
            e.ToTable("ToaNha", t =>
            {
                t.HasCheckConstraint("CK_ToaNha_NgayLapPhi", "\"NgayLapPhi\" BETWEEN 1 AND 28");
                t.HasCheckConstraint("CK_ToaNha_PhiTre", "\"SoNgayAnHan\" >= 0 AND \"TiLePhiTre\" BETWEEN 0 AND 100");
                t.HasCheckConstraint("CK_ToaNha_TrangThai", "\"TrangThaiToaNha\" BETWEEN 0 AND 2");
            });
            MapBaseEntityColumns(e, "ToaNhaId");

            e.Property(x => x.Code).HasColumnName("MaToaNha").HasMaxLength(40);
            e.Property(x => x.ProjectId).HasColumnName("KhuId");
            e.Property(x => x.ManagerId).HasColumnName("QuanLyId");
            e.Property(x => x.Status).HasColumnName("TrangThaiToaNha");
            e.Property(x => x.DefaultBillingDay).HasColumnName("NgayLapPhi").HasDefaultValue(1);
            e.Property(x => x.DefaultLateFeeAfterDays).HasColumnName("SoNgayAnHan").HasDefaultValue(7);
            e.Property(x => x.DefaultLateFeePercent).HasColumnName("TiLePhiTre").HasDefaultValue(5);

            e.HasIndex(x => x.Code).IsUnique().HasFilter("\"DaXoa\" = false");
            e.HasIndex(x => x.ProjectId);
            e.HasIndex(x => x.Status);

            e.HasOne(x => x.Project).WithMany(p => p.Buildings).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Manager).WithMany(u => u.ManagedBuildings).HasForeignKey(x => x.ManagerId).OnDelete(DeleteBehavior.SetNull);
        });

        // ================== Floor ==================
        b.Entity<Floor>(e =>
        {
            e.ToTable("Tang");
            MapBaseEntityColumns(e, "TangId");

            e.Property(x => x.BuildingId).HasColumnName("ToaNhaId");
            e.Property(x => x.Number).HasColumnName("SoTang");
            e.Ignore(x => x.Label);   // nhãn "Tầng n" suy ra từ SoTang

            e.HasIndex(x => new { x.BuildingId, x.Number }).IsUnique();

            e.Navigation(x => x.Building).AutoInclude();
            e.HasOne(x => x.Building).WithMany(b2 => b2.Floors)
             .HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.Restrict);
        });

        // ================== MaintenanceRequest ==================
        b.Entity<MaintenanceRequest>(e =>
        {
            e.Property(x => x.Xmin).IsRowVersion(); // Npgsql: uint + IsRowVersion → cột hệ thống xmin
            e.ToTable("YeuCauBaoTri", t =>
            {
                // BR: sự cố ở đúng một nơi, trong căn hoặc khu vực chung (có toà và mô tả vị trí).
                t.HasCheckConstraint("CK_YeuCauBaoTri_NoiXayRa",
                    "(\"CanHoId\" IS NOT NULL AND \"ToaNhaId\" IS NULL) OR " +
                    "(\"CanHoId\" IS NULL AND \"ToaNhaId\" IS NOT NULL AND \"ViTri\" IS NOT NULL)");
                t.HasCheckConstraint("CK_YeuCauBaoTri_ChiPhi", "\"ChiPhiSuaChua\" >= 0");
                t.HasCheckConstraint("CK_YeuCauBaoTri_DanhGia", "\"DiemDanhGia\" IS NULL OR \"DiemDanhGia\" BETWEEN 1 AND 5");
                t.HasCheckConstraint("CK_YeuCauBaoTri_HanXuLy", "\"HanXuLy\" IS NULL OR \"HanXuLy\" >= \"NgayBao\"");
                // Miền giá trị của enum MaintenanceStatus / MaintenanceCategory (thêm giá trị mới thì sửa cả hai nơi).
                t.HasCheckConstraint("CK_YeuCauBaoTri_TrangThai", "\"TrangThaiYeuCau\" BETWEEN 0 AND 7");
                t.HasCheckConstraint("CK_YeuCauBaoTri_LoaiSuCo", "\"LoaiSuCo\" IN (0, 1, 2, 3, 4, 5, 6, 99)");
                // Đã xử lý (3) hoặc đã đóng (4) phải có kết quả xử lý.
                t.HasCheckConstraint("CK_YeuCauBaoTri_KetQua", "\"TrangThaiYeuCau\" NOT IN (3, 4) OR coalesce(btrim(\"KetQuaXuLy\"), '') <> ''");
            });
            MapBaseEntityColumns(e, "YeuCauBaoTriId");

            e.Property(x => x.RequestNumber).HasColumnName("SoYeuCau").HasMaxLength(40).IsRequired();
            e.Property(x => x.ApartmentId).HasColumnName("CanHoId");
            e.Property(x => x.CommonBuildingId).HasColumnName("ToaNhaId");
            e.Property(x => x.Location).HasColumnName("ViTri").HasMaxLength(200);
            e.Property(x => x.ReporterId).HasColumnName("NguoiBaoId").IsRequired(); // mỗi yêu cầu có đúng một người báo
            e.Property(x => x.Title).HasColumnName("TieuDe").HasMaxLength(250).IsRequired();
            e.Property(x => x.Description).HasColumnName("MoTa").HasMaxLength(4000);
            e.Property(x => x.Category).HasColumnName("LoaiSuCo");
            e.Property(x => x.Priority).HasColumnName("MucUuTienId");
            e.Property(x => x.Status).HasColumnName("TrangThaiYeuCau");
            e.Property(x => x.AssignedToId).HasColumnName("NguoiPhuTrachId");
            e.Property(x => x.ResolutionNote).HasColumnName("KetQuaXuLy").HasMaxLength(2000);
            e.Property(x => x.AiTriageJson).HasColumnName("PhanLoaiAi");
            e.Property(x => x.ActualCost).HasColumnName("ChiPhiSuaChua").HasColumnType("decimal(18,0)");
            e.Property(x => x.ChargeToTenant).HasColumnName("TinhPhiCuDan");
            e.Property(x => x.ReportedAt).HasColumnName("NgayBao");
            e.Ignore(x => x.AcknowledgedAt);   // mốc thời gian suy ra từ LichSuYeuCau
            e.Ignore(x => x.ResolvedAt);
            e.Ignore(x => x.ClosedAt);
            e.Property(x => x.DueAt).HasColumnName("HanXuLy");
            e.Property(x => x.Rating).HasColumnName("DiemDanhGia");
            e.Property(x => x.RatingComment).HasColumnName("NhanXet").HasMaxLength(1000);

            e.HasIndex(x => x.RequestNumber).IsUnique();
            e.HasIndex(x => x.ApartmentId);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.Priority);
            e.HasIndex(x => x.AssignedToId);
            e.HasIndex(x => x.CommonBuildingId);
            e.HasIndex(x => x.DueAt);
            e.HasIndex(x => x.ReportedAt);   // mọi báo cáo KPI lọc theo kỳ báo

            e.HasOne(x => x.Apartment).WithMany(a => a.MaintenanceRequests)
             .HasForeignKey(x => x.ApartmentId).OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.CommonBuilding).WithMany()
             .HasForeignKey(x => x.CommonBuildingId).OnDelete(DeleteBehavior.Restrict);

            // Mức ưu tiên phải là một mức có trong bảng MucUuTien (không xoá được mức đang được dùng).
            e.HasOne<SlaPolicy>().WithMany()
             .HasForeignKey(x => x.Priority).OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Reporter).WithMany()
             .HasForeignKey(x => x.ReporterId).OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.AssignedTo).WithMany()
             .HasForeignKey(x => x.AssignedToId).OnDelete(DeleteBehavior.SetNull);
        });

        // ================== MaintenancePhoto / StatusChange / SlaPolicy ==================
        b.Entity<MaintenancePhoto>(e =>
        {
            e.ToTable("AnhYeuCau", t => t.HasCheckConstraint("CK_AnhYeuCau_GiaiDoan", "\"GiaiDoan\" IN (0, 1)"));
            e.HasQueryFilter(x => !x.MaintenanceRequest.IsDeleted); // khớp bộ lọc xoá mềm của yêu cầu cha
            e.Property(x => x.Id).HasColumnName("AnhYeuCauId");
            e.Property(x => x.MaintenanceRequestId).HasColumnName("YeuCauBaoTriId");
            e.Property(x => x.Url).HasColumnName("DuongDan").HasMaxLength(500).IsRequired();
            e.Property(x => x.Stage).HasColumnName("GiaiDoan");
            e.Property(x => x.UploadedById).HasColumnName("NguoiTaiId");
            e.Property(x => x.UploadedAt).HasColumnName("NgayTai");
            e.HasIndex(x => x.MaintenanceRequestId);
            e.HasOne(x => x.MaintenanceRequest).WithMany(r => r.Photos)
             .HasForeignKey(x => x.MaintenanceRequestId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UploadedById).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<MaintenanceStatusChange>(e =>
        {
            e.ToTable("LichSuYeuCau");
            e.HasQueryFilter(x => !x.MaintenanceRequest.IsDeleted);
            e.Property(x => x.Id).HasColumnName("LichSuYeuCauId");
            e.Property(x => x.MaintenanceRequestId).HasColumnName("YeuCauBaoTriId");
            e.Property(x => x.ToStatus).HasColumnName("TrangThaiMoi");
            e.Property(x => x.ChangedById).HasColumnName("NguoiThucHienId");
            e.Property(x => x.ChangedAt).HasColumnName("ThoiDiem");
            e.Property(x => x.Note).HasColumnName("GhiChu").HasMaxLength(1000);
            e.HasIndex(x => new { x.MaintenanceRequestId, x.ChangedAt });
            e.HasOne(x => x.MaintenanceRequest).WithMany(r => r.History)
             .HasForeignKey(x => x.MaintenanceRequestId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ChangedBy).WithMany().HasForeignKey(x => x.ChangedById).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<SlaPolicy>(e =>
        {
            e.ToTable("MucUuTien", t =>
            {
                t.HasCheckConstraint("CK_MucUuTien_ThoiHan", "\"PhutPhanHoi\" > 0 AND \"PhutHoanTat\" >= \"PhutPhanHoi\"");
                t.HasCheckConstraint("CK_MucUuTien_Id", "\"MucUuTienId\" BETWEEN 0 AND 3");
            });
            e.HasKey(x => x.Priority);
            e.Property(x => x.Priority).HasColumnName("MucUuTienId").ValueGeneratedNever();
            e.Property(x => x.Name).HasColumnName("TenMucUuTien").HasMaxLength(50).IsRequired();
            e.Property(x => x.ResponseMinutes).HasColumnName("PhutPhanHoi");
            e.Property(x => x.ResolveMinutes).HasColumnName("PhutHoanTat");
            e.HasIndex(x => x.Name).IsUnique();
            e.HasData(SlaPolicy.Defaults);
        });

        // ================== UtilityType ==================
        b.Entity<UtilityType>(e =>
        {
            e.ToTable("LoaiDichVu", t =>
            {
                t.HasCheckConstraint("CK_LoaiDichVu_DonGia", "\"DonGia\" >= 0");
                t.HasCheckConstraint("CK_LoaiDichVu_CachTinh", "\"CachTinh\" BETWEEN 0 AND 3");
            });
            MapBaseEntityColumns(e, "LoaiDichVuId");

            e.Property(x => x.Code).HasColumnName("MaLoaiDichVu").HasMaxLength(40).IsRequired();
            e.Property(x => x.Name).HasColumnName("TenLoaiDichVu").HasMaxLength(120).IsRequired();
            e.Property(x => x.Unit).HasColumnName("DonViTinh").HasMaxLength(20);
            e.Property(x => x.DefaultRate).HasColumnName("DonGia").HasColumnType("decimal(18,2)");
            e.Property(x => x.Method).HasColumnName("CachTinh");

            e.HasIndex(x => x.Code).IsUnique();
        });

        // ================== UtilityReading ==================
        b.Entity<UtilityReading>(e =>
        {
            e.ToTable("ChiSoDichVu", t =>
            {
                t.HasCheckConstraint("CK_ChiSoDichVu_ChiSo", "\"ChiSoCu\" >= 0 AND \"ChiSoMoi\" >= \"ChiSoCu\"");
                t.HasCheckConstraint("CK_ChiSoDichVu_Ky", "\"Ky\" % 100 BETWEEN 1 AND 12");
            });
            MapBaseEntityColumns(e, "ChiSoDichVuId");

            e.Property(x => x.ApartmentId).HasColumnName("CanHoId");
            e.Property(x => x.UtilityTypeId).HasColumnName("LoaiDichVuId");
            e.Property(x => x.BillingMonth).HasColumnName("Ky");
            e.Property(x => x.PreviousReading).HasColumnName("ChiSoCu").HasColumnType("decimal(18,2)");
            e.Property(x => x.CurrentReading).HasColumnName("ChiSoMoi").HasColumnType("decimal(18,2)");
            e.Ignore(x => x.Consumption);   // = ChiSoMoi − ChiSoCu
            e.Property(x => x.ReadAt).HasColumnName("NgayDoc");
            e.Ignore(x => x.Billed);

            e.HasIndex(x => new { x.ApartmentId, x.UtilityTypeId, x.BillingMonth }).IsUnique().HasFilter("\"DaXoa\" = false");
            e.HasIndex(x => x.BillingMonth);

            // Chỉ số là dữ liệu của căn nhưng không phải bộ phận của căn: căn còn chỉ số thì không xoá được.
            e.HasOne(x => x.Apartment).WithMany(a => a.UtilityReadings)
             .HasForeignKey(x => x.ApartmentId).OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.UtilityType).WithMany(t => t.Readings)
             .HasForeignKey(x => x.UtilityTypeId).OnDelete(DeleteBehavior.Restrict);
        });

        // ================== Invoice ==================
        b.Entity<Invoice>(e =>
        {
            e.ToTable("HoaDon", t =>
            {
                t.HasCheckConstraint("CK_HoaDon_SoTien", "\"TienPhi\" >= 0 AND \"PhiTreHan\" >= 0 AND \"DaThu\" >= 0");
                t.HasCheckConstraint("CK_HoaDon_DaThu", "\"DaThu\" <= \"TienPhi\" + \"PhiTreHan\"");
                t.HasCheckConstraint("CK_HoaDon_HanThanhToan", "\"HanThanhToan\" >= \"NgayLap\"");
                t.HasCheckConstraint("CK_HoaDon_Ky", "\"Ky\" % 100 BETWEEN 1 AND 12");
                t.HasCheckConstraint("CK_HoaDon_TrangThai", "\"TrangThaiHoaDon\" BETWEEN 0 AND 6");
            });
            MapBaseEntityColumns(e, "HoaDonId");

            e.Property(x => x.InvoiceNumber).HasColumnName("SoHoaDon").HasMaxLength(40).IsRequired();
            e.Property(x => x.ApartmentId).HasColumnName("CanHoId");
            e.Property(x => x.Kind).HasColumnName("LoaiHoaDon");
            e.Property(x => x.BillingMonth).HasColumnName("Ky");
            e.Property(x => x.IssueDate).HasColumnName("NgayLap");
            e.Property(x => x.DueDate).HasColumnName("HanThanhToan");
            e.Property(x => x.SubTotal).HasColumnName("TienPhi").HasColumnType("decimal(18,0)");
            e.Property(x => x.LateFee).HasColumnName("PhiTreHan").HasColumnType("decimal(18,0)");
            // Tổng tiền và còn nợ do CSDL tự tính, không ghi tay nên không lệch với các khoản.
            e.Property(x => x.Total).HasColumnName("TongTien").HasColumnType("decimal(18,0)")
             .HasComputedColumnSql("\"TienPhi\" + \"PhiTreHan\"", stored: true);
            e.Property(x => x.AmountPaid).HasColumnName("DaThu").HasColumnType("decimal(18,0)");
            e.Property(x => x.Balance).HasColumnName("ConNo").HasColumnType("decimal(18,0)")
             .HasComputedColumnSql("\"TienPhi\" + \"PhiTreHan\" - \"DaThu\"", stored: true);
            e.Property(x => x.Status).HasColumnName("TrangThaiHoaDon");
            e.Property(x => x.Note).HasColumnName("GhiChu");

            e.HasIndex(x => x.InvoiceNumber).IsUnique();
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.DueDate);
            e.HasIndex(x => new { x.ApartmentId, x.BillingMonth, x.Kind });
            // BR: mỗi căn chỉ một hoá đơn phí tháng còn hiệu lực cho mỗi kỳ (huỷ thì được lập lại).
            e.HasIndex(x => new { x.ApartmentId, x.BillingMonth })
             .HasDatabaseName("UX_HoaDon_PhiThang_MoiKy").IsUnique()
             .HasFilter("\"LoaiHoaDon\" = 0 AND \"TrangThaiHoaDon\" <> 5 AND \"DaXoa\" = false");

            e.HasOne(x => x.Apartment)
             .WithMany(a => a.Invoices)
             .HasForeignKey(x => x.ApartmentId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ================== InvoiceItem ==================
        // Bảng chi tiết chuẩn: mỗi dòng là một loại dịch vụ trên một hoá đơn.
        b.Entity<InvoiceItem>(e =>
        {
            e.ToTable("ChiTietHoaDon", t =>
                t.HasCheckConstraint("CK_ChiTietHoaDon_SoTien", "\"SoLuong\" > 0 AND \"DonGia\" >= 0"));
            e.HasKey(x => new { x.InvoiceId, x.UtilityTypeId });

            e.Property(x => x.InvoiceId).HasColumnName("HoaDonId");
            e.Property(x => x.UtilityTypeId).HasColumnName("LoaiDichVuId");
            e.Property(x => x.Quantity).HasColumnName("SoLuong").HasColumnType("decimal(18,2)");
            e.Property(x => x.UnitPrice).HasColumnName("DonGia").HasColumnType("decimal(18,0)");
            e.Property(x => x.LineTotal).HasColumnName("ThanhTien").HasColumnType("decimal(18,0)")
             .HasComputedColumnSql("round(\"SoLuong\" * \"DonGia\")", stored: true);
            e.Ignore(x => x.Description);

            e.HasOne(x => x.Invoice)
             .WithMany(i => i.Items)
             .HasForeignKey(x => x.InvoiceId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.UtilityType).WithMany()
             .HasForeignKey(x => x.UtilityTypeId).OnDelete(DeleteBehavior.Restrict);
            e.Navigation(x => x.UtilityType).AutoInclude();
            e.HasQueryFilter(x => !x.Invoice.IsDeleted); // khớp bộ lọc xoá mềm của hoá đơn cha
        });

        // ================== RecurringCharge ==================
        // Đăng ký dịch vụ cố định của căn: khoá chính CanHoId + LoaiDichVuId, chỉ lưu số lượng.
        b.Entity<RecurringCharge>(e =>
        {
            e.ToTable("DangKyDichVu", t => t.HasCheckConstraint("CK_DangKyDichVu_SoLuong", "\"SoLuong\" > 0"));
            e.HasKey(x => new { x.ApartmentId, x.UtilityTypeId });

            e.Property(x => x.ApartmentId).HasColumnName("CanHoId");
            e.Property(x => x.UtilityTypeId).HasColumnName("LoaiDichVuId");
            e.Property(x => x.Quantity).HasColumnName("SoLuong");

            e.HasOne(x => x.Apartment)
             .WithMany(a => a.RecurringCharges)
             .HasForeignKey(x => x.ApartmentId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.UtilityType).WithMany()
             .HasForeignKey(x => x.UtilityTypeId).OnDelete(DeleteBehavior.Restrict);
            e.Navigation(x => x.UtilityType).AutoInclude();
            e.HasQueryFilter(x => !x.Apartment.IsDeleted);
        });

        // ================== Payment ==================
        b.Entity<Payment>(e =>
        {
            e.ToTable("ThanhToan", t =>
            {
                t.HasCheckConstraint("CK_ThanhToan_SoTien", "\"SoTien\" > 0");
                t.HasCheckConstraint("CK_ThanhToan_HoanTra", "\"SoTienHoanTra\" >= 0 AND \"SoTienHoanTra\" <= \"SoTien\"");
                t.HasCheckConstraint("CK_ThanhToan_PhuongThuc", "\"PhuongThuc\" BETWEEN 0 AND 4");
                t.HasCheckConstraint("CK_ThanhToan_TrangThai", "\"TrangThaiThanhToan\" BETWEEN 0 AND 5");
            });
            MapBaseEntityColumns(e, "ThanhToanId");

            e.Property(x => x.PaymentNumber).HasColumnName("SoThanhToan").HasMaxLength(40).IsRequired();
            e.Property(x => x.InvoiceId).HasColumnName("HoaDonId");
            e.Property(x => x.Amount).HasColumnName("SoTien").HasColumnType("decimal(18,0)");
            e.Property(x => x.Method).HasColumnName("PhuongThuc");
            e.Property(x => x.Status).HasColumnName("TrangThaiThanhToan");
            e.Property(x => x.TransactionRef).HasColumnName("MaGiaoDich").HasMaxLength(120);
            e.Property(x => x.PaidAt).HasColumnName("NgayThanhToan");
            e.Property(x => x.RefundedAt).HasColumnName("NgayHoanTien");
            e.Property(x => x.RefundedAmount).HasColumnName("SoTienHoanTra").HasColumnType("decimal(18,0)");
            e.Property(x => x.Note).HasColumnName("GhiChu");

            e.HasIndex(x => x.PaymentNumber).IsUnique();
            e.HasIndex(x => x.InvoiceId);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.TransactionRef);

            e.HasOne(x => x.Invoice)
             .WithMany(i => i.Payments)
             .HasForeignKey(x => x.InvoiceId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ================== Permission ==================
        b.Entity<Permission>(e =>
        {
            e.ToTable("Quyen");
            MapBaseEntityColumns(e, "QuyenId");

            e.Property(x => x.Code).HasColumnName("MaQuyen").HasMaxLength(100).IsRequired();
            e.Property(x => x.Module).HasColumnName("PhanHe").HasMaxLength(60).IsRequired();
            e.Property(x => x.DisplayName).HasColumnName("TenQuyen").HasMaxLength(200).IsRequired();

            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.Module);
        });

        // ================== RolePermission (junction) ==================
        b.Entity<RolePermission>(e =>
        {
            e.ToTable("VaiTro_Quyen");
            e.HasKey(x => new { x.RoleId, x.PermissionId });

            e.Property(x => x.RoleId).HasColumnName("VaiTroId");
            e.Property(x => x.PermissionId).HasColumnName("QuyenId");
            e.Property(x => x.CreatedAt).HasColumnName("NgayTao");
            e.Property(x => x.CreatedBy).HasColumnName("NguoiTao");
            e.Property(x => x.UpdatedAt).HasColumnName("NgayCapNhat");
            e.Property(x => x.UpdatedBy).HasColumnName("NguoiCapNhat");

            e.HasIndex(x => x.PermissionId);

            e.HasOne(x => x.Role)
             .WithMany()
             .HasForeignKey(x => x.RoleId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Permission)
             .WithMany(p => p.RolePermissions)
             .HasForeignKey(x => x.PermissionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ================== AuditLog ==================
        b.Entity<AuditLog>(e =>
        {
            e.ToTable("NhatKyHeThong");

            e.Property(x => x.EntityName).HasColumnName("TenBang").HasMaxLength(120).IsRequired();
            e.Property(x => x.EntityKey).HasColumnName("KhoaDong").HasMaxLength(120);
            e.Property(x => x.Action).HasColumnName("LoaiThaoTac");
            e.Property(x => x.Id).HasColumnName("NhatKyHeThongId");
            e.Property(x => x.UserId).HasColumnName("NguoiThaoTacId").HasMaxLength(36);
            e.Property(x => x.Timestamp).HasColumnName("ThoiDiem");
            e.Property(x => x.OldValues).HasColumnName("GiaTriCu");
            e.Property(x => x.NewValues).HasColumnName("GiaTriMoi");
            e.Property(x => x.IpAddress).HasColumnName("DiaChiIp").HasMaxLength(64);

            e.HasIndex(x => new { x.EntityName, x.EntityKey });
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.Timestamp);
        });

        // Soft delete global query filter
        ApplySoftDeleteFilter(b);
        ApplyRelationshipSoftDeleteFilters(b);

        // Audit columns max length
        ApplyAuditColumnConstraints(b);
        ApplyStandardTypes(b);
    }

    /// <summary>
    /// Kiểu dữ liệu thống nhất theo loại thuộc tính (một nơi duy nhất):
    /// Id người dùng, vai trò là GUID 36 ký tự; tiền là numeric(18,0) đồng; diện tích numeric(8,2);
    /// tên, tiêu đề varchar(200); mã varchar(40); mô tả varchar(2000); ghi chú varchar(1000);
    /// đường dẫn ảnh, địa chỉ varchar(500); họ tên varchar(100); điện thoại varchar(20);
    /// cột "Ngày..." chỉ mang ngày là date. Cột JSON (kết quả AI, giá trị nhật ký) giữ text để một chuỗi hỏng
    /// không làm hỏng thao tác lưu.
    /// </summary>
    private static void ApplyStandardTypes(ModelBuilder b)
    {
        const int Guid = 36;
        b.Entity<AppUser>().Property(x => x.Id).HasMaxLength(Guid);
        b.Entity<IdentityRole>().Property(x => x.Id).HasMaxLength(Guid);
        foreach (var fk in b.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys())
                     .Where(fk => fk.PrincipalEntityType.ClrType == typeof(AppUser)
                                  || fk.PrincipalEntityType.ClrType == typeof(IdentityRole)).ToList())
            foreach (var p in fk.Properties)
                p.SetMaxLength(Guid);
        b.Entity<AuditLog>().Property(x => x.UserId).HasMaxLength(Guid);

        b.Entity<Apartment>().Property(x => x.Area).HasColumnType("numeric(8,2)");
        b.Entity<UtilityType>().Property(x => x.DefaultRate).HasColumnType("numeric(18,0)");

        b.Entity<AppUser>(e =>
        {
            e.Property(x => x.FullName).HasMaxLength(100);
            e.Property(x => x.PhoneNumber).HasMaxLength(20);
            e.Property(x => x.PasswordHash).HasMaxLength(256);
        });
        b.Entity<Project>().Property(x => x.Address).HasMaxLength(500);
        b.Entity<Permission>().Property(x => x.Code).HasMaxLength(40);
        b.Entity<SlaPolicy>().Property(x => x.Name).HasMaxLength(200);
        b.Entity<MaintenanceRequest>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(2000);
        });
        b.Entity<UtilityType>().Property(x => x.Name).HasMaxLength(200);
        b.Entity<Invoice>().Property(x => x.Note).HasMaxLength(1000);
        b.Entity<Payment>().Property(x => x.Note).HasMaxLength(1000);

        b.Entity<ApartmentResident>(e =>
        {
            e.Property(x => x.MoveInDate).HasColumnType("date");
            e.Property(x => x.MoveOutDate).HasColumnType("date");
        });
        b.Entity<Invoice>(e =>
        {
            e.Property(x => x.IssueDate).HasColumnType("date");
            e.Property(x => x.DueDate).HasColumnType("date");
        });
        b.Entity<UtilityReading>().Property(x => x.ReadAt).HasColumnType("date");
    }

    // Map các cột audit/soft-delete/Id chuẩn của BaseEntity sang tên tiếng Việt
    private static void MapBaseEntityColumns<T>(EntityTypeBuilder<T> e, string idColumn) where T : BaseEntity
    {
        // Khoá chính đặt tên theo bảng (KhuVucId, CanHoId…) để không có hai bảng cùng một tên cột khoá.
        e.Property(x => x.Id).HasColumnName(idColumn);
        e.Property(x => x.CreatedAt).HasColumnName("NgayTao");
        e.Property(x => x.CreatedBy).HasColumnName("NguoiTao");
        e.Property(x => x.UpdatedAt).HasColumnName("NgayCapNhat");
        e.Property(x => x.UpdatedBy).HasColumnName("NguoiCapNhat");
        e.Property(x => x.IsDeleted).HasColumnName("DaXoa");
        e.Property(x => x.DeletedAt).HasColumnName("NgayXoa");
        e.Property(x => x.DeletedBy).HasColumnName("NguoiXoa");
    }

    private static void ApplySoftDeleteFilter(ModelBuilder b)
    {
        foreach (var et in b.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(et.ClrType))
            {
                var param = Expression.Parameter(et.ClrType, "e");
                var prop = Expression.Property(param, nameof(ISoftDeletable.IsDeleted));
                var notDeleted = Expression.Equal(prop, Expression.Constant(false));
                var lambda = Expression.Lambda(notDeleted, param);
                b.Entity(et.ClrType).HasQueryFilter(lambda);
            }
        }
    }

    private static void ApplyRelationshipSoftDeleteFilters(ModelBuilder b)
    {
        b.Entity<RolePermission>()
            .HasQueryFilter(x => !x.Permission.IsDeleted);
    }

    private static void ApplyAuditColumnConstraints(ModelBuilder b)
    {
        foreach (var et in b.Model.GetEntityTypes())
        {
            if (typeof(IAuditable).IsAssignableFrom(et.ClrType))
            {
                b.Entity(et.ClrType).Property(nameof(IAuditable.CreatedBy)).HasMaxLength(36);
                b.Entity(et.ClrType).Property(nameof(IAuditable.UpdatedBy)).HasMaxLength(36);
            }
            if (typeof(ISoftDeletable).IsAssignableFrom(et.ClrType))
            {
                b.Entity(et.ClrType).Property(nameof(ISoftDeletable.DeletedBy)).HasMaxLength(36);
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var auditLogs = ApplyAuditAndCollectLogs();
        if (auditLogs.Count > 0) AuditLogs.AddRange(auditLogs);
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        var auditLogs = ApplyAuditAndCollectLogs();
        if (auditLogs.Count > 0) AuditLogs.AddRange(auditLogs);
        return base.SaveChanges();
    }

    private List<AuditLog> ApplyAuditAndCollectLogs()
    {
        var now = DateTime.UtcNow;
        var userId = _currentUser?.UserId;
        var ip = _currentUser?.IpAddress;

        var logs = new List<AuditLog>();

        foreach (var entry in ChangeTracker.Entries().ToList())
        {
            if (entry.Entity is AuditLog) continue;

            // Audit fields
            if (entry.Entity is IAuditable auditable)
            {
                if (entry.State == EntityState.Added)
                {
                    auditable.CreatedAt = now;
                    auditable.CreatedBy = userId;
                }
                else if (entry.State == EntityState.Modified)
                {
                    auditable.UpdatedAt = now;
                    auditable.UpdatedBy = userId;
                    entry.Property(nameof(IAuditable.CreatedAt)).IsModified = false;
                    entry.Property(nameof(IAuditable.CreatedBy)).IsModified = false;
                }
            }

            // Soft delete
            if (entry.State == EntityState.Deleted && entry.Entity is ISoftDeletable sd)
            {
                entry.State = EntityState.Modified;
                sd.IsDeleted = true;
                sd.DeletedAt = now;
                sd.DeletedBy = userId;
            }

            var log = BuildAuditLog(entry, userId, ip, now);
            if (log != null) logs.Add(log);
        }

        return logs;
    }

    private static readonly HashSet<string> SecretColumns = new()
    {
        nameof(AppUser.PasswordHash), nameof(AppUser.SecurityStamp), nameof(AppUser.ConcurrencyStamp)
    };

    private static AuditLog? BuildAuditLog(
        EntityEntry entry, string? userId, string? ip, DateTime now)
    {
        if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            return null;

        var entityName = entry.Entity.GetType().Name;
        if (entityName == nameof(AuditLog)) return null;

        // Khoá ghép (dòng hoá đơn, đăng ký dịch vụ) ghi đủ các phần, cách nhau bởi dấu phẩy.
        var keyValue = string.Join(",", entry.Properties.Where(p => p.Metadata.IsPrimaryKey()).Select(p => p.CurrentValue));

        var oldValues = new Dictionary<string, object?>();
        var newValues = new Dictionary<string, object?>();
        var changedColumns = new List<string>();

        foreach (var p in entry.Properties)
        {
            var name = p.Metadata.Name;
            // Giá trị bí mật chỉ ghi là có đổi, không ghi nội dung (nhật ký xem được trên giao diện).
            object? Mask(object? v) => SecretColumns.Contains(name) && v != null ? "(ẩn)" : v;
            switch (entry.State)
            {
                case EntityState.Added:
                    newValues[name] = Mask(p.CurrentValue);
                    break;
                case EntityState.Deleted:
                    oldValues[name] = Mask(p.OriginalValue);
                    break;
                // UserManager.UpdateAsync đánh dấu mọi cột là sửa; chỉ ghi cột thật sự đổi giá trị.
                case EntityState.Modified when p.IsModified && !Equals(p.OriginalValue, p.CurrentValue):
                    oldValues[name] = Mask(p.OriginalValue);
                    newValues[name] = Mask(p.CurrentValue);
                    changedColumns.Add(name);
                    break;
            }
        }

        var action = entry.State switch
        {
            EntityState.Added => AuditAction.Create,
            EntityState.Deleted => AuditAction.Delete,
            EntityState.Modified when entry.Entity is ISoftDeletable s && s.IsDeleted
                                      && changedColumns.Contains(nameof(ISoftDeletable.IsDeleted))
                => AuditAction.SoftDelete,
            _ => AuditAction.Update
        };

        return new AuditLog
        {
            EntityName = entityName,
            EntityKey = keyValue,
            Action = action,
            UserId = userId,
            IpAddress = ip,
            Timestamp = now,
            OldValues = oldValues.Count > 0 ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues.Count > 0 ? JsonSerializer.Serialize(newValues) : null
        };
    }
}
