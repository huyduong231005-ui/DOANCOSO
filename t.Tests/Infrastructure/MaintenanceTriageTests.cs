using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using t.Infrastructure.Ai;
using t.Models.Entities;

namespace t.Tests.Infrastructure;

public class MaintenanceTriageTests
{
    private const string ValidJson =
        """{"category":"electrical","priority":"high","is_dangerous":false,"need_more_info":false,"follow_up_question":"","summary":"Mất điện toàn bộ căn hộ"}""";

    [Fact]
    public void Parse_maps_model_labels_to_enums_by_name()
    {
        var result = MaintenanceTriageService.Parse(ValidJson);

        Assert.NotNull(result);
        Assert.Equal(MaintenanceCategory.Electrical, result.Category);
        Assert.Equal(MaintenancePriority.High, result.Priority);
        Assert.False(result.IsDangerous);
    }

    [Fact]
    public void Parse_maps_other_to_enum_value_99()
    {
        var result = MaintenanceTriageService.Parse(ValidJson.Replace("electrical", "other"));

        Assert.Equal(MaintenanceCategory.Other, result!.Category);
    }

    [Fact]
    public void Parse_reads_handler_and_ignores_missing_or_unknown_values()
    {
        var withHandler = ValidJson.Replace("\"priority\"", "\"handler\":\"security\",\"priority\"");
        var unknown = ValidJson.Replace("\"priority\"", "\"handler\":\"police\",\"priority\"");

        Assert.Equal("security", MaintenanceTriageService.Parse(withHandler)!.Handler);
        Assert.Null(MaintenanceTriageService.Parse(unknown)!.Handler);
        Assert.Null(MaintenanceTriageService.Parse(ValidJson)!.Handler);
    }

    [Fact]
    public void Parse_keeps_follow_up_question_only_when_model_needs_more_info()
    {
        var vague = ValidJson.Replace("\"need_more_info\":false,\"follow_up_question\":\"\"",
            "\"need_more_info\":true,\"follow_up_question\":\"Hỏng ở đâu ạ?\"");
        var clearButChatty = ValidJson.Replace("\"follow_up_question\":\"\"", "\"follow_up_question\":\"Hỏng ở đâu ạ?\"");

        Assert.Equal("Hỏng ở đâu ạ?", MaintenanceTriageService.Parse(vague)!.FollowUpQuestion);
        Assert.Null(MaintenanceTriageService.Parse(clearButChatty)!.FollowUpQuestion);
        Assert.Null(MaintenanceTriageService.Parse(ValidJson)!.FollowUpQuestion);
    }

    [Theory]
    [InlineData("không phải json")]
    [InlineData("""{"category":"hvac","priority":"high","is_dangerous":false}""")]
    [InlineData("""{"category":"1","priority":"high","is_dangerous":false}""")]
    [InlineData("""{"category":"electrical","priority":"high"}""")]
    [InlineData("""{"category":"electrical","priority":"asap","is_dangerous":false}""")]
    [InlineData("""{"category":"electrical","priority":"high","is_dangerous":"true"}""")]
    [InlineData("[]")]
    public void Parse_returns_null_for_output_outside_the_schema(string raw)
    {
        Assert.Null(MaintenanceTriageService.Parse(raw));
    }

    [Fact]
    public void Apply_keeps_tenant_choice_when_model_is_unavailable()
    {
        var (category, priority) = MaintenanceTriageService.Apply(
            MaintenanceCategory.Pest, MaintenancePriority.High, null);

        Assert.Equal(MaintenanceCategory.Pest, category);
        Assert.Equal(MaintenancePriority.High, priority);
    }

    [Fact]
    public void Apply_never_lowers_priority_chosen_by_tenant()
    {
        var triage = MaintenanceTriageService.Parse(ValidJson.Replace("\"high\"", "\"low\""));

        var (_, priority) = MaintenanceTriageService.Apply(
            MaintenanceCategory.Other, MaintenancePriority.Urgent, triage);

        Assert.Equal(MaintenancePriority.Urgent, priority);
    }

    [Fact]
    public void Apply_raises_to_urgent_when_model_flags_danger()
    {
        var triage = MaintenanceTriageService.Parse(ValidJson.Replace("\"is_dangerous\":false", "\"is_dangerous\":true"));

        var (category, priority) = MaintenanceTriageService.Apply(
            MaintenanceCategory.Other, MaintenancePriority.Low, triage);

        Assert.Equal(MaintenanceCategory.Electrical, category);
        Assert.Equal(MaintenancePriority.Urgent, priority);
    }

    // Gọi Ollama thật. Chỉ chạy khi đặt OLLAMA_LIVE_URL (vd http://127.0.0.1:11434), còn lại bỏ qua
    // để CI và máy không có GPU vẫn xanh. Chạy lại sau mỗi lần đổi Ollama:Model.
    [Fact]
    public async Task TriageAsync_classifies_a_dangerous_report_against_live_ollama()
    {
        var url = Environment.GetEnvironmentVariable("OLLAMA_LIVE_URL");
        if (string.IsNullOrEmpty(url)) return;
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Ollama:BaseUrl"] = url,
            ["Ollama:Model"] = Environment.GetEnvironmentVariable("OLLAMA_LIVE_MODEL") ?? "qwen3:4b",
            ["Ollama:JsonMode"] = Environment.GetEnvironmentVariable("OLLAMA_LIVE_JSON_MODE") ?? "false",
            ["Ollama:TimeoutSeconds"] = "120",
        }).Build();
        using var services = new ServiceCollection().AddHttpClient().BuildServiceProvider();
        var service = new MaintenanceTriageService(
            config, services.GetRequiredService<IHttpClientFactory>(), NullLogger<MaintenanceTriageService>.Instance);

        var triage = await service.TriageAsync("Có khói bốc ra từ tủ điện ở hành lang tầng 9.");

        Assert.NotNull(triage);
        Assert.Equal(MaintenanceCategory.Electrical, triage.Category);
        Assert.True(triage.IsDangerous);
    }

    [Fact]
    public void Apply_raises_priority_when_model_rates_higher_than_tenant()
    {
        var (_, priority) = MaintenanceTriageService.Apply(
            MaintenanceCategory.Other, MaintenancePriority.Low, MaintenanceTriageService.Parse(ValidJson));

        Assert.Equal(MaintenancePriority.High, priority);
    }

    [Theory]
    [InlineData("")]                       // tính năng tắt
    [InlineData("http://127.0.0.1:1")]     // Ollama không chạy
    public async Task TriageAsync_returns_null_instead_of_throwing_when_model_is_unreachable(string baseUrl)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Ollama:BaseUrl"] = baseUrl,
            ["Ollama:Model"] = "qwen3:4b",
        }).Build();
        using var services = new ServiceCollection().AddHttpClient().BuildServiceProvider();
        var service = new MaintenanceTriageService(
            config, services.GetRequiredService<IHttpClientFactory>(), NullLogger<MaintenanceTriageService>.Instance);

        Assert.Null(await service.TriageAsync("Bồn rửa bị rò nước"));
    }

    [Fact]
    public void Apply_keeps_category_the_tenant_chose_explicitly()
    {
        var triage = MaintenanceTriageService.Parse(ValidJson);

        var (category, _) = MaintenanceTriageService.Apply(
            MaintenanceCategory.Plumbing, MaintenancePriority.Medium, triage);

        Assert.Equal(MaintenanceCategory.Plumbing, category);
    }
}
