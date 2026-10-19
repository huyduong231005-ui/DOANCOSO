using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace t.Infrastructure.ModelBinding;

/// <summary>
/// Ô &lt;input type="number"&gt; luôn gửi số theo dạng invariant ("22.5"). Binder mặc định đọc theo
/// culture của máy (vi-VN coi "." là phân cách nghìn → 225), nên số thập phân/decimal đọc bằng InvariantCulture.
/// </summary>
public sealed class InvariantNumberModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext ctx)
    {
        var value = ctx.ValueProvider.GetValue(ctx.ModelName);
        if (value == ValueProviderResult.None) return Task.CompletedTask;
        ctx.ModelState.SetModelValue(ctx.ModelName, value);

        var raw = value.FirstValue?.Trim();
        var type = Nullable.GetUnderlyingType(ctx.ModelType) ?? ctx.ModelType;
        if (string.IsNullOrEmpty(raw))
        {
            if (Nullable.GetUnderlyingType(ctx.ModelType) != null) ctx.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        object? parsed = type == typeof(decimal)
            ? decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var m) ? m : null
            : double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : null;

        if (parsed == null)
            ctx.ModelState.TryAddModelError(ctx.ModelName, $"Giá trị '{raw}' không phải là số hợp lệ.");
        else
            ctx.Result = ModelBindingResult.Success(parsed);
        return Task.CompletedTask;
    }
}

public sealed class InvariantNumberModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        var t = Nullable.GetUnderlyingType(context.Metadata.ModelType) ?? context.Metadata.ModelType;
        return t == typeof(decimal) || t == typeof(double) ? new InvariantNumberModelBinder() : null;
    }
}
