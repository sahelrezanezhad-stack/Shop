using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shop.Infrastructure.Persistence;
using Shop.Presentation.Accounts;
using Shop.Presentation.Security;

namespace Shop.Presentation.Controllers;

[Route("admin")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[AutoValidateAntiforgeryToken]
public sealed class AdminController(ShopDbContext db, IOptionsMonitor<SallyAdminOptions> options,
    IPasswordHasher<SallyAdminOptions> passwordHasher) : Controller
{
    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["returnUrl"] = SafeReturn(returnUrl);
        return View(new SignInInput());
    }

    [HttpPost("login"), EnableRateLimiting("admin-login")]
    public async Task<IActionResult> Login(SignInInput input, string? returnUrl = null)
    {
        ViewData["returnUrl"] = SafeReturn(returnUrl);
        if (!ModelState.IsValid) return View(input);
        var admin = options.CurrentValue;
        var verified = false;
        if (admin.IsConfigured)
        {
            try
            {
                verified = passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, input.Password)
                    != PasswordVerificationResult.Failed;
            }
            catch (FormatException) { /* Invalid deployment secret fails closed. */ }
        }
        if (!verified || !string.Equals(input.Email.Trim(), admin.Email.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("", "The administrator credentials are incorrect.");
            return View(input);
        }
        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, admin.NormalizedEmail),
            new Claim(ClaimTypes.Name, "SALLY Administrator"), new Claim(ClaimTypes.Role, "Administrator"),
            new Claim(SallySecurity.CredentialVersionClaim,
                SallySecurity.CredentialVersion(admin.PasswordHash, admin.NormalizedEmail))
        };
        await HttpContext.SignInAsync(SallySecurity.AdminScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, SallySecurity.AdminScheme)),
            new AuthenticationProperties { IsPersistent = false, ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(20) });
        return LocalRedirect(SafeReturn(returnUrl));
    }

    [HttpGet(""), Authorize(Policy = SallySecurity.AdminPolicy)]
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(new AdminOverview(await db.Products.CountAsync(ct), await db.Orders.CountAsync(ct),
            await db.Orders.CountAsync(x => x.Status == "PendingPayment", ct),
            await db.SallyEmailOutbox.CountAsync(x => x.SentUtc == null && x.Attempts < 10, ct)));

    [HttpPost("logout"), Authorize(Policy = SallySecurity.AdminPolicy)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(SallySecurity.AdminScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("denied")]
    public IActionResult Denied() => StatusCode(StatusCodes.Status403Forbidden);

    private string SafeReturn(string? value) => !string.IsNullOrEmpty(value) && Url.IsLocalUrl(value)
        && (value.StartsWith("/admin/", StringComparison.OrdinalIgnoreCase) || value == "/admin"
            || value.StartsWith("/Products", StringComparison.OrdinalIgnoreCase)) ? value : "/admin";
}

public sealed record AdminOverview(int Products, int Orders, int PendingOrders, int PendingEmails);
