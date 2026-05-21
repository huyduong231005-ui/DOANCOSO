using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace t.Infrastructure.Localization;

// Replaces the default English error messages on common DataAnnotations attributes
// with Vietnamese ones at model-metadata build time. Attributes that already carry
// an explicit ErrorMessage/ErrorMessageResourceName are left alone.
public class VietnameseValidationMessagesProvider : IValidationMetadataProvider
{
    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        foreach (var attr in context.ValidationMetadata.ValidatorMetadata)
        {
            if (attr is not ValidationAttribute va) continue;
            if (!string.IsNullOrEmpty(va.ErrorMessage) || !string.IsNullOrEmpty(va.ErrorMessageResourceName))
                continue;

            switch (va)
            {
                case RequiredAttribute:
                    va.ErrorMessage = "Trường này là bắt buộc.";
                    break;
                case EmailAddressAttribute:
                    va.ErrorMessage = "Email không hợp lệ.";
                    break;
                case PhoneAttribute:
                    va.ErrorMessage = "Số điện thoại không hợp lệ.";
                    break;
                case UrlAttribute:
                    va.ErrorMessage = "Đường dẫn không hợp lệ.";
                    break;
                case CompareAttribute cmp:
                    va.ErrorMessage = $"Giá trị phải trùng với {cmp.OtherProperty}.";
                    break;
                case StringLengthAttribute:
                    va.ErrorMessage = "Độ dài chuỗi không hợp lệ (tối đa {1} ký tự).";
                    break;
                case MinLengthAttribute:
                    va.ErrorMessage = "Độ dài tối thiểu là {1}.";
                    break;
                case MaxLengthAttribute:
                    va.ErrorMessage = "Độ dài tối đa là {1}.";
                    break;
                case RangeAttribute:
                    va.ErrorMessage = "Giá trị phải nằm trong khoảng {1} đến {2}.";
                    break;
                case RegularExpressionAttribute:
                    va.ErrorMessage = "Giá trị không đúng định dạng.";
                    break;
            }
        }
    }
}
