using System.ComponentModel.DataAnnotations;

namespace t.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập địa chỉ email.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng. Ví dụ: ten@domain.com")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ và tên phải từ 2 đến 100 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ email.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng. Ví dụ: ten@domain.com")]
    public string Email { get; set; } = string.Empty;

    [RegularExpression(@"^(0|\+84)[0-9]{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ. Ví dụ: 0901234567 hoặc +84901234567")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [DataType(DataType.Password)]
    [RegularExpression(@"^(?=.*\d).{6,100}$", ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự và chứa ít nhất 1 chữ số.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp với mật khẩu đã nhập.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập địa chỉ email.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng. Ví dụ: ten@domain.com")]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [DataType(DataType.Password)]
    [RegularExpression(@"^(?=.*\d).{6,100}$", ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự và chứa ít nhất 1 chữ số.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
