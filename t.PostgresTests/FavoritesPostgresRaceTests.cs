using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Testcontainers.PostgreSql;
using t.Application.Commands.Favorites;
using t.Data;
using t.Models.Entities;

namespace t.PostgresTests;

public sealed class FavoritesPostgresRaceTests : IAsyncLifetime
{
    private readonly string? _externalConnectionString =
        Environment.GetEnvironmentVariable("FAVORITES_TEST_POSTGRES_CONNECTION");
    private readonly PostgreSqlContainer? _postgres;

    public FavoritesPostgresRaceTests()
    {
        if (string.IsNullOrWhiteSpace(_externalConnectionString))
            _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
    }

    public Task InitializeAsync() => _postgres?.StartAsync() ?? Task.CompletedTask;

    public Task DisposeAsync() => _postgres?.DisposeAsync().AsTask() ?? Task.CompletedTask;

    [Fact]
    public async Task SetFavorite_ShouldConvergeToOneActiveRow_WhenTwoAddsRace()
    {
        var connectionString = _externalConnectionString ?? _postgres!.GetConnectionString();
        var baseOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        var (apartmentId, userId) = await SeedAsync(baseOptions);
        var barrier = new ConcurrentFavoriteInsertBarrier();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .AddInterceptors(barrier)
            .Options;

        await using var firstDb = new AppDbContext(options);
        await using var secondDb = new AppDbContext(options);
        var first = new SetFavoriteCommandHandler(firstDb);
        var second = new SetFavoriteCommandHandler(secondDb);
        var command = new SetFavoriteCommand(userId, apartmentId, ShouldBeFavorite: true);

        var results = await Task.WhenAll(
            first.HandleAsync(command),
            second.HandleAsync(command));

        Assert.All(results, result => Assert.True(result.IsFavorite));

        await using var assertionDb = new AppDbContext(baseOptions);
        var rows = await assertionDb.Favorites
            .IgnoreQueryFilters()
            .Where(f => f.UserId == command.UserId && f.ApartmentId == command.ApartmentId)
            .ToListAsync();
        var row = Assert.Single(rows);
        Assert.False(row.IsDeleted);
    }

    private static async Task<(int ApartmentId, string UserId)> SeedAsync(DbContextOptions<AppDbContext> options)
    {
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var suffix = Guid.NewGuid().ToString("N");
        var hostId = $"race-host-{suffix}";
        var userId = $"race-user-{suffix}";
        var host = new AppUser
        {
            Id = hostId,
            UserName = $"race-host-{suffix}@example.com",
            NormalizedUserName = $"RACE-HOST-{suffix}@EXAMPLE.COM",
            Email = $"race-host-{suffix}@example.com",
            NormalizedEmail = $"RACE-HOST-{suffix}@EXAMPLE.COM",
            FullName = "Race Host"
        };
        var user = new AppUser
        {
            Id = userId,
            UserName = $"race-user-{suffix}@example.com",
            NormalizedUserName = $"RACE-USER-{suffix}@EXAMPLE.COM",
            Email = $"race-user-{suffix}@example.com",
            NormalizedEmail = $"RACE-USER-{suffix}@EXAMPLE.COM",
            FullName = "Race User"
        };
        var region = new Region { Name = "Race Region", Slug = $"race-region-{suffix}" };
        var category = new Category { Name = "Race Category", Slug = $"race-category-{suffix}" };
        db.AddRange(host, user, region, category);
        await db.SaveChangesAsync();

        var apartment = new Apartment
        {
            Title = "Race Apartment",
            Slug = $"race-apartment-{suffix}",
            Address = "Race Address",
            HostId = host.Id,
            RegionId = region.Id,
            CategoryId = category.Id,
            Status = ListingStatus.Active
        };
        db.Apartments.Add(apartment);
        await db.SaveChangesAsync();

        return (apartment.Id, userId);
    }

    private sealed class ConcurrentFavoriteInsertBarrier : SaveChangesInterceptor
    {
        private readonly TaskCompletionSource _bothInsertsReady =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrivals;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context?.ChangeTracker.Entries<Favorite>()
                    .Any(entry => entry.State == EntityState.Added) != true)
                return result;

            if (Interlocked.Increment(ref _arrivals) >= 2)
                _bothInsertsReady.TrySetResult();

            await _bothInsertsReady.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            return result;
        }
    }
}
