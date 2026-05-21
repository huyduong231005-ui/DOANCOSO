namespace t.Infrastructure.Security;

public interface IDevResetTokenStore
{
    void Save(string email, string token);
    string? Get(string email);
}
