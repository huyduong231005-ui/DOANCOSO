using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using t.Models.Entities;

namespace t.Data;

public static class SeedData
{
    public const string RoleAdmin = "Admin";
    public const string RoleManager = "Manager";
    public const string RoleTenant = "Tenant";

    // Thợ nhận sự cố theo chuyên môn (xem MaintenanceDispatcher.RoleFor).
    public const string RoleElectrician = "Electrician";
    public const string RolePlumber = "Plumber";
    public const string RoleAcTechnician = "AcTechnician";
    public const string RoleCarpenter = "Carpenter";
    public const string RoleGlazier = "Glazier";

    // Đơn giá tham khảo khu căn hộ tại TP.HCM (2025–2026).
    private const decimal WaterRate = 18_000m;        // đ/m³
    private const decimal MotorbikeFee = 120_000m;    // đ/xe/tháng
    private const decimal CarFee = 1_500_000m;        // đ/xe/tháng

    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        try
        {
            await ctx.Database.MigrateAsync();
        }
        catch (InvalidOperationException)
        {
            await ctx.Database.EnsureCreatedAsync();
        }

        await SeedRolesAndPermissionsAsync(ctx, roleMgr);

        var admin = await SeedAdminAsync(userMgr);
        if (admin != null && !await userMgr.IsInRoleAsync(admin, RoleAdmin))
            await userMgr.AddToRoleAsync(admin, RoleAdmin);

        await SeedMaintenanceStaffAsync(userMgr);

        if (await ctx.Regions.AnyAsync()) return;

        var hcm = new Region { Name = "TP. Hồ Chí Minh", Slug = "tp-ho-chi-minh", ImageUrl = "/img/region-hcm.jpg" };
        ctx.Regions.AddRange(
            new Region { Name = "Hà Nội", Slug = "ha-noi", ImageUrl = "/img/region-hanoi.jpg" },
            new Region { Name = "Đà Nẵng", Slug = "da-nang", ImageUrl = "/img/region-danang.jpg" },
            hcm);
        var water = new UtilityType { Code = "WATER", Name = "Nước", Unit = "m³", BillingMode = UtilityBillingMode.Metered, DefaultRate = WaterRate, Icon = "water_drop" };
        ctx.UtilityTypes.AddRange(water,
            new UtilityType { Code = "ELEC", Name = "Điện", Unit = "kWh", BillingMode = UtilityBillingMode.Metered, DefaultRate = 3_500m, Icon = "bolt" });
        await ctx.SaveChangesAsync();

        var manager = await userMgr.FindByEmailAsync("quanly@luxehaven.vn");

        var grandPark = new Project
        {
            Name = "Luxe Haven Grand Park", Slug = "luxe-haven-grand-park", RegionId = hcm.Id,
            Address = "Đường Nguyễn Xiển, Long Thạnh Mỹ, TP. Thủ Đức, TP. HCM",
            ThumbnailUrl = "/img/hero-2.jpg",
            ShortDescription = "Khu căn hộ 3 toà A, B, C quanh công viên nội khu, Ban quản lý đặt tại sảnh toà A."
        };
        var riverside = new Project
        {
            Name = "Luxe Haven Riverside", Slug = "luxe-haven-riverside", RegionId = hcm.Id,
            Address = "Đường Nguyễn Hữu Cảnh, P.22, Bình Thạnh, TP. HCM",
            ThumbnailUrl = "/img/rent-2.jpg",
            ShortDescription = "Hai toà ven sông R1, R2; căn hộ 1–3 phòng ngủ."
        };
        ctx.Projects.AddRange(grandPark, riverside);
        await ctx.SaveChangesAsync();

        var buildings = new[]
        {
            NewBuilding(grandPark, "A", "Toà A", 20, 8, 15_000m, manager?.Id, "/img/hero-1.jpg"),
            NewBuilding(grandPark, "B", "Toà B", 18, 8, 15_000m, manager?.Id, "/img/hero-3.jpg"),
            NewBuilding(grandPark, "C", "Toà C", 15, 6, 15_000m, manager?.Id, "/img/hero-4.jpg"),
            NewBuilding(riverside, "R1", "Toà R1", 12, 6, 18_000m, manager?.Id, "/img/rent-2.jpg"),
            NewBuilding(riverside, "R2", "Toà R2", 12, 6, 18_000m, manager?.Id, "/img/rent-4.jpg"),
        };
        ctx.Buildings.AddRange(buildings);
        await ctx.SaveChangesAsync();

