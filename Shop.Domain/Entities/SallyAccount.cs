namespace Shop.Domain.Entities;

// A separate customer sign-in identity. Never expose PasswordHash through storefront APIs.
public sealed class SallyAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DisplayName { get; set; } = "";
    public string Email { get; set; } = "";
    public string NormalizedEmail { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public bool EmailVerified { get; set; }
    public string? VerificationTokenHash { get; set; }
    public DateTime? VerificationExpiresUtc { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
