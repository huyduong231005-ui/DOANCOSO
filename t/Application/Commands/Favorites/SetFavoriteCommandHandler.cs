using Microsoft.EntityFrameworkCore;
using t.Data;
using t.Models.Entities;

namespace t.Application.Commands.Favorites;

public sealed class SetFavoriteCommandHandler
{
    private readonly AppDbContext _db;

    public SetFavoriteCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<SetFavoriteResult> HandleAsync(
        SetFavoriteCommand command,
        CancellationToken cancellationToken = default)
    {
        var apartmentExists = await _db.Apartments
            .AnyAsync(a => a.Id == command.ApartmentId, cancellationToken);
        if (!apartmentExists)
            return new SetFavoriteResult(SetFavoriteStatus.ApartmentNotFound, IsFavorite: false);

        try
        {
            return await ApplyDesiredStateAsync(command, cancellationToken);
        }
        catch (DbUpdateException) when (command.ShouldBeFavorite)
        {
            _db.ChangeTracker.Clear();

            var conflictExists = await _db.Favorites
                .IgnoreQueryFilters()
                .AnyAsync(
                    f => f.UserId == command.UserId && f.ApartmentId == command.ApartmentId,
                    cancellationToken);
            if (!conflictExists)
                throw;

            return await ApplyDesiredStateAsync(command, cancellationToken);
        }
    }

    private async Task<SetFavoriteResult> ApplyDesiredStateAsync(
        SetFavoriteCommand command,
        CancellationToken cancellationToken)
    {
        var existing = await _db.Favorites
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                f => f.UserId == command.UserId && f.ApartmentId == command.ApartmentId,
                cancellationToken);

        if (existing == null)
        {
            if (!command.ShouldBeFavorite)
                return new SetFavoriteResult(SetFavoriteStatus.Unchanged, IsFavorite: false);

            _db.Favorites.Add(new Favorite
            {
                UserId = command.UserId,
                ApartmentId = command.ApartmentId
            });
            await _db.SaveChangesAsync(cancellationToken);
            return new SetFavoriteResult(SetFavoriteStatus.Updated, IsFavorite: true);
        }

        if (existing.IsDeleted)
        {
            if (!command.ShouldBeFavorite)
                return new SetFavoriteResult(SetFavoriteStatus.Unchanged, IsFavorite: false);

            existing.IsDeleted = false;
            existing.DeletedAt = null;
            existing.DeletedBy = null;
            await _db.SaveChangesAsync(cancellationToken);
            return new SetFavoriteResult(SetFavoriteStatus.Updated, IsFavorite: true);
        }

        if (command.ShouldBeFavorite)
            return new SetFavoriteResult(SetFavoriteStatus.Unchanged, IsFavorite: true);

        _db.Favorites.Remove(existing);
        await _db.SaveChangesAsync(cancellationToken);
        return new SetFavoriteResult(SetFavoriteStatus.Updated, IsFavorite: false);
    }
}
