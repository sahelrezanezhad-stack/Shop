using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shop.Infrastructure.Persistence;

namespace Shop.Presentation.Security;

public static class SallySecurity
{
    public const string CustomerScheme = "SallyCookie";
    public const string AdminScheme = "SallyAdminCookie";
    public const string AdminPolicy = "SallyAdministrator";
    public const string CredentialVersionClaim = "sally:credential-version";

    // Only a digest enters the encrypted authentication ticket, never a password hash.
    public static string CredentialVersion(string hash, string email)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hash + "\n" + email)));

    public static bool EqualsSecret(string? left, string right)
        => left is not null && CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(left), Encoding.UTF8.GetBytes(right));
}

// The store owner's credential is provisioned through deployment secrets. It never comes
// from SallyAccounts, customer registration, a default password, or a public setup endpoint.
// Additional managed administrator identities would require a separately reviewed schema.
public sealed class SallyAdminOptions
{
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(PasswordHash);
    public string NormalizedEmail => Email.Trim().ToUpperInvariant();
}

public sealed class SallyCustomerCookieEvents(ShopDbContext db) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var idText = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var stamp = context.Principal?.FindFirstValue(SallySecurity.CredentialVersionClaim);
        if (Guid.TryParse(idText, out var id))
        {
            var account = await db.SallyAccounts.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == id, context.HttpContext.RequestAborted);
            if (account is not null && SallySecurity.EqualsSecret(stamp,
                    SallySecurity.CredentialVersion(account.PasswordHash, account.NormalizedEmail))) return;
        }
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(SallySecurity.CustomerScheme);
    }
}

public sealed class SallyAdminCookieEvents(IOptionsMonitor<SallyAdminOptions> options) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var current = options.CurrentValue;
        var stamp = context.Principal?.FindFirstValue(SallySecurity.CredentialVersionClaim);
        if (current.IsConfigured && context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier) == current.NormalizedEmail
            && context.Principal.IsInRole("Administrator") && SallySecurity.EqualsSecret(stamp,
                SallySecurity.CredentialVersion(current.PasswordHash, current.NormalizedEmail))) return;
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(SallySecurity.AdminScheme);
    }
}
