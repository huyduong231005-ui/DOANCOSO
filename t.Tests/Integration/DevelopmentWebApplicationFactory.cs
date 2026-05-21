namespace t.Tests.Integration;

public sealed class DevelopmentWebApplicationFactory : TestWebApplicationFactory
{
    protected override string EnvironmentName => "Development";
}
