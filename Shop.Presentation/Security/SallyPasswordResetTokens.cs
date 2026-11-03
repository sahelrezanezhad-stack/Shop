using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Shop.Domain.Entities;

namespace Shop.Presentation.Security;

// Reuses the existing encrypted outbox; no new column, table, or schema change.
// Changing a password or email changes the protector purpose and revokes older tokens.
public sealed class SallyPasswordResetTokens(IDataProtectionProvider protection)
{
    private ITimeLimitedDataProtector For(SallyAccount account) => protection
        .CreateProtector("SALLY.PasswordReset.v1")
        .CreateProtector(account.Id.ToString("N"))
        .CreateProtector(account.PasswordHash)
        .CreateProtector(account.NormalizedEmail)
        .ToTimeLimitedDataProtector();

    public string Create(SallyAccount account) => For(account).Protect("reset-password", TimeSpan.FromMinutes(30));

    public bool IsValid(SallyAccount account, string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 2048) return false;
        try { return For(account).Unprotect(token) == "reset-password"; }
        catch (Exception ex) when (ex is CryptographicException or FormatException) { return false; }
    }
}
