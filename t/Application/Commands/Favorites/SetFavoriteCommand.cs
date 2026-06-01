namespace t.Application.Commands.Favorites;

public sealed record SetFavoriteCommand(
    string UserId,
    int ApartmentId,
    bool ShouldBeFavorite);

public enum SetFavoriteStatus
{
    Updated,
    Unchanged,
    ApartmentNotFound
}

public sealed record SetFavoriteResult(
    SetFavoriteStatus Status,
    bool IsFavorite);
