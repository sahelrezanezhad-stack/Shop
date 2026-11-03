using System.ComponentModel.DataAnnotations;

namespace Shop.Presentation.Accounts;

public sealed class SignUpInput
{
    [Required(ErrorMessage="Please enter your full name.")]
    [StringLength(120,MinimumLength=2)]
    public string DisplayName { get; set; } = "";
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = "";
    [Required, StringLength(128, MinimumLength=10)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";
    [Compare(nameof(Password), ErrorMessage="The passwords do not match.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
}
public sealed class SignInInput
{
    [Required,EmailAddress,StringLength(256)] public string Email { get; set; }="";
    [Required,StringLength(128)] public string Password { get; set; }="";
}

public sealed class ForgotPasswordInput
{
    [Required, EmailAddress, StringLength(256)] public string Email { get; set; } = "";
}

public sealed class ResetPasswordInput
{
    [Required] public Guid Id { get; set; }
    [Required, StringLength(2048)] public string Token { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 10), DataType(DataType.Password)]
    public string Password { get; set; } = "";
    [Required, Compare(nameof(Password)), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
}

public sealed class ChangeEmailInput
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = "";

    [Required, StringLength(128)]
    public string CurrentPassword { get; set; } = "";
}
