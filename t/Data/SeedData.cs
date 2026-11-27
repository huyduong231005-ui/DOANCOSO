using t.Infrastructure.Residents;
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
    private const decimal ManagementFeePerM2 = 15_000m; // đ/m²/tháng, chung mọi toà

    private static UtilityType TypeOf(AppDbContext ctx, string code) => ctx.UtilityTypes.Local.First(t => t.Code == code);

    // Dữ liệu mẫu dùng các tập tĩnh (FloorsOf, OccupiedUnits): nhiều CSDL test seed song song thì phải chạy lần lượt.
    private static readonly SemaphoreSlim SeedLock = new(1, 1);

    public static async Task InitializeAsync(IServiceProvider services)
    {
        await SeedLock.WaitAsync();
        try { await InitializeCoreAsync(services); }
        finally { SeedLock.Release(); }
    }

    private static async Task InitializeCoreAsync(IServiceProvider services)
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

        if (await ctx.Projects.AnyAsync()) return;

        // Bảng giá: mọi khoản thu là một loại dịch vụ. Migration đã tạo sẵn phí quản lý, gửi xe, sửa chữa;
        // loại nào đã có thì dùng lại, chưa có mới thêm.
        var types = await ctx.UtilityTypes.ToListAsync();
        foreach (var t in new[]
                 {
                     new UtilityType { Code = UtilityType.ManagementCode, Name = "Phí quản lý", Unit = "m²", DefaultRate = ManagementFeePerM2, Method = BillingMethod.ByArea },
                     new UtilityType { Code = "NUOC", Name = "Nước", Unit = "m³", DefaultRate = WaterRate, Method = BillingMethod.ByMeter },
                     new UtilityType { Code = "DIEN", Name = "Điện", Unit = "kWh", DefaultRate = 3_500m, Method = BillingMethod.ByMeter },
                     new UtilityType { Code = "XEMAY", Name = "Gửi xe máy", Unit = "xe", DefaultRate = MotorbikeFee, Method = BillingMethod.BySubscription },
                     new UtilityType { Code = "OTO", Name = "Gửi ô tô", Unit = "xe", DefaultRate = CarFee, Method = BillingMethod.BySubscription },
                     new UtilityType { Code = UtilityType.RepairCode, Name = "Sửa chữa", Unit = "lần", DefaultRate = 0, Method = BillingMethod.PerUse }
                 }.Where(t => types.All(x => x.Code != t.Code)))
            ctx.UtilityTypes.Add(t);
        var water = TypeOf(ctx, "NUOC");
        await ctx.SaveChangesAsync();


        var grandPark = new Project
        {
            Name = "Luxe Haven Grand Park",
            Address = "Đường Nguyễn Xiển, Long Thạnh Mỹ, TP. Thủ Đức, TP. HCM"
        };
        var riverside = new Project
        {
            Name = "Luxe Haven Riverside",
            Address = "Đường Nguyễn Hữu Cảnh, P.22, Bình Thạnh, TP. HCM"
        };
        ctx.Projects.AddRange(grandPark, riverside);
        await ctx.SaveChangesAsync();

        var buildings = new[]
        {
            NewBuilding(grandPark, "A", 20, 8),
            NewBuilding(grandPark, "B", 18, 8),
            NewBuilding(grandPark, "C", 15, 6),
            NewBuilding(riverside, "R1", 12, 6),
            NewBuilding(riverside, "R2", 12, 6),
        };
        ctx.Buildings.AddRange(buildings);
        await ctx.SaveChangesAsync();

        var rng = new Random(2026);
        foreach (var b in buildings)
        {
            var perFloor = b.Code.StartsWith('R') || b.Code == "C" ? 6 : 8;
            for (var f = 1; f <= FloorsOf[b.Code]; f++)
                for (var pos = 1; pos <= perFloor; pos++)
                    ctx.Apartments.Add(NewApartment(b, f, pos, perFloor, rng));
        }
        await ctx.SaveChangesAsync();

        var apartments = await ctx.Apartments.Include(a => a.Building).OrderBy(a => a.Id).ToListAsync();
        SeedResidentsAndCharges(ctx, apartments, rng);
        await ctx.SaveChangesAsync();

        await SeedResidentAccountsAsync(ctx, userMgr, apartments);
        SeedBillingHistory(ctx, apartments, water, rng);
        await SeedMaintenanceAsync(ctx, apartments);
        await ctx.SaveChangesAsync();
    }

    // Căn mẫu sẽ có người ở (CuTru). "Đang ở" không lưu ở căn nên seed tự nhớ danh sách này.
    private static readonly HashSet<string> OccupiedUnits = new();

    // Số tầng của toà mẫu (toà không lưu số tầng; mỗi căn ghi số tầng của mình).
    private static readonly Dictionary<string, int> FloorsOf = new();

    private static Building NewBuilding(Project p, string code, int floors, int perFloor)
    {
        FloorsOf[code] = floors;
        return NewBuildingCore(p, code);
    }

    private static Building NewBuildingCore(Project p, string code) => new()
    {
        Code = code,
        ProjectId = p.Id,
        Status = BuildingStatus.Active,
        DefaultBillingDay = 1, DefaultLateFeeAfterDays = 15, DefaultLateFeePercent = 2
    };

    // Vị trí căn trên tầng quyết định loại căn: 2 căn góc đầu là 3PN, 2 căn cuối 1PN/studio, giữa là 2PN.
    private static Apartment NewApartment(Building b, int floor, int pos, int perFloor, Random rng)
    {
        var (area, bed) = pos <= 2 ? (95.5m, 3)
            : pos > perFloor - 1 ? (35.2m, 1)
            : pos > perFloor - 2 ? (52.4m, 1)
            : (69.8m, 2);
        var roll = rng.NextDouble();
        var unitCode = $"{b.Code}-{floor:00}.{pos:00}";
        var condition = floor >= FloorsOf[b.Code] - 1 && roll < 0.6 ? ApartmentCondition.NotHandedOver
            : roll < 0.95 ? ApartmentCondition.HandedOver
            : ApartmentCondition.UnderMaintenance;
        if (condition == ApartmentCondition.HandedOver && roll < 0.78) OccupiedUnits.Add(unitCode);
        return new Apartment
        {
            BuildingId = b.Id, FloorNumber = floor,
            UnitCode = unitCode,
            Area = area, Bedrooms = bed,
            Condition = condition
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
        foreach (var a in apartments.Where(a => OccupiedUnits.Contains(a.UnitCode)))
        {
            var rented = rng.NextDouble() < 0.25;
            var moveIn = today.AddDays(-rng.Next(60, 1500));
            var head = RandomName(rng);
            ctx.ApartmentResidents.Add(new ApartmentResident
            {
                ApartmentId = a.Id,
                User = PendingAccounts.New(head, RandomPhone(rng), null, $"0{rng.Next(10, 99)}0{rng.Next(10_000_000, 99_999_999)}"),
                Role = rented ? ResidentRole.Tenant : ResidentRole.Owner, IsPrimary = true,
                MoveInDate = moveIn
            });
            for (var m = rng.Next(0, a.Bedrooms + 1); m > 0; m--)
                ctx.ApartmentResidents.Add(new ApartmentResident
                {
                    ApartmentId = a.Id, User = PendingAccounts.New(RandomName(rng), null, null, null),
                    Role = ResidentRole.Member, MoveInDate = moveIn
                });

            var bikes = rng.Next(0, 3);
            if (bikes > 0)
                ctx.RecurringCharges.Add(new RecurringCharge { ApartmentId = a.Id, UtilityTypeId = TypeOf(ctx, "XEMAY").Id, Quantity = bikes });
            if (a.Bedrooms >= 2 && rng.NextDouble() < 0.3)
                ctx.RecurringCharges.Add(new RecurringCharge { ApartmentId = a.Id, UtilityTypeId = TypeOf(ctx, "OTO").Id, Quantity = 1 });
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
            var user = new AppUser { UserName = email, Email = email, FullName = name, PhoneNumber = phone, EmailConfirmed = true };
            if (!(await userMgr.CreateAsync(user, "Renter@123")).Succeeded) continue;
            await userMgr.AddToRoleAsync(user, RoleTenant);

            var apt = apartments.First(a => a.UnitCode == unit);
            var existing = await ctx.ApartmentResidents.Where(r => r.ApartmentId == apt.Id).ToListAsync();
            existing.ForEach(r => r.IsPrimary = false);
            if (existing.Count == 0)
                ctx.RecurringCharges.Add(new RecurringCharge { ApartmentId = apt.Id, UtilityTypeId = TypeOf(ctx, "XEMAY").Id, Quantity = 1 });
            apt.Condition = ApartmentCondition.HandedOver;
            OccupiedUnits.Add(apt.UnitCode);
            ctx.ApartmentResidents.Add(new ApartmentResident
            {
                ApartmentId = apt.Id, UserId = user.Id, ConfirmedAt = DateTime.UtcNow.Date.AddMonths(-14),
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
        var charges = ctx.RecurringCharges.Local.ToLookup(c => c.ApartmentId);
        var fee = TypeOf(ctx, UtilityType.ManagementCode);
        var occupied = apartments.Where(a => OccupiedUnits.Contains(a.UnitCode)).ToList();
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
                    ReadAt = month.AddDays(-1)
                };
                meter[a.Id] += used;
                ctx.UtilityReadings.Add(reading);

                var area = (decimal)a.Area;
                var items = new List<InvoiceItem>
                {
                    new() { UtilityTypeId = fee.Id, Quantity = area, UnitPrice = fee.DefaultRate, LineTotal = Math.Round(area * fee.DefaultRate, 0) },
                    new() { UtilityTypeId = water.Id, Quantity = used, UnitPrice = WaterRate, LineTotal = used * WaterRate }
                };
                foreach (var c in charges[a.Id])
                {
                    var rate = ctx.UtilityTypes.Local.First(t => t.Id == c.UtilityTypeId).DefaultRate;
                    items.Add(new InvoiceItem { UtilityTypeId = c.UtilityTypeId, Quantity = c.Quantity, UnitPrice = rate, LineTotal = c.Quantity * rate });
                }

                var total = items.Sum(i => i.LineTotal);
                invoiceSeq[billingMonth] = invoiceSeq.GetValueOrDefault(billingMonth) + 1;
                var due = month.AddDays(a.Building.DefaultBillingDay + 6);
                var invoice = new Invoice
                {
                    InvoiceNumber = $"HD-{billingMonth}-{invoiceSeq[billingMonth]:0000}",
                    ApartmentId = a.Id, Kind = InvoiceKind.Monthly, BillingMonth = billingMonth,
                    IssueDate = month, DueDate = due, SubTotal = total, Total = total, Balance = total,
                    Status = InvoiceStatus.Issued, Items = items
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
                        PaymentNumber = $"PT-{paidAt:yyyyMMdd}-{paymentSeq:0000}", Amount = total,
                        Method = rng.NextDouble() < 0.8 ? PaymentMethod.BankTransfer : PaymentMethod.Cash,
                        Status = PaymentStatus.Succeeded, PaidAt = paidAt
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

    private static async Task SeedMaintenanceAsync(AppDbContext ctx, List<Apartment> apartments)
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
            // Người báo là người đang cư trú tại căn (ưu tiên người đại diện); căn trống thì bỏ mẫu này.
            var reporterId = await ctx.ApartmentResidents.Where(r => r.ApartmentId == (apt == null ? 0 : apt.Id) && r.MoveOutDate == null)
                .OrderByDescending(r => r.IsPrimary).Select(r => r.UserId).FirstOrDefaultAsync();
            if (apt == null || reporterId == null) continue;
            seq++;
            var reported = now.AddDays(-daysAgo);
            var sla = SlaPolicy.Defaults.Single(p => p.Priority == pri);
            var req = new MaintenanceRequest
            {
                RequestNumber = $"YC-{reported:yyyyMM}-{seq:0000}",
                ApartmentId = apt.Id, ReporterId = reporterId, Title = title, Description = desc,
                Category = cat, Priority = pri, Status = status, ReportedAt = reported,
                DueAt = reported.AddMinutes(sla.ResolveMinutes),
                // Đã xử lý thì phải có kết quả (CK_YeuCauBaoTri_KetQua).
                ResolutionNote = status == MaintenanceStatus.Resolved ? "Đã thay gioăng, vệ sinh ống thoát nước dàn lạnh." : null
            };
            req.History.Add(new MaintenanceStatusChange { ToStatus = MaintenanceStatus.Open, ChangedAt = reported, Note = "Tạo yêu cầu" });
            // Lịch sử đi đúng bảng chuyển trạng thái: Mới gửi → Đã tiếp nhận → Đang xử lý → Đã xử lý.
            var path = new[] { MaintenanceStatus.Open, MaintenanceStatus.Acknowledged, MaintenanceStatus.InProgress, MaintenanceStatus.Resolved };
            for (var k = 1; k < path.Length && path[k - 1] != status; k++)
                req.History.Add(new MaintenanceStatusChange { ToStatus = path[k], ChangedAt = reported.AddHours(k * 2), Note = "Dữ liệu mẫu" });
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
            ("quanly@luxehaven.vn", "Trần Thị Mai (Quản lý toà nhà)", RoleManager),
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
            FullName = "Quản trị viên", PhoneNumber = "098 000 0001",
            EmailConfirmed = true
        };
        var result = await userMgr.CreateAsync(admin, "Admin@123");
        return result.Succeeded ? admin : null;
    }
}
