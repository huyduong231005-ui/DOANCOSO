using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using t.Data;

namespace t.Tests.Integration;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly IServiceProvider InMemoryProvider =
        new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();
    private readonly string _databaseName = $"tests-{Guid.NewGuid():N}";

    protected virtual string EnvironmentName => "Testing";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.RemoveAll<AppDbContext>();

            // Đặt biến môi trường TEST_PG (chuỗi kết nối PostgreSQL) để chạy cả bộ test trên CSDL thật:
            // mỗi factory một CSDL tạm, có đủ migration, ràng buộc CHECK và trigger; xoá khi xong.
            var pg = Environment.GetEnvironmentVariable("TEST_PG");
            if (!string.IsNullOrWhiteSpace(pg))
            {
                _pgConnection = new Npgsql.NpgsqlConnectionStringBuilder(pg) { Database = _databaseName }.ConnectionString;
                services.AddDbContext<AppDbContext>(options => options.UseNpgsql(_pgConnection));
                return;
            }

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName)
                    .UseInternalServiceProvider(InMemoryProvider));
        });
    }

    private string? _pgConnection;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing || _pgConnection == null) return;
        var admin = new Npgsql.NpgsqlConnectionStringBuilder(_pgConnection) { Database = "postgres", Pooling = false };
        Npgsql.NpgsqlConnection.ClearAllPools();
        using var conn = new Npgsql.NpgsqlConnection(admin.ConnectionString);
        conn.Open();
        using var cmd = new Npgsql.NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)", conn);
        cmd.ExecuteNonQuery();
    }
}
