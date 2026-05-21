using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.WebEncoders;
using t.Application.Commands.Auth;
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

var builder = WebApplication.CreateBuilder(args);

// On Windows, the host adds an EventLog logger provider by default. It throws
// ObjectDisposedException during shutdown if any logger writes after the provider
// has been disposed (a noisy red "BackgroundService failed" stack trace that has
// nothing to do with the actual app). Drop it — Console + Debug are enough for dev.
builder.Logging.AddFilter("Microsoft.Extensions.Logging.EventLog.EventLogLoggerProvider", LogLevel.None);

// ── Database ──
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

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
