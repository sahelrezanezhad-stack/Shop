using Microsoft.AspNetCore.DataProtection;
using Shop.Domain.Entities;
using Shop.Presentation.Security;
using Xunit;

namespace Shop.Security.Tests;

public sealed class PasswordResetTests
{
    [Fact]
    public void ResetTokenIsBoundToAccountAndCurrentCredentials()
    {
        var tokens = new SallyPasswordResetTokens(new EphemeralDataProtectionProvider());
        var account = new SallyAccount { PasswordHash = "original-hash", NormalizedEmail = "CUSTOMER@EXAMPLE.TEST" };
        var token = tokens.Create(account);
        Assert.True(tokens.IsValid(account, token));
        Assert.False(tokens.IsValid(new SallyAccount { PasswordHash = account.PasswordHash, NormalizedEmail = account.NormalizedEmail }, token));
        account.PasswordHash = "changed-hash";
        Assert.False(tokens.IsValid(account, token));
        account.PasswordHash = "original-hash";
        account.NormalizedEmail = "NEW@EXAMPLE.TEST";
        Assert.False(tokens.IsValid(account, token));
    }

    [Fact]
    public void ExpiredAndMalformedTokensAreRejected()
    {
        var protection = new EphemeralDataProtectionProvider();
        var tokens = new SallyPasswordResetTokens(protection);
        var account = new SallyAccount { PasswordHash = "hash", NormalizedEmail = "A@EXAMPLE.TEST" };
        var expired = protection.CreateProtector("SALLY.PasswordReset.v1").CreateProtector(account.Id.ToString("N"))
            .CreateProtector(account.PasswordHash).CreateProtector(account.NormalizedEmail).ToTimeLimitedDataProtector()
            .Protect("reset-password", DateTimeOffset.UtcNow.AddSeconds(-1));
        Assert.False(tokens.IsValid(account, expired));
        Assert.False(tokens.IsValid(account, "invalid-token"));
        Assert.False(tokens.IsValid(account, new string('x', 2049)));
        Assert.False(tokens.IsValid(account, null));
    }
}
