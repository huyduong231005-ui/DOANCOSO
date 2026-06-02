using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace t.Infrastructure.ModelBinding;

public sealed class InvariantNullableDoubleModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None)
            return Task.CompletedTask;

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

        var value = valueProviderResult.FirstValue;
        if (string.IsNullOrWhiteSpace(value))
        {
            bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var coordinate))
        {
            bindingContext.Result = ModelBindingResult.Success(coordinate);
            return Task.CompletedTask;
        }

        bindingContext.ModelState.TryAddModelError(
            bindingContext.ModelName,
            $"Giá trị '{value}' không phải là tọa độ hợp lệ.");
        return Task.CompletedTask;
    }
}
