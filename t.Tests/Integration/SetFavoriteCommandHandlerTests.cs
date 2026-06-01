using Microsoft.EntityFrameworkCore;
using t.Application.Commands.Favorites;
using t.Data;
using t.Models.Entities;

namespace t.Tests.Integration;

public class SetFavoriteCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldRecover_WhenConcurrentInsertWinsTheUniqueKey()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"favorite-conflict-{Guid.NewGuid():N}")
            .Options;

        await using var db = new ConcurrentInsertAppDbContext(options);
        db.Apartments.Add(new Apartment
        {
            Title = "Race Test Apartment",
            Slug = "race-test-apartment",
            Address = "Race Test Address"
        });
        await db.SaveChangesAsync();

        var apartmentId = await db.Apartments.Select(a => a.Id).SingleAsync();
        var handler = new SetFavoriteCommandHandler(db);

        var result = await handler.HandleAsync(
            new SetFavoriteCommand("race-user", apartmentId, ShouldBeFavorite: true));

        Assert.Equal(SetFavoriteStatus.Unchanged, result.Status);
        Assert.True(result.IsFavorite);

        var rows = await db.Favorites
            .IgnoreQueryFilters()
            .Where(f => f.UserId == "race-user" && f.ApartmentId == apartmentId)
            .ToListAsync();
        var row = Assert.Single(rows);
        Assert.False(row.IsDeleted);
    }

    private sealed class ConcurrentInsertAppDbContext : AppDbContext
    {
        private bool _injectConcurrentInsert = true;

        public ConcurrentInsertAppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var pendingFavorite = ChangeTracker.Entries<Favorite>()
                .FirstOrDefault(entry => entry.State == EntityState.Added);
            if (!_injectConcurrentInsert || pendingFavorite == null)
                return await base.SaveChangesAsync(cancellationToken);

            _injectConcurrentInsert = false;
            var winner = new Favorite
            {
                UserId = pendingFavorite.Entity.UserId,
                ApartmentId = pendingFavorite.Entity.ApartmentId
            };

            ChangeTracker.Clear();
            Favorites.Add(winner);
            await base.SaveChangesAsync(cancellationToken);
            ChangeTracker.Clear();

            throw new DbUpdateException("Simulated concurrent insert conflict.");
        }
    }
}
