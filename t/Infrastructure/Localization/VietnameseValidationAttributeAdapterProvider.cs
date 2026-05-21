using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.Extensions.Localization;

namespace t.Infrastructure.Localization;

// Some "[Required]" attributes are added implicitly by the framework for non-nullable
// value-type properties (int, DateTime, decimal). They bypass IValidationMetadataProvider
// because they're built into the binder. This adapter intercepts each ValidationAttribute
// on its way to client-side adapter generation and patches the default English message
// to Vietnamese if none has been set.
public class VietnameseValidationAttributeAdapterProvider : ValidationAttributeAdapterProvider, IValidationAttributeAdapterProvider
{
    public new IAttributeAdapter? GetAttributeAdapter(ValidationAttribute attribute, IStringLocalizer? stringLocalizer)
    {
        if (string.IsNullOrEmpty(attribute.ErrorMessage) && string.IsNullOrEmpty(attribute.ErrorMessageResourceName))
        {
            attribute.ErrorMessage = attribute switch
            {
                RequiredAttribute       => "Trường này là bắt buộc.",
                EmailAddressAttribute   => "Email không hợp lệ.",
                PhoneAttribute          => "Số điện thoại không hợp lệ.",
                UrlAttribute            => "Đường dẫn không hợp lệ.",
                StringLengthAttribute   => "Độ dài chuỗi không hợp lệ (tối đa {1} ký tự).",
                MinLengthAttribute      => "Độ dài tối thiểu là {1}.",
                MaxLengthAttribute      => "Độ dài tối đa là {1}.",
                RangeAttribute          => "Giá trị phải nằm trong khoảng {1} đến {2}.",
                RegularExpressionAttribute => "Giá trị không đúng định dạng.",
                CompareAttribute cmp    => $"Giá trị phải trùng với {cmp.OtherProperty}.",
                _                       => attribute.ErrorMessage!
            };
        }
        return base.GetAttributeAdapter(attribute, stringLocalizer);
    }
}
