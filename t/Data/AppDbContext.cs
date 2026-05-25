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
    public DbSet<ApartmentImage> ApartmentImages => Set<ApartmentImage>();
    public DbSet<Amenity> Amenities => Set<Amenity>();
    public DbSet<ApartmentAmenity> ApartmentAmenities => Set<ApartmentAmenity>();
    public DbSet<Region> Regions => Set<Region>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectImage> ProjectImages => Set<ProjectImage>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Favorite> Favorites => Set<Favorite>();

    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<Floor> Floors => Set<Floor>();
    public DbSet<Lease> Leases => Set<Lease>();
    public DbSet<LeaseTenant> LeaseTenants => Set<LeaseTenant>();
    public DbSet<UtilityType> UtilityTypes => Set<UtilityType>();
    public DbSet<UtilityReading> UtilityReadings => Set<UtilityReading>();

    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<MaintenanceRequest> MaintenanceRequests => Set<MaintenanceRequest>();
    public DbSet<LeaseInspection> LeaseInspections => Set<LeaseInspection>();
    public DbSet<DepositTransaction> DepositTransactions => Set<DepositTransaction>();

    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ViewingAppointment> ViewingAppointments => Set<ViewingAppointment>();

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
            e.Property(x => x.IsHost).HasColumnName("LaChuNha");
            e.Property(x => x.HostTitle).HasColumnName("DanhXungChuNha");
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

        // ================== Category ==================
        b.Entity<Category>(e =>
        {
            e.ToTable("DanhMuc");
            MapBaseEntityColumns(e);

            e.Property(c => c.Name).HasColumnName("Ten").HasMaxLength(120).IsRequired();
            e.Property(c => c.Slug).HasColumnName("Slug").HasMaxLength(160).IsRequired();
            e.Property(c => c.Icon).HasColumnName("Icon");

            e.HasIndex(c => c.Slug).IsUnique();
        });

        // ================== Amenity ==================
        b.Entity<Amenity>(e =>
        {
            e.ToTable("TienIch");
            MapBaseEntityColumns(e);

            e.Property(a => a.Name).HasColumnName("Ten").HasMaxLength(120).IsRequired();
            e.Property(a => a.Slug).HasColumnName("Slug").HasMaxLength(160).IsRequired();
            e.Property(a => a.Icon).HasColumnName("Icon");

            e.HasIndex(a => a.Slug).IsUnique();
        });

        // ================== Apartment ==================
        b.Entity<Apartment>(e =>
        {
            e.ToTable("CanHo");
            MapBaseEntityColumns(e);

            e.Property(a => a.Title).HasColumnName("TieuDe").HasMaxLength(250).IsRequired();
            e.Property(a => a.Slug).HasColumnName("Slug").HasMaxLength(280).IsRequired();
            e.Property(a => a.UnitCode).HasColumnName("MaCanHo").HasMaxLength(40);
            e.Property(a => a.Description).HasColumnName("MoTa");
            e.Property(a => a.DescriptionExtra).HasColumnName("MoTaThem");
            e.Property(a => a.Price).HasColumnName("Gia").HasColumnType("decimal(18,0)");
            e.Property(a => a.DefaultDeposit).HasColumnName("TienDatCocMacDinh").HasColumnType("decimal(18,0)");
            e.Property(a => a.FeeNote).HasColumnName("GhiChuPhi");
            e.Property(a => a.Area).HasColumnName("DienTich");
            e.Property(a => a.Bedrooms).HasColumnName("SoPhongNgu");
            e.Property(a => a.Bathrooms).HasColumnName("SoPhongTam");
            e.Property(a => a.Address).HasColumnName("DiaChi").HasMaxLength(500);
            e.Property(a => a.Latitude).HasColumnName("ViDo");
            e.Property(a => a.Longitude).HasColumnName("KinhDo");
            e.Property(a => a.Status).HasColumnName("TrangThai");
            e.Property(a => a.Occupancy).HasColumnName("TinhTrangThue");
            e.Property(a => a.IsFeatured).HasColumnName("NoiBat");
            e.Property(a => a.ViewCount).HasColumnName("LuotXem");
            e.Property(a => a.ModerationNote).HasColumnName("GhiChuKiemDuyet");
            e.Property(a => a.ApprovedAt).HasColumnName("NgayDuyet");
            e.Property(a => a.ApprovedBy).HasColumnName("NguoiDuyet");
            e.Property(a => a.HostId).HasColumnName("ChuNhaId");
            e.Property(a => a.RegionId).HasColumnName("KhuVucId");
            e.Property(a => a.CategoryId).HasColumnName("DanhMucId");
            e.Property(a => a.ProjectId).HasColumnName("DuAnId");
            e.Property(a => a.BuildingId).HasColumnName("ToaNhaId");
            e.Property(a => a.FloorId).HasColumnName("TangId");

            e.HasIndex(a => a.Slug).IsUnique();
            e.HasIndex(a => a.Status);
            e.HasIndex(a => a.Occupancy);
            e.HasIndex(a => a.Price);
            e.HasIndex(a => a.RegionId);
            e.HasIndex(a => a.CategoryId);
            e.HasIndex(a => a.ProjectId);
            e.HasIndex(a => a.BuildingId);
            e.HasIndex(a => a.FloorId);
            e.HasIndex(a => a.HostId);
            e.HasIndex(a => a.IsFeatured);
            e.HasIndex(a => new { a.BuildingId, a.UnitCode })
             .IsUnique()
             .HasFilter("\"ToaNhaId\" IS NOT NULL AND \"MaCanHo\" IS NOT NULL");

            e.HasOne(a => a.Host)
             .WithMany(u => u.Apartments)
             .HasForeignKey(a => a.HostId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(a => a.Region)
             .WithMany(r => r.Apartments)
             .HasForeignKey(a => a.RegionId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(a => a.Category)
             .WithMany(c => c.Apartments)
             .HasForeignKey(a => a.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(a => a.Project)
             .WithMany(p => p.Apartments)
             .HasForeignKey(a => a.ProjectId)
             .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(a => a.Building)
             .WithMany(b2 => b2.Apartments)
             .HasForeignKey(a => a.BuildingId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(a => a.Floor)
             .WithMany(f => f.Apartments)
             .HasForeignKey(a => a.FloorId)
             .OnDelete(DeleteBehavior.NoAction);
        });

        // ================== ApartmentImage ==================
        b.Entity<ApartmentImage>(e =>
        {
            e.ToTable("AnhCanHo");
            MapBaseEntityColumns(e);

            e.Property(i => i.Url).HasColumnName("Url").HasMaxLength(500).IsRequired();
            e.Property(i => i.Caption).HasColumnName("ChuThich");
            e.Property(i => i.IsCover).HasColumnName("AnhBia");
            e.Property(i => i.SortOrder).HasColumnName("ThuTu");
            e.Property(i => i.ApartmentId).HasColumnName("CanHoId");

            e.HasIndex(i => i.ApartmentId);

            e.HasOne(i => i.Apartment)
             .WithMany(a => a.Images)
             .HasForeignKey(i => i.ApartmentId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ================== ApartmentAmenity (junction) ==================
        b.Entity<ApartmentAmenity>(e =>
        {
            e.ToTable("CanHo_TienIch");
            e.HasKey(aa => new { aa.ApartmentId, aa.AmenityId });

            e.Property(aa => aa.ApartmentId).HasColumnName("CanHoId");
            e.Property(aa => aa.AmenityId).HasColumnName("TienIchId");
            e.Property(aa => aa.CreatedAt).HasColumnName("NgayTao");
            e.Property(aa => aa.CreatedBy).HasColumnName("NguoiTao");
            e.Property(aa => aa.UpdatedAt).HasColumnName("NgayCapNhat");
            e.Property(aa => aa.UpdatedBy).HasColumnName("NguoiCapNhat");

            e.HasOne(aa => aa.Apartment)
             .WithMany(a => a.ApartmentAmenities)
             .HasForeignKey(aa => aa.ApartmentId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(aa => aa.Amenity)
             .WithMany(a => a.ApartmentAmenities)
             .HasForeignKey(aa => aa.AmenityId)
             .OnDelete(DeleteBehavior.Cascade);
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
            e.Property(p => p.PriceFrom).HasColumnName("GiaTu").HasColumnType("decimal(18,0)");
            e.Property(p => p.Status).HasColumnName("TrangThai");
            e.Property(p => p.ShortDescription).HasColumnName("MoTaNgan");
            e.Property(p => p.FullDescription).HasColumnName("MoTaDayDu");

            e.HasIndex(p => p.Slug).IsUnique();
            e.HasIndex(p => p.RegionId);
            e.HasIndex(p => p.Status);

            e.HasOne(p => p.Region)
             .WithMany(r => r.Projects)
             .HasForeignKey(p => p.RegionId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ================== ProjectImage ==================
        b.Entity<ProjectImage>(e =>
        {
            e.ToTable("AnhDuAn");
            MapBaseEntityColumns(e);

            e.Property(i => i.ProjectId).HasColumnName("DuAnId");
            e.Property(i => i.Url).HasColumnName("Url").HasMaxLength(500).IsRequired();
            e.Property(i => i.Caption).HasColumnName("ChuThich");
            e.Property(i => i.SortOrder).HasColumnName("ThuTu");
            e.Property(i => i.IsCover).HasColumnName("AnhBia");

            e.HasIndex(i => i.ProjectId);

            e.HasOne(i => i.Project)
             .WithMany(p => p.Images)
             .HasForeignKey(i => i.ProjectId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ================== Review ==================
        b.Entity<Review>(e =>
        {
            e.ToTable("DanhGia");
            MapBaseEntityColumns(e);

            e.Property(r => r.Rating).HasColumnName("DiemSao");
            e.Property(r => r.Content).HasColumnName("NoiDung").HasMaxLength(2000).IsRequired();
            e.Property(r => r.RenterNote).HasColumnName("GhiChuNguoiThue");
            e.Property(r => r.Status).HasColumnName("TrangThai");
            e.Property(r => r.ApprovedAt).HasColumnName("NgayDuyet");
            e.Property(r => r.ApprovedBy).HasColumnName("NguoiDuyet");
            e.Property(r => r.ApartmentId).HasColumnName("CanHoId");
            e.Property(r => r.UserId).HasColumnName("NguoiDungId");

            e.HasIndex(r => r.ApartmentId);
            e.HasIndex(r => r.UserId);
            e.HasIndex(r => r.Status);

            e.HasOne(r => r.Apartment)
             .WithMany(a => a.Reviews)
             .HasForeignKey(r => r.ApartmentId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.User)
             .WithMany(u => u.Reviews)
             .HasForeignKey(r => r.UserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ================== Favorite ==================
        b.Entity<Favorite>(e =>
        {
            e.ToTable("YeuThich");
            MapBaseEntityColumns(e);

            e.Property(f => f.UserId).HasColumnName("NguoiDungId");
            e.Property(f => f.ApartmentId).HasColumnName("CanHoId");

            e.HasIndex(f => new { f.UserId, f.ApartmentId }).IsUnique();

            e.HasOne(f => f.Apartment)
             .WithMany(a => a.Favorites)
             .HasForeignKey(f => f.ApartmentId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(f => f.User)
             .WithMany(u => u.Favorites)
             .HasForeignKey(f => f.UserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ================== Building ==================
        b.Entity<Building>(e =>
        {
            e.ToTable("ToaNha");
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

            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => x.RegionId);
            e.HasIndex(x => x.ProjectId);
            e.HasIndex(x => x.Status);

            e.HasOne(x => x.Region).WithMany().HasForeignKey(x => x.RegionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.SetNull);
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

        // ================== Lease ==================
        b.Entity<Lease>(e =>
        {
            e.ToTable("HopDongThue");
            MapBaseEntityColumns(e);

            e.Property(x => x.LeaseNumber).HasColumnName("SoHopDong").HasMaxLength(40).IsRequired();
            e.Property(x => x.ApartmentId).HasColumnName("CanHoId");
            e.Property(x => x.PrimaryTenantId).HasColumnName("NguoiThueChinhId");
            e.Property(x => x.StartDate).HasColumnName("NgayBatDau");
            e.Property(x => x.EndDate).HasColumnName("NgayKetThuc");
            e.Property(x => x.MonthlyRent).HasColumnName("TienThueThang").HasColumnType("decimal(18,0)");
            e.Property(x => x.Deposit).HasColumnName("TienDatCoc").HasColumnType("decimal(18,0)");
            e.Property(x => x.DepositHeld).HasColumnName("TienDatCocDangGiu").HasColumnType("decimal(18,0)");
            e.Property(x => x.DepositRefunded).HasColumnName("TienDatCocDaHoanTra").HasColumnType("decimal(18,0)");
            e.Property(x => x.BillingDay).HasColumnName("NgayChotKy");
            e.Property(x => x.LateFeePercent).HasColumnName("PhanTramPhiTre");
            e.Property(x => x.LateFeeAfterDays).HasColumnName("SoNgayTreHan");
            e.Property(x => x.Status).HasColumnName("TrangThai");
            e.Property(x => x.ActivatedAt).HasColumnName("NgayKichHoat");
            e.Property(x => x.TerminatedAt).HasColumnName("NgayChamDut");
            e.Property(x => x.TerminationReason).HasColumnName("LyDoChamDut");
            e.Property(x => x.ContractUrl).HasColumnName("UrlHopDong");
            e.Property(x => x.Notes).HasColumnName("GhiChu").HasMaxLength(2000);

            e.HasIndex(x => x.LeaseNumber).IsUnique();
            e.HasIndex(x => x.ApartmentId);
            e.HasIndex(x => x.PrimaryTenantId);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => new { x.StartDate, x.EndDate });

            e.HasOne(x => x.Apartment).WithMany(a => a.Leases)
             .HasForeignKey(x => x.ApartmentId).OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.PrimaryTenant).WithMany(u => u.Leases)
             .HasForeignKey(x => x.PrimaryTenantId).OnDelete(DeleteBehavior.Restrict);
        });

        // ================== LeaseTenant (junction) ==================
        b.Entity<LeaseTenant>(e =>
        {
            e.ToTable("HopDong_NguoiThue");
            e.HasKey(x => new { x.LeaseId, x.TenantId });

            e.Property(x => x.LeaseId).HasColumnName("HopDongId");
            e.Property(x => x.TenantId).HasColumnName("NguoiThueId");
            e.Property(x => x.Relationship).HasColumnName("MoiQuanHe").HasMaxLength(60);
            e.Property(x => x.CreatedAt).HasColumnName("NgayTao");
            e.Property(x => x.CreatedBy).HasColumnName("NguoiTao");
            e.Property(x => x.UpdatedAt).HasColumnName("NgayCapNhat");
            e.Property(x => x.UpdatedBy).HasColumnName("NguoiCapNhat");

            e.HasOne(x => x.Lease).WithMany(l => l.AdditionalTenants)
             .HasForeignKey(x => x.LeaseId).OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Tenant).WithMany(u => u.CoTenancies)
             .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        });

        // ================== MaintenanceRequest ==================
        b.Entity<MaintenanceRequest>(e =>
        {
            e.ToTable("YeuCauBaoTri");
            MapBaseEntityColumns(e);

            e.Property(x => x.RequestNumber).HasColumnName("SoYeuCau").HasMaxLength(40).IsRequired();
            e.Property(x => x.ApartmentId).HasColumnName("CanHoId");
            e.Property(x => x.LeaseId).HasColumnName("HopDongId");
            e.Property(x => x.ReporterId).HasColumnName("NguoiBaoCaoId");
            e.Property(x => x.Title).HasColumnName("TieuDe").HasMaxLength(250).IsRequired();
            e.Property(x => x.Description).HasColumnName("MoTa").HasMaxLength(4000);
            e.Property(x => x.Category).HasColumnName("LoaiBaoTri");
            e.Property(x => x.Priority).HasColumnName("MucDoUuTien");
            e.Property(x => x.Status).HasColumnName("TrangThai");
            e.Property(x => x.AssignedToId).HasColumnName("NguoiPhuTrachId");
            e.Property(x => x.ResolutionNote).HasColumnName("GhiChuXuLy").HasMaxLength(2000);
            e.Property(x => x.PhotoUrls).HasColumnName("DanhSachAnh").HasMaxLength(2000);
            e.Property(x => x.EstimatedCost).HasColumnName("ChiPhiUocTinh").HasColumnType("decimal(18,0)");
            e.Property(x => x.ActualCost).HasColumnName("ChiPhiThucTe").HasColumnType("decimal(18,0)");
            e.Property(x => x.ChargeToTenant).HasColumnName("TinhPhiKhachThue");
            e.Property(x => x.ReportedAt).HasColumnName("NgayBaoCao");
            e.Property(x => x.AcknowledgedAt).HasColumnName("NgayTiepNhan");
            e.Property(x => x.ResolvedAt).HasColumnName("NgayGiaiQuyet");
            e.Property(x => x.ClosedAt).HasColumnName("NgayDongLai");

            e.HasIndex(x => x.RequestNumber).IsUnique();
            e.HasIndex(x => x.ApartmentId);
            e.HasIndex(x => x.LeaseId);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.Priority);
            e.HasIndex(x => x.AssignedToId);

            e.HasOne(x => x.Apartment).WithMany()
             .HasForeignKey(x => x.ApartmentId).OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Lease).WithMany(l => l.MaintenanceRequests)
             .HasForeignKey(x => x.LeaseId).OnDelete(DeleteBehavior.SetNull);

            e.HasOne(x => x.Reporter).WithMany()
             .HasForeignKey(x => x.ReporterId).OnDelete(DeleteBehavior.NoAction);

            e.HasOne(x => x.AssignedTo).WithMany()
             .HasForeignKey(x => x.AssignedToId).OnDelete(DeleteBehavior.NoAction);
        });

        // ================== LeaseInspection ==================
        b.Entity<LeaseInspection>(e =>
        {
            e.ToTable("KiemKeCanHo");
            MapBaseEntityColumns(e);

            e.Property(x => x.LeaseId).HasColumnName("HopDongId");
            e.Property(x => x.Type).HasColumnName("Loai");
            e.Property(x => x.InspectedAt).HasColumnName("NgayKiemKe");
            e.Property(x => x.InspectorId).HasColumnName("NguoiKiemTraId");
            e.Property(x => x.OverallCondition).HasColumnName("TinhTrangChung");
            e.Property(x => x.Summary).HasColumnName("TomTat").HasMaxLength(2000);
            e.Property(x => x.DamageNotes).HasColumnName("GhiChuHuHong").HasMaxLength(2000);
            e.Property(x => x.PhotoUrls).HasColumnName("DanhSachAnh").HasMaxLength(2000);
            e.Property(x => x.DepositDeduction).HasColumnName("KhauTruDatCoc").HasColumnType("decimal(18,0)");
            e.Property(x => x.TenantSigned).HasColumnName("KhachThueDaKy");

            e.HasIndex(x => x.LeaseId);
            e.HasIndex(x => new { x.LeaseId, x.Type });

            e.HasOne(x => x.Lease).WithMany(l => l.Inspections)
             .HasForeignKey(x => x.LeaseId).OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Inspector).WithMany()
             .HasForeignKey(x => x.InspectorId).OnDelete(DeleteBehavior.NoAction);
        });

        // ================== DepositTransaction ==================
        b.Entity<DepositTransaction>(e =>
        {
            e.ToTable("GiaoDichDatCoc");
            MapBaseEntityColumns(e);

            e.Property(x => x.LeaseId).HasColumnName("HopDongId");
            e.Property(x => x.Type).HasColumnName("Loai");
            e.Property(x => x.Amount).HasColumnName("SoTien").HasColumnType("decimal(18,0)");
            e.Property(x => x.Reason).HasColumnName("LyDo").HasMaxLength(500);
            e.Property(x => x.RelatedInspectionId).HasColumnName("KiemKeLienQuanId");
            e.Property(x => x.RelatedRequestId).HasColumnName("YeuCauLienQuanId");
            e.Property(x => x.RecordedAt).HasColumnName("NgayGhiNhan");
            e.Property(x => x.RecordedBy).HasColumnName("NguoiGhiNhan");

            e.HasIndex(x => x.LeaseId);
            e.HasIndex(x => x.Type);
            e.HasIndex(x => x.RecordedAt);

            e.HasOne(x => x.Lease).WithMany(l => l.DepositTransactions)
             .HasForeignKey(x => x.LeaseId).OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.RelatedInspection).WithMany()
             .HasForeignKey(x => x.RelatedInspectionId).OnDelete(DeleteBehavior.NoAction);

            e.HasOne(x => x.RelatedRequest).WithMany()
             .HasForeignKey(x => x.RelatedRequestId).OnDelete(DeleteBehavior.NoAction);
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
            e.ToTable("ChiSoDichVu");
            MapBaseEntityColumns(e);

            e.Property(x => x.LeaseId).HasColumnName("HopDongId");
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

            e.HasIndex(x => new { x.LeaseId, x.UtilityTypeId, x.BillingMonth }).IsUnique();
            e.HasIndex(x => x.BillingMonth);

            e.HasOne(x => x.Lease).WithMany(l => l.UtilityReadings)
             .HasForeignKey(x => x.LeaseId).OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.UtilityType).WithMany(t => t.Readings)
             .HasForeignKey(x => x.UtilityTypeId).OnDelete(DeleteBehavior.Restrict);
        });

        // ================== Invoice ==================
        b.Entity<Invoice>(e =>
        {
            e.ToTable("HoaDon");
            MapBaseEntityColumns(e);

            e.Property(x => x.InvoiceNumber).HasColumnName("SoHoaDon").HasMaxLength(40).IsRequired();
            e.Property(x => x.LeaseId).HasColumnName("HopDongId");
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
            e.HasIndex(x => x.LeaseId);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.DueDate);
            e.HasIndex(x => new { x.LeaseId, x.BillingMonth, x.Kind });

            e.HasOne(x => x.Lease)
             .WithMany(l => l.Invoices)
             .HasForeignKey(x => x.LeaseId)
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

        // ================== Payment ==================
        b.Entity<Payment>(e =>
        {
            e.ToTable("ThanhToan");
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

        // ================== ViewingAppointment ==================
        b.Entity<ViewingAppointment>(e =>
        {
            e.ToTable("LichXemNha");
            MapBaseEntityColumns(e);

            e.Property(x => x.ApartmentId).HasColumnName("CanHoId");
            e.Property(x => x.UserId).HasColumnName("NguoiDungId");
            e.Property(x => x.ContactName).HasColumnName("TenLienHe").HasMaxLength(100).IsRequired();
            e.Property(x => x.ContactPhone).HasColumnName("SoDienThoaiLienHe").HasMaxLength(30).IsRequired();
            e.Property(x => x.ContactEmail).HasColumnName("EmailLienHe").HasMaxLength(200);
            e.Property(x => x.ScheduledDate).HasColumnName("NgayHen");
            e.Property(x => x.SlotHour).HasColumnName("GioHen");
            e.Property(x => x.Note).HasColumnName("GhiChu").HasMaxLength(1000);
            e.Property(x => x.Status).HasColumnName("TrangThai");
            e.Property(x => x.ConfirmedAt).HasColumnName("NgayXacNhan");
            e.Property(x => x.ConfirmedBy).HasColumnName("NguoiXacNhan");
            e.Property(x => x.CancelledAt).HasColumnName("NgayHuy");
            e.Property(x => x.CancelledBy).HasColumnName("NguoiHuy");
            e.Property(x => x.CancellationReason).HasColumnName("LyDoHuy").HasMaxLength(500);

            e.HasIndex(x => x.ApartmentId);
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.ScheduledDate);
            e.HasIndex(x => x.Status);

            e.HasOne(x => x.Apartment)
             .WithMany()
             .HasForeignKey(x => x.ApartmentId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.NoAction);
        });

        // Soft delete global query filter
        ApplySoftDeleteFilter(b);

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
