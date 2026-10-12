using System.Net.Http.Json;
using System.Text.Json;
using t.Models.Entities;

namespace t.Infrastructure.Ai;

public record MaintenanceTriage(
    MaintenanceCategory Category,
    MaintenancePriority Priority,
    bool IsDangerous,
    string RawJson,
    // Câu hỏi lại cư dân khi mô tả quá mơ hồ; null nếu mô hình thấy mô tả đã đủ rõ.
    string? FollowUpQuestion = null);

/// <summary>
/// Hỏi mô hình chạy trên Ollama để phân loại mô tả sự cố của cư dân.
/// Tắt hẳn khi <c>Ollama:BaseUrl</c> để trống (máy không có GPU hoặc không cài Ollama).
/// Mọi lỗi đều trả về null để việc báo sự cố không phụ thuộc vào mô hình.
/// </summary>
public class MaintenanceTriageService
{
    // Giữ nguyên từng chữ so với prompt dùng khi train/test (run_test.py): mô hình fine-tune
    // học theo đúng prompt này, sửa ở đây mà không train lại sẽ làm giảm độ chính xác.
    private static readonly string SystemPrompt = """
        Bạn là trợ lý tiếp nhận sự cố của ban quản lý chung cư.
        Cư dân gửi một mô tả sự cố. Căn hộ và người báo đã được hệ thống biết sẵn, không cần hỏi lại.
        Hãy đọc mô tả và trả về đúng một đối tượng JSON, không viết gì thêm.

        category - chọn một:
        - plumbing: cấp thoát nước, rò rỉ, tắc nghẽn, thấm nước
        - electrical: điện, ổ cắm, đèn, tủ điện, cầu dao
        - appliance: thiết bị gia dụng (máy lạnh, máy giặt, bếp, tủ lạnh, máy nước nóng)
        - structural: tường, trần, sàn, cửa, cửa sổ, khóa
        - cleaning: vệ sinh, rác, mùi hôi khu vực chung
        - pest: côn trùng, chuột
        - network: internet, wifi, truyền hình
        - other: không thuộc loại nào ở trên hoặc không đủ thông tin để xác định

        priority - chọn một:
        - urgent: đe dọa an toàn của người hoặc tài sản ngay lúc này (cháy, khói, rò gas, chập điện, điện giật, ngập nước lan rộng, người mắc kẹt, vật sắp rơi)
        - high: mất một chức năng thiết yếu của căn hộ (mất điện, mất nước, mất internet khi cần gấp, toilet duy nhất bị tắc, không khóa được cửa, nước đang rò liên tục)
        - medium: hỏng hóc gây bất tiện nhưng vẫn sinh hoạt được
        - low: vấn đề nhỏ, thẩm mỹ, không gấp

        is_dangerous: true nếu có nguy cơ cháy nổ, điện giật, ngạt khí, hoặc thương tích cho người.
        need_more_info: true chỉ khi mô tả quá mơ hồ, không xác định được hỏng cái gì hoặc hỏng ở đâu.
        follow_up_question: nếu need_more_info là true, viết MỘT câu hỏi ngắn, lịch sự bằng tiếng Việt để hỏi lại cư dân; nếu không thì để chuỗi rỗng.
        summary: tóm tắt sự cố trong một câu tiếng Việt có dấu, tối đa 20 từ.
        """.Replace("\r\n", "\n"); // checkout trên Windows sinh CRLF; prompt lúc train dùng LF

    // Mô tả dài hơn mức này bị cắt khi gửi cho mô hình (bản lưu trong DB vẫn đủ): ngữ cảnh chỉ 2048 token.
    private const int MaxDescriptionChars = 2000;

    private static readonly string[] Categories =
        Enum.GetNames<MaintenanceCategory>().Select(n => n.ToLowerInvariant()).ToArray();
    private static readonly string[] Priorities =
        Enum.GetNames<MaintenancePriority>().Select(n => n.ToLowerInvariant()).ToArray();

    private static readonly object Schema = new
    {
        type = "object",
        properties = new
        {
            category = new { type = "string", @enum = Categories },
            priority = new { type = "string", @enum = Priorities },
            is_dangerous = new { type = "boolean" },
            need_more_info = new { type = "boolean" },
            follow_up_question = new { type = "string" },
            summary = new { type = "string" },
        },
        required = new[] { "category", "priority", "is_dangerous", "need_more_info", "follow_up_question", "summary" },
    };

    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<MaintenanceTriageService> _log;

    public MaintenanceTriageService(IConfiguration config, IHttpClientFactory httpFactory, ILogger<MaintenanceTriageService> log)
    {
        _config = config;
        _httpFactory = httpFactory;
        _log = log;
    }

