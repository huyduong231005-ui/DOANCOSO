using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.WebEncoders;
using t.Application.Commands.Auth;
using t.Application.Commands.Favorites;
using t.Application.Commands.Listings;
using t.Application.Queries.Auth;
using t.Application.Queries.Projects;
using t.Application.Queries.Rentals;
using t.Data;
using t.Infrastructure.Audit;
using t.Infrastructure.Billing;
using t.Infrastructure.Email;
using t.Infrastructure.Pdf;
using t.Infrastructure.Security;
using t.Infrastructure.Storage;
using t.Models.Entities;

// PostgreSQL: cho phép DateTime.Kind=Unspecified hoặc Local map vào "timestamp with time zone"
// như hành vi EF Core 5/Npgsql 5 cũ. Tránh phải sửa hàng loạt DateTime → UTC trong code.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Render (và đa số cloud) gán PORT qua biến môi trường. App phải bind vào port đó.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Khi chạy sau reverse proxy (Render/Railway/Cloudflare), tin các header X-Forwarded-*
// để biết request gốc là HTTPS, IP thật của khách...
builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                       | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

// On Windows, the host adds an EventLog logger provider by default. It throws
// ObjectDisposedException during shutdown if any logger writes after the provider
// has been disposed (a noisy red "BackgroundService failed" stack trace that has
// nothing to do with the actual app). Drop it — Console + Debug are enough for dev.
builder.Logging.AddFilter("Microsoft.Extensions.Logging.EventLog.EventLogLoggerProvider", LogLevel.None);

// ── Database ──
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Identity ──
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Home/Login";
    options.LogoutPath = "/Home/Logout";
    options.AccessDeniedPath = "/Home/Login";
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.Configure<WebEncoderOptions>(opt =>
    opt.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

builder.Services.AddSingleton<Microsoft.AspNetCore.Mvc.DataAnnotations.IValidationAttributeAdapterProvider,
                              t.Infrastructure.Localization.VietnameseValidationAttributeAdapterProvider>();
builder.Services.AddControllersWithViews(options =>
{
    options.ModelMetadataDetailsProviders.Add(new t.Infrastructure.Localization.VietnameseValidationMessagesProvider());

    var msg = options.ModelBindingMessageProvider;
    msg.SetMissingBindRequiredValueAccessor(name => $"Thiếu giá trị cho '{name}'.");
    msg.SetMissingKeyOrValueAccessor(() => "Giá trị bắt buộc.");
    msg.SetMissingRequestBodyRequiredValueAccessor(() => "Yêu cầu phải có nội dung.");
    msg.SetValueMustNotBeNullAccessor(value => $"Giá trị '{value}' không hợp lệ.");
    msg.SetAttemptedValueIsInvalidAccessor((value, name) => $"Giá trị '{value}' không hợp lệ cho trường '{name}'.");
    msg.SetUnknownValueIsInvalidAccessor(name => $"Giá trị không hợp lệ cho trường '{name}'.");
    msg.SetValueIsInvalidAccessor(value => $"Giá trị '{value}' không hợp lệ.");
    msg.SetValueMustBeANumberAccessor(name => $"Trường '{name}' phải là số.");
    msg.SetNonPropertyAttemptedValueIsInvalidAccessor(value => $"Giá trị '{value}' không hợp lệ.");
    msg.SetNonPropertyUnknownValueIsInvalidAccessor(() => "Giá trị không hợp lệ.");
    msg.SetNonPropertyValueMustBeANumberAccessor(() => "Giá trị phải là số.");
});
builder.Services.AddScoped<CreateListingCommandHandler>();
builder.Services.AddScoped<AuthCommandHandler>();
builder.Services.AddScoped<SetFavoriteCommandHandler>();
builder.Services.AddScoped<AuthQueryHandler>();
builder.Services.AddScoped<ProjectsQueryHandler>();
builder.Services.AddScoped<RentalsQueryHandler>();
builder.Services.AddSingleton<IDevResetTokenStore, InMemoryDevResetTokenStore>();
builder.Services.AddScoped<InvoiceGenerator>();
builder.Services.AddHostedService<InvoiceGenerationService>();
builder.Services.AddHostedService<LeaseLifecycleService>();
builder.Services.AddScoped<FileStorageService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<t.Infrastructure.Payments.VietQrService>();
builder.Services.AddScoped<InvoicePdfGenerator>();
builder.Services.AddScoped<LeaseContractPdfGenerator>();
builder.Services.AddSingleton<IEmailSender, LoggingEmailSender>();
builder.Services.AddHostedService<NotificationReminderService>();

var app = builder.Build();

// ── Seed Data ──
await SeedData.InitializeAsync(app.Services);

// Đặt ForwardedHeaders TRƯỚC mọi middleware khác để các bước sau biết là HTTPS thật
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Trong container Render, SSL đã được terminate ở edge nên tắt redirect (tránh vòng lặp).
if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PORT")))
    app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

public partial class Program { }
