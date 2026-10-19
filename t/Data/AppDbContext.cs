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
    public DbSet<Region> Regions => Set<Region>();
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
            e.HasIndex(u => u.IsDeleted);
            e.HasIndex(u => u.Phone);

            // Custom columns
            e.Property(x => x.FullName).HasColumnName("HoTen");
            e.Property(x => x.AvatarUrl).HasColumnName("UrlAvatar");
            e.Property(x => x.Phone).HasColumnName("SoDienThoai");
            e.Property(x => x.LastLoginAt).HasColumnName("NgayDangNhapCuoi");

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

        // ================== Region ==================
        b.Entity<Region>(e =>
        {
            e.ToTable("KhuVuc");
            MapBaseEntityColumns(e);

            e.Property(r => r.Name).HasColumnName("Ten").HasMaxLength(120).IsRequired();
            e.Property(r => r.Slug).HasColumnName("Slug").HasMaxLength(160).IsRequired();
            e.Property(r => r.ImageUrl).HasColumnName("UrlAnh");

            e.HasIndex(r => r.Slug).IsUnique();
        });

        // ================== Apartment ==================
        b.Entity<Apartment>(e =>
        {
            e.ToTable("CanHo", t =>
            {
                t.HasCheckConstraint("CK_CanHo_DienTich", "\"DienTich\" > 0");
                t.HasCheckConstraint("CK_CanHo_SoPhongNgu", "\"SoPhongNgu\" >= 0");
            });
            MapBaseEntityColumns(e);

            e.Property(a => a.UnitCode).HasColumnName("MaCanHo").HasMaxLength(40).IsRequired();
            e.Property(a => a.Area).HasColumnName("DienTich");
            e.Property(a => a.Bedrooms).HasColumnName("SoPhongNgu");
            e.Property(a => a.Bathrooms).HasColumnName("SoPhongTam");
            e.Property(a => a.HouseDirection).HasColumnName("HuongNha");
            e.Property(a => a.Occupancy).HasColumnName("TinhTrangThue");
            e.Property(a => a.Note).HasColumnName("MoTa");
            e.Property(a => a.BuildingId).HasColumnName("ToaNhaId");
            e.Property(a => a.FloorId).HasColumnName("TangId");

            e.HasIndex(a => a.Occupancy);
            e.HasIndex(a => a.FloorId);
            e.HasIndex(a => new { a.BuildingId, a.UnitCode }).IsUnique().HasFilter("\"DaXoa\" = false");

            e.HasOne(a => a.Building)
             .WithMany(b2 => b2.Apartments)
             .HasForeignKey(a => a.BuildingId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(a => a.Floor)
             .WithMany(f => f.Apartments)
             .HasForeignKey(a => a.FloorId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ================== ApartmentResident ==================
        b.Entity<ApartmentResident>(e =>
        {
            e.ToTable("CuDan", t => t.HasCheckConstraint("CK_CuDan_NgayChuyenDi",
                "\"NgayChuyenDi\" IS NULL OR \"NgayChuyenDi\" >= \"NgayVaoO\""));
            MapBaseEntityColumns(e);

            e.Property(x => x.ApartmentId).HasColumnName("CanHoId");
            e.Property(x => x.FullName).HasColumnName("HoTen").HasMaxLength(150).IsRequired();
            e.Property(x => x.Phone).HasColumnName("SoDienThoai").HasMaxLength(30);
            e.Property(x => x.Email).HasColumnName("Email").HasMaxLength(200);
            e.Property(x => x.IdNumber).HasColumnName("SoGiayTo").HasMaxLength(30);
            e.Property(x => x.Role).HasColumnName("VaiTro");
            e.Property(x => x.IsPrimary).HasColumnName("DaiDienCanHo");
            e.Property(x => x.MoveInDate).HasColumnName("NgayVaoO");
            e.Property(x => x.MoveOutDate).HasColumnName("NgayChuyenDi");
            e.Property(x => x.UserId).HasColumnName("NguoiDungId");
            e.Property(x => x.Note).HasColumnName("GhiChu").HasMaxLength(1000);
            e.Ignore(x => x.IsActive);

            e.HasIndex(x => x.ApartmentId);
            e.HasIndex(x => x.UserId);

            e.HasOne(x => x.Apartment).WithMany(a => a.Residents)
             .HasForeignKey(x => x.ApartmentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany(u => u.Residencies)
             .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        });

        // ================== Project ==================
        b.Entity<Project>(e =>
        {
            e.ToTable("DuAn");
            MapBaseEntityColumns(e);

            e.Property(p => p.Name).HasColumnName("Ten").HasMaxLength(200).IsRequired();
            e.Property(p => p.Slug).HasColumnName("Slug").HasMaxLength(220).IsRequired();
            e.Property(p => p.RegionId).HasColumnName("KhuVucId");
            e.Property(p => p.Address).HasColumnName("DiaChi");
            e.Property(p => p.ThumbnailUrl).HasColumnName("UrlAnhDaiDien");
            e.Property(p => p.ShortDescription).HasColumnName("MoTaNgan");

            e.HasIndex(p => p.Slug).IsUnique();
            e.HasIndex(p => p.RegionId);

            e.HasOne(p => p.Region)
             .WithMany(r => r.Projects)
             .HasForeignKey(p => p.RegionId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ================== Building ==================
        b.Entity<Building>(e =>
        {
            e.ToTable("ToaNha", t =>
            {
                t.HasCheckConstraint("CK_ToaNha_DonGiaPhiQuanLy", "\"DonGiaPhiQuanLy\" >= 0");
                t.HasCheckConstraint("CK_ToaNha_NgayChotKy", "\"NgayChotKyMacDinh\" BETWEEN 1 AND 28");
            });
            MapBaseEntityColumns(e);

            e.Property(x => x.Name).HasColumnName("Ten").HasMaxLength(200).IsRequired();
            e.Property(x => x.Slug).HasColumnName("Slug").HasMaxLength(220).IsRequired();
            e.Property(x => x.Code).HasColumnName("Ma").HasMaxLength(40);
            e.Property(x => x.ProjectId).HasColumnName("DuAnId");
            e.Property(x => x.RegionId).HasColumnName("KhuVucId");
            e.Property(x => x.Address).HasColumnName("DiaChi").HasMaxLength(500);
            e.Property(x => x.FloorCount).HasColumnName("SoLuongTang");
            e.Property(x => x.ThumbnailUrl).HasColumnName("UrlAnhDaiDien");
            e.Property(x => x.Description).HasColumnName("MoTa");
            e.Property(x => x.ManagerId).HasColumnName("QuanLyId");
            e.Property(x => x.Status).HasColumnName("TrangThai");
            e.Property(x => x.DefaultBillingDay).HasColumnName("NgayChotKyMacDinh").HasDefaultValue(1);
            e.Property(x => x.DefaultLateFeeAfterDays).HasColumnName("SoNgayTreHanMacDinh").HasDefaultValue(7);
            e.Property(x => x.DefaultLateFeePercent).HasColumnName("PhanTramPhiTreMacDinh").HasDefaultValue(5);
            e.Property(x => x.ManagementFeePerM2).HasColumnName("DonGiaPhiQuanLy").HasColumnType("decimal(18,0)");

            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => x.RegionId);
            e.HasIndex(x => x.ProjectId);
            e.HasIndex(x => x.Status);

            e.HasOne(x => x.Region).WithMany().HasForeignKey(x => x.RegionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Project).WithMany(p => p.Buildings).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Manager).WithMany(u => u.ManagedBuildings).HasForeignKey(x => x.ManagerId).OnDelete(DeleteBehavior.SetNull);
        });

        // ================== Floor ==================
        b.Entity<Floor>(e =>
        {
            e.ToTable("Tang");
            MapBaseEntityColumns(e);

            e.Property(x => x.BuildingId).HasColumnName("ToaNhaId");
            e.Property(x => x.Number).HasColumnName("SoTang");
            e.Property(x => x.Label).HasColumnName("NhanTang").HasMaxLength(50);

            e.HasIndex(x => new { x.BuildingId, x.Number }).IsUnique();

            e.HasOne(x => x.Building).WithMany(b2 => b2.Floors)
             .HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.Cascade);
        });

        // ================== MaintenanceRequest ==================
        b.Entity<MaintenanceRequest>(e =>
        {
            e.ToTable("YeuCauBaoTri", t =>
            {
                // BR: sự cố ở đúng một nơi, trong căn hoặc khu vực chung (có toà và mô tả vị trí).
                t.HasCheckConstraint("CK_YeuCauBaoTri_NoiXayRa",
                    "(\"CanHoId\" IS NOT NULL AND \"ToaNhaKhuVucChungId\" IS NULL) OR " +
                    "(\"CanHoId\" IS NULL AND \"ToaNhaKhuVucChungId\" IS NOT NULL AND \"ViTri\" IS NOT NULL)");
                t.HasCheckConstraint("CK_YeuCauBaoTri_ChiPhi", "\"ChiPhiUocTinh\" >= 0 AND \"ChiPhiThucTe\" >= 0");
                t.HasCheckConstraint("CK_YeuCauBaoTri_DanhGia", "\"DanhGia\" IS NULL OR \"DanhGia\" BETWEEN 1 AND 5");
                t.HasCheckConstraint("CK_YeuCauBaoTri_HanXuLy", "\"HanXuLy\" IS NULL OR \"HanXuLy\" >= \"NgayBaoCao\"");
            });
            MapBaseEntityColumns(e);

            e.Property(x => x.RequestNumber).HasColumnName("SoYeuCau").HasMaxLength(40).IsRequired();
            e.Property(x => x.ApartmentId).HasColumnName("CanHoId");
            e.Property(x => x.CommonBuildingId).HasColumnName("ToaNhaKhuVucChungId");
            e.Property(x => x.Location).HasColumnName("ViTri").HasMaxLength(200);
            e.Property(x => x.ReporterId).HasColumnName("NguoiBaoCaoId");
            e.Property(x => x.Title).HasColumnName("TieuDe").HasMaxLength(250).IsRequired();
            e.Property(x => x.Description).HasColumnName("MoTa").HasMaxLength(4000);
            e.Property(x => x.Category).HasColumnName("LoaiBaoTri");
            e.Property(x => x.Priority).HasColumnName("MucDoUuTien");
            e.Property(x => x.Status).HasColumnName("TrangThai");
            e.Property(x => x.AssignedToId).HasColumnName("NguoiPhuTrachId");
            e.Property(x => x.ResolutionNote).HasColumnName("GhiChuXuLy").HasMaxLength(2000);
            e.Property(x => x.EstimatedCost).HasColumnName("ChiPhiUocTinh").HasColumnType("decimal(18,0)");
            e.Property(x => x.ActualCost).HasColumnName("ChiPhiThucTe").HasColumnType("decimal(18,0)");
            e.Property(x => x.ChargeToTenant).HasColumnName("TinhPhiKhachThue");
            e.Property(x => x.ReportedAt).HasColumnName("NgayBaoCao");
            e.Property(x => x.AcknowledgedAt).HasColumnName("NgayTiepNhan");
            e.Property(x => x.ResolvedAt).HasColumnName("NgayGiaiQuyet");
            e.Property(x => x.ClosedAt).HasColumnName("NgayDongLai");
            e.Property(x => x.DueAt).HasColumnName("HanXuLy");
            e.Property(x => x.Rating).HasColumnName("DanhGia");
            e.Property(x => x.RatingComment).HasColumnName("NhanXet").HasMaxLength(1000);

            e.HasIndex(x => x.RequestNumber).IsUnique();
            e.HasIndex(x => x.ApartmentId);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.Priority);
            e.HasIndex(x => x.AssignedToId);
            e.HasIndex(x => x.CommonBuildingId);
            e.HasIndex(x => x.DueAt);

            e.HasOne(x => x.Apartment).WithMany(a => a.MaintenanceRequests)
             .HasForeignKey(x => x.ApartmentId).OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.CommonBuilding).WithMany()
             .HasForeignKey(x => x.CommonBuildingId).OnDelete(DeleteBehavior.Restrict);

            // Mức ưu tiên phải là một mức có trong bảng SLA (không xoá được mức SLA đang được dùng).
            e.HasOne<SlaPolicy>().WithMany()
             .HasForeignKey(x => x.Priority).OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Reporter).WithMany()
             .HasForeignKey(x => x.ReporterId).OnDelete(DeleteBehavior.NoAction);

            e.HasOne(x => x.AssignedTo).WithMany()
             .HasForeignKey(x => x.AssignedToId).OnDelete(DeleteBehavior.NoAction);
        });

        // ================== MaintenancePhoto / StatusChange / SlaPolicy ==================
        b.Entity<MaintenancePhoto>(e =>
        {
            e.ToTable("AnhSuCo");
            e.HasQueryFilter(x => !x.MaintenanceRequest.IsDeleted); // khớp bộ lọc xoá mềm của yêu cầu cha
            e.Property(x => x.MaintenanceRequestId).HasColumnName("YeuCauBaoTriId");
            e.Property(x => x.Url).HasColumnName("DuongDan").HasMaxLength(500).IsRequired();
            e.Property(x => x.Stage).HasColumnName("GiaiDoan");
            e.Property(x => x.UploadedById).HasColumnName("NguoiTaiLenId");
            e.Property(x => x.UploadedAt).HasColumnName("NgayTaiLen");
            e.HasIndex(x => x.MaintenanceRequestId);
            e.HasOne(x => x.MaintenanceRequest).WithMany(r => r.Photos)
             .HasForeignKey(x => x.MaintenanceRequestId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UploadedById).OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<MaintenanceStatusChange>(e =>
        {
            e.ToTable("LichSuTrangThaiSuCo");
            e.HasQueryFilter(x => !x.MaintenanceRequest.IsDeleted);
            e.Property(x => x.MaintenanceRequestId).HasColumnName("YeuCauBaoTriId");
            e.Property(x => x.FromStatus).HasColumnName("TuTrangThai");
            e.Property(x => x.ToStatus).HasColumnName("SangTrangThai");
            e.Property(x => x.ChangedById).HasColumnName("NguoiThucHienId");
            e.Property(x => x.ChangedAt).HasColumnName("ThoiDiem");
            e.Property(x => x.Note).HasColumnName("GhiChu").HasMaxLength(1000);
            e.HasIndex(x => new { x.MaintenanceRequestId, x.ChangedAt });
            e.HasOne(x => x.MaintenanceRequest).WithMany(r => r.History)
             .HasForeignKey(x => x.MaintenanceRequestId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ChangedBy).WithMany().HasForeignKey(x => x.ChangedById).OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<SlaPolicy>(e =>
        {
            e.ToTable("MucSla", t => t.HasCheckConstraint("CK_MucSla_ThoiHan",
                "\"PhutPhanHoi\" > 0 AND \"PhutHoanTat\" >= \"PhutPhanHoi\""));
            e.HasKey(x => x.Priority);
            e.Property(x => x.Priority).HasColumnName("MucDoUuTien").ValueGeneratedNever();
            e.Property(x => x.ResponseMinutes).HasColumnName("PhutPhanHoi");
            e.Property(x => x.ResolveMinutes).HasColumnName("PhutHoanTat");
            e.HasData(SlaPolicy.Defaults);
        });

        // ================== UtilityType ==================
        b.Entity<UtilityType>(e =>
        {
            e.ToTable("LoaiDichVu");
            MapBaseEntityColumns(e);

            e.Property(x => x.Code).HasColumnName("Ma").HasMaxLength(40).IsRequired();
            e.Property(x => x.Name).HasColumnName("Ten").HasMaxLength(120).IsRequired();
            e.Property(x => x.Unit).HasColumnName("DonViTinh").HasMaxLength(20);
            e.Property(x => x.BillingMode).HasColumnName("CheDoTinhPhi");
            e.Property(x => x.DefaultRate).HasColumnName("DonGiaMacDinh").HasColumnType("decimal(18,2)");
            e.Property(x => x.Icon).HasColumnName("Icon");

            e.HasIndex(x => x.Code).IsUnique();
        });

        // ================== UtilityReading ==================
        b.Entity<UtilityReading>(e =>
        {
            e.ToTable("ChiSoDichVu", t => t.HasCheckConstraint("CK_ChiSoDichVu_ChiSo", "\"ChiSoMoi\" >= \"ChiSoCu\""));
            MapBaseEntityColumns(e);

            e.Property(x => x.ApartmentId).HasColumnName("CanHoId");
            e.Property(x => x.UtilityTypeId).HasColumnName("LoaiDichVuId");
            e.Property(x => x.BillingMonth).HasColumnName("ThangTinhPhi");
            e.Property(x => x.PreviousReading).HasColumnName("ChiSoCu").HasColumnType("decimal(18,2)");
            e.Property(x => x.CurrentReading).HasColumnName("ChiSoMoi").HasColumnType("decimal(18,2)");
            e.Property(x => x.Consumption).HasColumnName("TieuThu").HasColumnType("decimal(18,2)");
            e.Property(x => x.Rate).HasColumnName("DonGia").HasColumnType("decimal(18,2)");
            e.Property(x => x.Amount).HasColumnName("ThanhTien").HasColumnType("decimal(18,0)");
            e.Property(x => x.Billed).HasColumnName("DaXuatHoaDon");
            e.Property(x => x.ReadAt).HasColumnName("NgayDocChiSo");
            e.Property(x => x.Note).HasColumnName("GhiChu");

            e.HasIndex(x => new { x.ApartmentId, x.UtilityTypeId, x.BillingMonth }).IsUnique().HasFilter("\"DaXoa\" = false");
            e.HasIndex(x => x.BillingMonth);

            e.HasOne(x => x.Apartment).WithMany(a => a.UtilityReadings)
             .HasForeignKey(x => x.ApartmentId).OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.UtilityType).WithMany(t => t.Readings)
             .HasForeignKey(x => x.UtilityTypeId).OnDelete(DeleteBehavior.Restrict);
        });

        // ================== Invoice ==================
        b.Entity<Invoice>(e =>
        {
            e.ToTable("HoaDon", t =>
            {
                t.HasCheckConstraint("CK_HoaDon_SoTien", "\"TongTien\" >= 0 AND \"DaThanhToan\" >= 0");
                t.HasCheckConstraint("CK_HoaDon_ConLai", "\"ConLai\" >= 0 AND \"ConLai\" <= \"TongTien\"");
            });
            MapBaseEntityColumns(e);

            e.Property(x => x.InvoiceNumber).HasColumnName("SoHoaDon").HasMaxLength(40).IsRequired();
            e.Property(x => x.ApartmentId).HasColumnName("CanHoId");
            e.Property(x => x.Kind).HasColumnName("LoaiHoaDon");
            e.Property(x => x.BillingMonth).HasColumnName("ThangTinhPhi");
            e.Property(x => x.IsRecurring).HasColumnName("DinhKy");
            e.Property(x => x.IssueDate).HasColumnName("NgayPhatHanh");
            e.Property(x => x.DueDate).HasColumnName("NgayDenHan");
            e.Property(x => x.SubTotal).HasColumnName("TamTinh").HasColumnType("decimal(18,0)");
            e.Property(x => x.Discount).HasColumnName("GiamGia").HasColumnType("decimal(18,0)");
            e.Property(x => x.Tax).HasColumnName("Thue").HasColumnType("decimal(18,0)");
            e.Property(x => x.LateFee).HasColumnName("PhiTreHan").HasColumnType("decimal(18,0)");
            e.Property(x => x.Total).HasColumnName("TongTien").HasColumnType("decimal(18,0)");
            e.Property(x => x.AmountPaid).HasColumnName("DaThanhToan").HasColumnType("decimal(18,0)");
            e.Property(x => x.Balance).HasColumnName("ConLai").HasColumnType("decimal(18,0)");
            e.Property(x => x.Status).HasColumnName("TrangThai");
            e.Property(x => x.Currency).HasColumnName("DonViTienTe").HasMaxLength(8).IsRequired();
            e.Property(x => x.Note).HasColumnName("GhiChu");

            e.HasIndex(x => x.InvoiceNumber).IsUnique();
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.DueDate);
            e.HasIndex(x => new { x.ApartmentId, x.BillingMonth, x.Kind });
            // BR: mỗi căn chỉ một hoá đơn phí tháng còn hiệu lực cho mỗi kỳ (huỷ thì được lập lại).
            e.HasIndex(x => new { x.ApartmentId, x.BillingMonth })
             .HasDatabaseName("UX_HoaDon_PhiThang_MoiKy").IsUnique()
             .HasFilter("\"LoaiHoaDon\" = 0 AND \"TrangThai\" <> 5 AND \"DaXoa\" = false");

            e.HasOne(x => x.Apartment)
             .WithMany(a => a.Invoices)
             .HasForeignKey(x => x.ApartmentId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ================== InvoiceItem ==================
        b.Entity<InvoiceItem>(e =>
        {
            e.ToTable("ChiTietHoaDon");
            MapBaseEntityColumns(e);

            e.Property(x => x.InvoiceId).HasColumnName("HoaDonId");
            e.Property(x => x.Description).HasColumnName("MoTa").HasMaxLength(500).IsRequired();
            e.Property(x => x.Quantity).HasColumnName("SoLuong").HasColumnType("decimal(18,2)");
            e.Property(x => x.UnitPrice).HasColumnName("DonGia").HasColumnType("decimal(18,0)");
            e.Property(x => x.LineTotal).HasColumnName("ThanhTien").HasColumnType("decimal(18,0)");
            e.Property(x => x.SortOrder).HasColumnName("ThuTu");

            e.HasIndex(x => x.InvoiceId);

            e.HasOne(x => x.Invoice)
             .WithMany(i => i.Items)
             .HasForeignKey(x => x.InvoiceId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ================== RecurringCharge ==================
        b.Entity<RecurringCharge>(e =>
        {
            e.ToTable("PhiDinhKy", t => t.HasCheckConstraint("CK_PhiDinhKy_SoTien", "\"SoTien\" >= 0"));
            MapBaseEntityColumns(e);

            e.Property(x => x.ApartmentId).HasColumnName("CanHoId");
            e.Property(x => x.Description).HasColumnName("MoTa").HasMaxLength(250).IsRequired();
            e.Property(x => x.Amount).HasColumnName("SoTien").HasColumnType("decimal(18,0)");
            e.Property(x => x.SortOrder).HasColumnName("ThuTu");
            e.Property(x => x.IsActive).HasColumnName("DangApDung");

            e.HasIndex(x => x.ApartmentId);

            e.HasOne(x => x.Apartment)
             .WithMany(a => a.RecurringCharges)
             .HasForeignKey(x => x.ApartmentId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ================== Payment ==================
        b.Entity<Payment>(e =>
        {
            e.ToTable("ThanhToan", t => t.HasCheckConstraint("CK_ThanhToan_SoTien", "\"SoTien\" > 0"));
            MapBaseEntityColumns(e);

            e.Property(x => x.PaymentNumber).HasColumnName("SoThanhToan").HasMaxLength(40).IsRequired();
            e.Property(x => x.InvoiceId).HasColumnName("HoaDonId");
            e.Property(x => x.Amount).HasColumnName("SoTien").HasColumnType("decimal(18,0)");
            e.Property(x => x.Currency).HasColumnName("DonViTienTe").HasMaxLength(8).IsRequired();
            e.Property(x => x.Method).HasColumnName("PhuongThuc");
            e.Property(x => x.Status).HasColumnName("TrangThai");
            e.Property(x => x.TransactionRef).HasColumnName("MaGiaoDich").HasMaxLength(120);
            e.Property(x => x.Provider).HasColumnName("NhaCungCap").HasMaxLength(80);
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
            MapBaseEntityColumns(e);

            e.Property(x => x.Code).HasColumnName("Ma").HasMaxLength(100).IsRequired();
            e.Property(x => x.Module).HasColumnName("Module").HasMaxLength(60).IsRequired();
            e.Property(x => x.DisplayName).HasColumnName("TenHienThi").HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasColumnName("MoTa");

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
            e.Property(x => x.EntityKey).HasColumnName("KhoaBang").HasMaxLength(120);
            e.Property(x => x.Action).HasColumnName("HanhDong");
            e.Property(x => x.UserId).HasColumnName("NguoiDungId").HasMaxLength(450);
            e.Property(x => x.UserName).HasColumnName("TenNguoiDung").HasMaxLength(256);
            e.Property(x => x.Timestamp).HasColumnName("ThoiGian");
            e.Property(x => x.OldValues).HasColumnName("GiaTriCu");
            e.Property(x => x.NewValues).HasColumnName("GiaTriMoi");
            e.Property(x => x.ChangedColumns).HasColumnName("CotThayDoi");
            e.Property(x => x.IpAddress).HasColumnName("DiaChiIp").HasMaxLength(64);
            e.Property(x => x.UserAgent).HasColumnName("UserAgent").HasMaxLength(512);

            e.HasIndex(x => new { x.EntityName, x.EntityKey });
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.Timestamp);
        });

        // Soft delete global query filter
        ApplySoftDeleteFilter(b);
        ApplyRelationshipSoftDeleteFilters(b);

        // Audit columns max length
        ApplyAuditColumnConstraints(b);
    }

    // Map các cột audit/soft-delete/Id/RowVersion chuẩn của BaseEntity sang tên tiếng Việt
    private static void MapBaseEntityColumns<T>(EntityTypeBuilder<T> e) where T : BaseEntity
    {
        e.Property(x => x.Id).HasColumnName("Id");
        e.Property(x => x.CreatedAt).HasColumnName("NgayTao");
        e.Property(x => x.CreatedBy).HasColumnName("NguoiTao");
        e.Property(x => x.UpdatedAt).HasColumnName("NgayCapNhat");
        e.Property(x => x.UpdatedBy).HasColumnName("NguoiCapNhat");
        e.Property(x => x.IsDeleted).HasColumnName("DaXoa");
        e.Property(x => x.DeletedAt).HasColumnName("NgayXoa");
        e.Property(x => x.DeletedBy).HasColumnName("NguoiXoa");
        e.Property(x => x.RowVersion).HasColumnName("PhienBan");
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
                b.Entity(et.ClrType).Property(nameof(IAuditable.CreatedBy)).HasMaxLength(450);
                b.Entity(et.ClrType).Property(nameof(IAuditable.UpdatedBy)).HasMaxLength(450);
            }
            if (typeof(ISoftDeletable).IsAssignableFrom(et.ClrType))
            {
                b.Entity(et.ClrType).Property(nameof(ISoftDeletable.DeletedBy)).HasMaxLength(450);
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
        var userName = _currentUser?.UserName;
        var ip = _currentUser?.IpAddress;
        var ua = _currentUser?.UserAgent;

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

            var log = BuildAuditLog(entry, userId, userName, ip, ua, now);
            if (log != null) logs.Add(log);
        }

        return logs;
    }

    private static AuditLog? BuildAuditLog(
        EntityEntry entry, string? userId, string? userName,
        string? ip, string? ua, DateTime now)
    {
        if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            return null;

        var entityName = entry.Entity.GetType().Name;
        if (entityName == nameof(AuditLog)) return null;

        var keyProp = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
        var keyValue = keyProp?.CurrentValue?.ToString();

        var oldValues = new Dictionary<string, object?>();
        var newValues = new Dictionary<string, object?>();
        var changedColumns = new List<string>();

        foreach (var p in entry.Properties)
        {
            var name = p.Metadata.Name;
            switch (entry.State)
            {
                case EntityState.Added:
                    newValues[name] = p.CurrentValue;
                    break;
                case EntityState.Deleted:
                    oldValues[name] = p.OriginalValue;
                    break;
                case EntityState.Modified when p.IsModified:
                    oldValues[name] = p.OriginalValue;
                    newValues[name] = p.CurrentValue;
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
            UserName = userName,
            IpAddress = ip,
            UserAgent = ua,
            Timestamp = now,
            OldValues = oldValues.Count > 0 ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues.Count > 0 ? JsonSerializer.Serialize(newValues) : null,
            ChangedColumns = changedColumns.Count > 0 ? string.Join(",", changedColumns) : null
        };
    }
}