    /// <summary>False khi <c>Ollama:BaseUrl</c> để trống, tức tính năng được tắt có chủ ý.</summary>
    public bool IsEnabled => !string.IsNullOrWhiteSpace(_config["Ollama:BaseUrl"]);

    public async Task<MaintenanceTriage?> TriageAsync(string description, CancellationToken ct = default)
    {
        var baseUrl = _config["Ollama:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl)) return null;
        try
        {
            using var client = _httpFactory.CreateClient("ollama");
            // Đo trên GTX 1650: ~2 giây mỗi yêu cầu khi mô hình đã nạp. Lần gọi đầu sau khi Ollama
            // gỡ mô hình khỏi VRAM mất 8-10 giây nên sẽ quá hạn và rơi về lựa chọn của cư dân.
            client.Timeout = TimeSpan.FromSeconds(_config.GetValue("Ollama:TimeoutSeconds", 5));
            using var response = await client.PostAsJsonAsync(baseUrl.TrimEnd('/') + "/api/chat", new
            {
                model = _config["Ollama:Model"],
                messages = new object[]
                {
                    new { role = "system", content = SystemPrompt },
                    new { role = "user", content = description.Length > MaxDescriptionChars ? description[..MaxDescriptionChars] : description },
                },
                stream = false,
                think = false,
                // Bản fine-tune chạy bằng runner llama.cpp, runner này ép khóa JSON theo thứ tự bảng chữ cái
                // khi nhận schema (follow_up_question đứng trước need_more_info), lệch thứ tự lúc train và
                // làm mô hình hỏi lại thừa. Bật Ollama:JsonMode cho mô hình fine-tune; Parse vẫn kiểm tra nhãn.
                format = _config.GetValue("Ollama:JsonMode", false) ? (object)"json" : Schema,
                options = new { temperature = 0, num_ctx = 2048, num_predict = 300 },
            }, ct);
            response.EnsureSuccessStatusCode();
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var content = body.RootElement.GetProperty("message").GetProperty("content").GetString();
            var triage = Parse(content);
            if (triage == null) _log.LogWarning("Maintenance triage returned output outside the schema");
            return triage;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Maintenance triage failed; keeping tenant's own choice");
            return null;
        }
    }

    /// <summary>Đọc JSON mô hình trả về. Null nếu sai định dạng hoặc nhãn nằm ngoài enum.</summary>
    public static MaintenanceTriage? Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;
            // Ánh xạ theo tên, không theo số: MaintenanceCategory.Other = 99.
            // So với danh sách tên vì Enum.TryParse chấp nhận cả chuỗi số như "1".
            var category = root.GetProperty("category").GetString()?.ToLowerInvariant();
            var priority = root.GetProperty("priority").GetString()?.ToLowerInvariant();
            if (!Categories.Contains(category) || !Priorities.Contains(priority)) return null;
            var dangerous = root.GetProperty("is_dangerous");
            if (dangerous.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return null;
            return new MaintenanceTriage(
                Enum.Parse<MaintenanceCategory>(category!, ignoreCase: true),
                Enum.Parse<MaintenancePriority>(priority!, ignoreCase: true),
                dangerous.GetBoolean(),
                raw,
                FollowUpQuestionOf(root));
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return null;
        }
    }

    private static string? FollowUpQuestionOf(JsonElement root)
    {
        if (!root.TryGetProperty("need_more_info", out var more) || more.ValueKind != JsonValueKind.True) return null;
        if (!root.TryGetProperty("follow_up_question", out var q) || q.ValueKind != JsonValueKind.String) return null;
        var question = q.GetString();
        return string.IsNullOrWhiteSpace(question) ? null : question.Trim();
    }

    /// <summary>
    /// Gộp lựa chọn của cư dân với đề xuất của mô hình. Mô hình chỉ điền loại khi cư dân để mặc định
    /// "Other", và chỉ được nâng mức ưu tiên, không bao giờ hạ: bỏ sót một sự cố nguy hiểm tệ hơn
    /// nhiều so với báo động thừa.
    /// </summary>
    public static (MaintenanceCategory Category, MaintenancePriority Priority) Apply(
        MaintenanceCategory tenantCategory, MaintenancePriority tenantPriority, MaintenanceTriage? triage)
    {
        if (triage == null) return (tenantCategory, tenantPriority);
        var category = tenantCategory == MaintenanceCategory.Other ? triage.Category : tenantCategory;
        var priority = triage.IsDangerous ? MaintenancePriority.Urgent : triage.Priority;
        return (category, priority > tenantPriority ? priority : tenantPriority);
    }
}
