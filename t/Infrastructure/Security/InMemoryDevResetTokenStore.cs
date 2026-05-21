using System.Collections.Concurrent;

namespace t.Infrastructure.Security;

public sealed class InMemoryDevResetTokenStore : IDevResetTokenStore
{
    private readonly ConcurrentDictionary<string, string> _tokens = new(StringComparer.OrdinalIgnoreCase);

    public void Save(string email, string token)
    {
        _tokens[email] = token;
    }

    public string? Get(string email)
    {
        return _tokens.TryGetValue(email, out var token) ? token : null;
    }
}