        var rng = new Random(2026);
        foreach (var b in buildings)
        {
            var floors = Enumerable.Range(1, b.FloorCount)
                .Select(n => new Floor { BuildingId = b.Id, Number = n, Label = $"Tầng {n}" }).ToList();
            ctx.Floors.AddRange(floors);
            await ctx.SaveChangesAsync();

            var perFloor = b.Code.StartsWith('R') || b.Code == "C" ? 6 : 8;
            foreach (var f in floors)
                for (var pos = 1; pos <= perFloor; pos++)
                    ctx.Apartments.Add(NewApartment(b, f, pos, perFloor, rng));
        }
        await ctx.SaveChangesAsync();

        var apartments = await ctx.Apartments.Include(a => a.Building).OrderBy(a => a.Id).ToListAsync();
        SeedResidentsAndCharges(ctx, apartments, rng);
        await ctx.SaveChangesAsync();

        await SeedResidentAccountsAsync(ctx, userMgr, apartments);
        SeedBillingHistory(ctx, apartments, water, rng);
        SeedMaintenance(ctx, apartments);
        await ctx.SaveChangesAsync();
    }

    private static Building NewBuilding(Project p, string code, string name, int floors, int perFloor,
                                        decimal feePerM2, string? managerId, string thumb) => new()
    {
        Name = name, Code = code, Slug = $"{p.Slug}-{code.ToLowerInvariant()}",
        ProjectId = p.Id, RegionId = p.RegionId, Address = p.Address,
        FloorCount = floors, ThumbnailUrl = thumb, ManagerId = managerId,
        Description = $"{name} · {floors} tầng · {perFloor} căn/tầng.",
        Status = BuildingStatus.Active,
        DefaultBillingDay = 1, DefaultLateFeeAfterDays = 15, DefaultLateFeePercent = 2,
        ManagementFeePerM2 = feePerM2
    };

    // Vị trí căn trên tầng quyết định loại căn: 2 căn góc đầu là 3PN, 2 căn cuối 1PN/studio, giữa là 2PN.
    private static Apartment NewApartment(Building b, Floor f, int pos, int perFloor, Random rng)
    {
        var (area, bed, bath) = pos <= 2 ? (95.5, 3, 2)
            : pos > perFloor - 1 ? (35.2, 1, 1)
            : pos > perFloor - 2 ? (52.4, 1, 1)
            : (69.8, 2, 2);
        var roll = rng.NextDouble();
        var occupancy = f.Number >= b.FloorCount - 1 && roll < 0.6 ? ApartmentOccupancy.NotHandedOver
            : roll < 0.78 ? ApartmentOccupancy.Occupied
            : roll < 0.95 ? ApartmentOccupancy.Vacant
            : ApartmentOccupancy.UnderMaintenance;
        return new Apartment
        {
            BuildingId = b.Id, FloorId = f.Id,
            UnitCode = $"{b.Code}-{f.Number:00}.{pos:00}",
            Area = area, Bedrooms = bed, Bathrooms = bath,
            HouseDirection = (HouseDirection)((pos + f.Number) % 8),
            Occupancy = occupancy
        };
    }

    private static readonly string[] LastNames = { "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ", "Đặng", "Bùi", "Đỗ", "Ngô", "Dương", "Lý" };
    private static readonly string[] MiddleNames = { "Văn", "Thị", "Minh", "Ngọc", "Hữu", "Thanh", "Quốc", "Gia", "Thu", "Đức", "Hoài", "Khánh" };
    private static readonly string[] FirstNames = { "An", "Bình", "Châu", "Dũng", "Giang", "Hà", "Hải", "Hiếu", "Hùng", "Khoa", "Lan", "Linh", "Long", "Mai", "Nam", "Ngân", "Phúc", "Quân", "Sơn", "Tâm", "Thảo", "Trang", "Tuấn", "Vy", "Yến" };

    private static string RandomName(Random rng) =>
        $"{LastNames[rng.Next(LastNames.Length)]} {MiddleNames[rng.Next(MiddleNames.Length)]} {FirstNames[rng.Next(FirstNames.Length)]}";

    private static string RandomPhone(Random rng) => $"09{rng.Next(0, 10)} {rng.Next(100, 1000)} {rng.Next(1000, 10000)}";

    private static void SeedResidentsAndCharges(AppDbContext ctx, List<Apartment> apartments, Random rng)
    {
        var today = DateTime.UtcNow.Date;
        foreach (var a in apartments.Where(a => a.Occupancy == ApartmentOccupancy.Occupied))
        {
            var rented = rng.NextDouble() < 0.25;
            var moveIn = today.AddDays(-rng.Next(60, 1500));
            var head = RandomName(rng);
            ctx.ApartmentResidents.Add(new ApartmentResident
            {
                ApartmentId = a.Id, FullName = head, Phone = RandomPhone(rng),
                Role = rented ? ResidentRole.Tenant : ResidentRole.Owner, IsPrimary = true,
                MoveInDate = moveIn, IdNumber = $"0{rng.Next(10, 99)}0{rng.Next(10_000_000, 99_999_999)}"
            });
            for (var m = rng.Next(0, a.Bedrooms + 1); m > 0; m--)
                ctx.ApartmentResidents.Add(new ApartmentResident
                {
                    ApartmentId = a.Id, FullName = RandomName(rng), Role = ResidentRole.Member, MoveInDate = moveIn
                });

            var bikes = rng.Next(0, 3);
            var sort = 0;
            for (var i = 1; i <= bikes; i++)
                ctx.RecurringCharges.Add(new RecurringCharge { ApartmentId = a.Id, Description = $"Gửi xe máy #{i}", Amount = MotorbikeFee, SortOrder = sort++ });
            if (a.Bedrooms >= 2 && rng.NextDouble() < 0.3)
                ctx.RecurringCharges.Add(new RecurringCharge { ApartmentId = a.Id, Description = "Gửi ô tô", Amount = CarFee, SortOrder = sort });
        }
    }

    // Tài khoản cổng cư dân mẫu, gắn vào căn cụ thể để đăng nhập thử.
    private static async Task SeedResidentAccountsAsync(AppDbContext ctx, UserManager<AppUser> userMgr, List<Apartment> apartments)
    {
        var accounts = new (string Email, string Name, string Phone, string Unit, ResidentRole Role)[]
        {
            ("renter@luxehaven.vn", "Nguyễn Minh Quân", "091 234 5678", "A-12.05", ResidentRole.Owner),
            ("renter2@luxehaven.vn", "Lê Thu Hà", "092 345 6789", "B-08.03", ResidentRole.Tenant),
            ("renter3@luxehaven.vn", "Trần Văn Hùng", "093 456 7890", "A-15.01", ResidentRole.Owner),
            ("renter4@luxehaven.vn", "Phạm Thị Mai", "094 567 8901", "R1-06.02", ResidentRole.Owner),
        };
        foreach (var (email, name, phone, unit, role) in accounts)
        {
            var user = new AppUser { UserName = email, Email = email, FullName = name, Phone = phone, EmailConfirmed = true };
            if (!(await userMgr.CreateAsync(user, "Renter@123")).Succeeded) continue;
            await userMgr.AddToRoleAsync(user, RoleTenant);

            var apt = apartments.First(a => a.UnitCode == unit);
            var existing = await ctx.ApartmentResidents.Where(r => r.ApartmentId == apt.Id).ToListAsync();
            existing.ForEach(r => r.IsPrimary = false);
            if (existing.Count == 0)
                ctx.RecurringCharges.Add(new RecurringCharge { ApartmentId = apt.Id, Description = "Gửi xe máy #1", Amount = MotorbikeFee });
            apt.Occupancy = ApartmentOccupancy.Occupied;
            ctx.ApartmentResidents.Add(new ApartmentResident
            {
                ApartmentId = apt.Id, FullName = name, Phone = phone, Email = email, UserId = user.Id,
                Role = role, IsPrimary = true, MoveInDate = DateTime.UtcNow.Date.AddMonths(-14)
            });
        }
        await ctx.SaveChangesAsync();
    }

    /// <summary>
    /// 3 kỳ phí gần nhất cho mọi căn đang ở: phí quản lý + nước + gửi xe.
    /// Hai kỳ trước phần lớn đã thanh toán (một ít còn nợ quá hạn), kỳ này một phần đã nộp.
    /// </summary>
    private static void SeedBillingHistory(AppDbContext ctx, List<Apartment> apartments, UtilityType water, Random rng)
    {
        var today = DateTime.UtcNow.Date;
        var charges = ctx.RecurringCharges.Local.Where(c => c.IsActive).ToLookup(c => c.ApartmentId);
        var occupied = apartments.Where(a => a.Occupancy == ApartmentOccupancy.Occupied).ToList();
        var invoiceSeq = new Dictionary<int, int>();
        var paymentSeq = 0;
        var meter = occupied.ToDictionary(a => a.Id, _ => (decimal)rng.Next(80, 400));

        for (var offset = -2; offset <= 0; offset++)
        {
            var month = new DateTime(today.Year, today.Month, 1).AddMonths(offset);
            var billingMonth = month.Year * 100 + month.Month;
            foreach (var a in occupied)
            {
                var used = (decimal)rng.Next(6, 10 + a.Bedrooms * 4);
                var reading = new UtilityReading
                {
                    ApartmentId = a.Id, UtilityTypeId = water.Id, BillingMonth = billingMonth,
                    PreviousReading = meter[a.Id], CurrentReading = meter[a.Id] + used,
                    Consumption = used, Rate = WaterRate, Amount = used * WaterRate,
                    ReadAt = month.AddDays(-1), Billed = true
                };
                meter[a.Id] += used;
                ctx.UtilityReadings.Add(reading);

                var area = (decimal)a.Area;
                var items = new List<InvoiceItem>
                {
                    new() { Description = $"Phí quản lý {a.UnitCode} – {month:MM/yyyy} ({area:0.##} m²)", Quantity = area,
                            UnitPrice = a.Building.ManagementFeePerM2, LineTotal = Math.Round(area * a.Building.ManagementFeePerM2, 0), SortOrder = 0 },
                    new() { Description = $"Nước (m³) – {used:0.##}", Quantity = used, UnitPrice = WaterRate, LineTotal = reading.Amount, SortOrder = 1 }
                };
                var sort = 2;
                foreach (var c in charges[a.Id])
                    items.Add(new InvoiceItem { Description = c.Description, Quantity = 1m, UnitPrice = c.Amount, LineTotal = c.Amount, SortOrder = sort++ });

                var total = items.Sum(i => i.LineTotal);
                invoiceSeq[billingMonth] = invoiceSeq.GetValueOrDefault(billingMonth) + 1;
                var due = month.AddDays(a.Building.DefaultBillingDay + 6);
                var invoice = new Invoice
                {
                    InvoiceNumber = $"INV-{billingMonth}-{invoiceSeq[billingMonth]:0000}",
                    ApartmentId = a.Id, Kind = InvoiceKind.Monthly, BillingMonth = billingMonth, IsRecurring = true,
                    IssueDate = month, DueDate = due, SubTotal = total, Total = total, Balance = total,
                    Status = InvoiceStatus.Issued, Currency = "VND", Items = items
                };

                var roll = rng.NextDouble();
                var paid = offset < 0 ? roll < 0.9 : roll < 0.55;
                if (paid)
                {
                    var paidAt = due.AddDays(-rng.Next(0, 6));
                    if (paidAt > today) paidAt = today;
                    paymentSeq++;
                    invoice.Payments.Add(new Payment
                    {
                        PaymentNumber = $"PMT-{paidAt:yyyyMMdd}-{paymentSeq:0000}", Amount = total,
                        Method = rng.NextDouble() < 0.8 ? PaymentMethod.BankTransfer : PaymentMethod.Cash,
                        Status = PaymentStatus.Succeeded, PaidAt = paidAt, Currency = "VND"
                    });
                    invoice.AmountPaid = total;
                    invoice.Balance = 0;
                    invoice.Status = InvoiceStatus.Paid;
                }
                else if (due < today && offset < 0)
                {
                    invoice.Status = InvoiceStatus.Overdue;
                }
                else if (due < today)
                {
                    // Kỳ hiện tại chưa nộp: lùi hạn để dịch vụ nền không gắn quá hạn ngay khi chạy demo.
                    invoice.DueDate = today.AddDays(5);
                }
                ctx.Invoices.Add(invoice);
            }
        }
    }

    private static void SeedMaintenance(AppDbContext ctx, List<Apartment> apartments)
    {
        var now = DateTime.UtcNow;
        var samples = new (string Unit, string Title, string Desc, MaintenanceCategory Cat, MaintenancePriority Pri, MaintenanceStatus Status, int DaysAgo)[]
        {
            ("A-12.05", "Vòi nước phòng tắm bị rò rỉ", "Vòi nước nóng ở phòng tắm chính rò nước, nhỏ giọt liên tục cả khi đã đóng.", MaintenanceCategory.Plumbing, MaintenancePriority.Medium, MaintenanceStatus.Open, 2),
            ("B-08.03", "Mất điện ổ cắm phòng khách", "Hai ổ cắm phòng khách không có điện, aptomat không nhảy.", MaintenanceCategory.Electrical, MaintenancePriority.High, MaintenanceStatus.InProgress, 1),
            ("A-07.02", "Thấm trần nhà vệ sinh", "Trần nhà vệ sinh bị ố vàng, nghi thấm từ căn tầng trên.", MaintenanceCategory.Structural, MaintenancePriority.Medium, MaintenanceStatus.Acknowledged, 4),
            ("C-03.04", "Máy lạnh phòng ngủ chảy nước", "Dàn lạnh phòng ngủ nhỏ chảy nước xuống tường.", MaintenanceCategory.Appliance, MaintenancePriority.Low, MaintenanceStatus.Resolved, 9),
            ("R1-06.02", "Kẹt cửa kính ban công", "Cửa lùa ban công kẹt ray, không đóng kín được.", MaintenanceCategory.Other, MaintenancePriority.Medium, MaintenanceStatus.Open, 0),
        };
        var seq = 0;
        foreach (var (unit, title, desc, cat, pri, status, daysAgo) in samples)
        {
            var apt = apartments.FirstOrDefault(a => a.UnitCode == unit);
            if (apt == null) continue;
            seq++;
            var reported = now.AddDays(-daysAgo);
            var sla = SlaPolicy.Defaults.Single(p => p.Priority == pri);
            var req = new MaintenanceRequest
            {
                RequestNumber = $"MR-{reported:yyyyMM}-{seq:0000}",
                ApartmentId = apt.Id, Title = title, Description = desc,
                Category = cat, Priority = pri, Status = status, ReportedAt = reported,
                AcknowledgedAt = status == MaintenanceStatus.Open ? null : reported.AddHours(2),
                ResolvedAt = status == MaintenanceStatus.Resolved ? reported.AddDays(1) : null,
                EstimatedCost = 300_000m,
                DueAt = reported.AddMinutes(sla.ResolveMinutes)
            };
            req.History.Add(new MaintenanceStatusChange { FromStatus = null, ToStatus = MaintenanceStatus.Open, ChangedAt = reported, Note = "Tạo yêu cầu" });
            if (status != MaintenanceStatus.Open)
                req.History.Add(new MaintenanceStatusChange { FromStatus = MaintenanceStatus.Open, ToStatus = status, ChangedAt = reported.AddHours(2), Note = "Dữ liệu mẫu" });
            ctx.MaintenanceRequests.Add(req);
        }
    }

    private static async Task SeedRolesAndPermissionsAsync(AppDbContext ctx, RoleManager<IdentityRole> roleMgr)
    {
        foreach (var role in new[] { RoleAdmin, RoleManager, RoleTenant,
                     RoleElectrician, RolePlumber, RoleAcTechnician, RoleCarpenter, RoleGlazier })
        {
            if (!await roleMgr.RoleExistsAsync(role))
                await roleMgr.CreateAsync(new IdentityRole(role));
        }

        if (await ctx.Permissions.AnyAsync()) return;

        var permissions = new (string Module, string Code, string Name)[]
        {
            ("Projects",   "projects.view",     "Xem khu & toà nhà"),
            ("Projects",   "projects.manage",   "Quản lý khu & toà nhà"),
            ("Apartments", "apartments.view",   "Xem căn hộ"),
            ("Apartments", "apartments.update", "Cập nhật căn hộ"),
            ("Residents",  "residents.view",    "Xem cư dân"),
            ("Residents",  "residents.manage",  "Đăng ký / chuyển đi cư dân"),
            ("Invoices",   "invoices.view",     "Xem hoá đơn"),
            ("Invoices",   "invoices.issue",    "Phát hành hoá đơn"),
            ("Payments",   "payments.view",     "Xem thanh toán"),
            ("Payments",   "payments.refund",   "Hoàn tiền"),
            ("Maintenance","maintenance.manage","Xử lý sự cố"),
            ("Users",      "users.view",        "Xem người dùng"),
            ("Users",      "users.manage",      "Quản lý người dùng"),
            ("Roles",      "roles.manage",      "Quản lý phân quyền"),
            ("Audit",      "audit.view",        "Xem nhật ký"),
        };

        foreach (var (module, code, name) in permissions)
            ctx.Permissions.Add(new Permission { Module = module, Code = code, DisplayName = name });

        await ctx.SaveChangesAsync();

        var allPerms = await ctx.Permissions.ToListAsync();
        var adminRole = await roleMgr.FindByNameAsync(RoleAdmin);
        var managerRole = await roleMgr.FindByNameAsync(RoleManager);

        if (adminRole != null)
            foreach (var p in allPerms)
                ctx.RolePermissions.Add(new RolePermission { RoleId = adminRole.Id, PermissionId = p.Id });

        if (managerRole != null)
            foreach (var p in allPerms.Where(p => p.Code != "roles.manage" && p.Code != "users.manage"))
                ctx.RolePermissions.Add(new RolePermission { RoleId = managerRole.Id, PermissionId = p.Id });

        await ctx.SaveChangesAsync();
    }

    // Quản lý và thợ mẫu cho luồng báo sự cố, mật khẩu demo theo vai trò (Quanly@123 / Tho@123).
    private const string ManagerDemoPassword = "Quanly@123";
    private const string TechnicianDemoPassword = "Tho@123";

    private static async Task SeedMaintenanceStaffAsync(UserManager<AppUser> userMgr)
    {
        var staff = new (string Email, string Name, string Role)[]
        {
            ("quanly@luxehaven.vn", "Trần Thị Mai (Quản lý tòa nhà)", RoleManager),
            ("tho.dien@luxehaven.vn", "Lê Văn Hùng (Thợ điện)", RoleElectrician),
            ("tho.nuoc@luxehaven.vn", "Phạm Văn Tâm (Thợ nước)", RolePlumber),
            ("tho.maylanh@luxehaven.vn", "Nguyễn Văn Sơn (Thợ máy lạnh)", RoleAcTechnician),
            ("tho.moc@luxehaven.vn", "Võ Văn Phúc (Thợ mộc)", RoleCarpenter),
            ("tho.nhomkinh@luxehaven.vn", "Đặng Văn Khoa (Thợ nhôm kính)", RoleGlazier),
        };
        foreach (var (email, name, role) in staff)
        {
            var password = role == RoleManager ? ManagerDemoPassword : TechnicianDemoPassword;
            var user = await userMgr.FindByEmailAsync(email);
            if (user != null)
            {
                // Tài khoản tạo từ bản cũ không có mật khẩu: bổ sung để đăng nhập được cổng kỹ thuật viên.
                if (!await userMgr.HasPasswordAsync(user)) await userMgr.AddPasswordAsync(user, password);
                continue;
            }
            user = new AppUser { UserName = email, Email = email, FullName = name, EmailConfirmed = true };
            if ((await userMgr.CreateAsync(user, password)).Succeeded) await userMgr.AddToRoleAsync(user, role);
        }
    }

    private static async Task<AppUser?> SeedAdminAsync(UserManager<AppUser> userMgr)
    {
        var existing = await userMgr.FindByEmailAsync("admin@luxehaven.vn");
        if (existing != null) return existing;

        var admin = new AppUser
        {
            UserName = "admin@luxehaven.vn", Email = "admin@luxehaven.vn",
            FullName = "Quản trị viên", Phone = "098 000 0001",
            EmailConfirmed = true
        };
        var result = await userMgr.CreateAsync(admin, "Admin@123");
        return result.Succeeded ? admin : null;
    }
}
