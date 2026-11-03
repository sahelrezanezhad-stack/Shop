using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Shop.Domain.Entities;
using Shop.Infrastructure.Persistence;
using Shop.Presentation.Accounts;
using Shop.Presentation.Security;

namespace Shop.Presentation.Controllers;

[Route("account")]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class AccountController : Controller
{
    public const string CookieScheme="SallyCookie";
    private readonly ShopDbContext _db;
    private readonly IPasswordHasher<SallyAccount> _passwordHasher;
    private readonly IDataProtector _protector;
    private readonly ILogger<AccountController> _logger;
    private readonly SallyPasswordResetTokens _resetTokens;
    private static readonly SallyAccount DummyAccount = new();
    private static readonly string DummyHash = new PasswordHasher<SallyAccount>()
        .HashPassword(DummyAccount, Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));
    public AccountController(ShopDbContext db, IPasswordHasher<SallyAccount> passwordHasher,
        IDataProtectionProvider protector, ILogger<AccountController> logger, SallyPasswordResetTokens resetTokens)
    {
        _db=db; _passwordHasher=passwordHasher; _protector=protector.CreateProtector("SALLY.EmailVerification.v1"); _logger=logger; _resetTokens=resetTokens;
    }
    [HttpGet("register")]
    public IActionResult Register() => View(new SignUpInput());

    [HttpPost("register"),ValidateAntiForgeryToken,EnableRateLimiting("account-writes")]
    public async Task<IActionResult> Register(SignUpInput input, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(input);
        var normalized=input.Email.Trim().ToUpperInvariant();
        if(await _db.SallyAccounts.AnyAsync(x=>x.NormalizedEmail==normalized,ct))
        {
            ModelState.AddModelError(nameof(input.Email),"This email address is already registered."); return View(input);
        }
        var account=new SallyAccount { DisplayName=input.DisplayName.Trim(), Email=input.Email.Trim(), NormalizedEmail=normalized };
        account.PasswordHash=_passwordHasher.HashPassword(account,input.Password);
        var rawToken=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        account.VerificationTokenHash=Sha(rawToken);
        account.VerificationExpiresUtc=DateTime.UtcNow.AddHours(24);
        _db.SallyAccounts.Add(account);
        _db.SallyEmailOutbox.Add(new SallyEmailOutbox { AccountId=account.Id,Kind="VerifyEmail",ProtectedToken=_protector.Protect(rawToken) });
        try { await _db.SaveChangesAsync(ct); }
        catch(DbUpdateException ex)
        {
            _logger.LogInformation(ex,"Account create failed (possibly duplicate email)");
            ModelState.AddModelError(nameof(input.Email),"This email address could not be registered."); return View(input);
        }
        await SignInUser(account);
        TempData["notice"]="Your account has been created. Verify your email address to receive order updates.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("login")]
    public IActionResult Login(string? returnUrl=null) { ViewData["returnUrl"]=SafeReturn(returnUrl); return View(new SignInInput()); }
    [HttpPost("login"),ValidateAntiForgeryToken,EnableRateLimiting("account-writes")]
    public async Task<IActionResult> Login(SignInInput input,string? returnUrl=null,CancellationToken ct=default)
    {
        ViewData["returnUrl"]=SafeReturn(returnUrl);
        if(!ModelState.IsValid) return View(input);
        var normalized=input.Email.Trim().ToUpperInvariant();
        var account=await _db.SallyAccounts.SingleOrDefaultAsync(x=>x.NormalizedEmail==normalized,ct);
        // Generic feedback to avoid credential disclosure.
        var passwordResult = _passwordHasher.VerifyHashedPassword(account ?? DummyAccount, account?.PasswordHash ?? DummyHash, input.Password);
        if (account is null || passwordResult is PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError("","The email address or password is incorrect.");return View(input);
        }
        if(passwordResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            var previousHash=account.PasswordHash;
            var newHash=_passwordHasher.HashPassword(account,input.Password);
            var updated=await _db.SallyAccounts.Where(x=>x.Id==account.Id && x.PasswordHash==previousHash)
                .ExecuteUpdateAsync(s=>s.SetProperty(x=>x.PasswordHash,newHash),ct);
            if(updated!=1){ModelState.AddModelError("","Please sign in again.");return View(input);}
            account.PasswordHash=newHash;
        }
        await SignInUser(account);
        return LocalRedirect(SafeReturn(returnUrl));
    }
    [HttpPost("logout"),ValidateAntiForgeryToken,Authorize(AuthenticationSchemes=CookieScheme)]
    public async Task<IActionResult> Logout() { await HttpContext.SignOutAsync(CookieScheme); return RedirectToAction("Index","Home"); }

    [HttpGet("verify")]
    public async Task<IActionResult> Verify(string? id,string? token,CancellationToken ct)
    {
        if(!Guid.TryParse(id,out var userId) || string.IsNullOrEmpty(token) || token.Length>256) return BadRequest("This verification link is invalid.");
        var account=await _db.SallyAccounts.SingleOrDefaultAsync(x=>x.Id==userId,ct);
        if(account is null || account.EmailVerified || account.VerificationTokenHash is null || account.VerificationExpiresUtc is null || account.VerificationExpiresUtc < DateTime.UtcNow ||
            !CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(account.VerificationTokenHash),Encoding.ASCII.GetBytes(Sha(token))))
            return BadRequest("This verification link is invalid or has expired.");
        var changed=await _db.SallyAccounts.Where(x=>x.Id==userId && !x.EmailVerified
                && x.VerificationTokenHash==account.VerificationTokenHash && x.VerificationExpiresUtc>DateTime.UtcNow)
            .ExecuteUpdateAsync(s=>s.SetProperty(x=>x.EmailVerified,true)
                .SetProperty(x=>x.VerificationTokenHash,(string?)null)
                .SetProperty(x=>x.VerificationExpiresUtc,(DateTime?)null),ct);
        if(changed!=1)return BadRequest("This verification link is invalid or has expired.");
        TempData["notice"]="Your email address has been verified.";
        return RedirectToAction(nameof(Index));
    }
    [HttpPost("resend"),ValidateAntiForgeryToken,Authorize(AuthenticationSchemes=CookieScheme),EnableRateLimiting("account-writes")]
    public async Task<IActionResult> Resend(CancellationToken ct)
    {
        if(!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)) return Forbid();
        var account=await _db.SallyAccounts.SingleOrDefaultAsync(x=>x.Id==id,ct);
        if(account is null) return NotFound();
        if(account.EmailVerified){TempData["notice"]="Your email address is already verified.";return RedirectToAction(nameof(Index));}
        var recent=await _db.SallyEmailOutbox.AnyAsync(x=>x.AccountId==id && x.Kind=="VerifyEmail" && x.Attempts < 10 && x.CreatedUtc>DateTime.UtcNow.AddMinutes(-10),ct);
        if(recent){TempData["notice"]="Please wait 10 minutes before requesting another verification link.";return RedirectToAction(nameof(Index));}
        // Expire old unsent links so only the newest one will be delivered.
        var pending=await _db.SallyEmailOutbox.Where(x=>x.AccountId==id && x.Kind=="VerifyEmail" && x.SentUtc==null).ToListAsync(ct);
        foreach(var old in pending){old.Attempts=10;old.LastError="Superseded";}
        var raw=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        account.VerificationTokenHash=Sha(raw);
        account.VerificationExpiresUtc=DateTime.UtcNow.AddHours(24);
        _db.SallyEmailOutbox.Add(new SallyEmailOutbox{AccountId=id,Kind="VerifyEmail",ProtectedToken=_protector.Protect(raw)});
        await _db.SaveChangesAsync(ct);
        TempData["notice"]="A new verification email has been queued.";
        return RedirectToAction(nameof(Index));
    }
    [HttpPost("change-email"), ValidateAntiForgeryToken, Authorize(AuthenticationSchemes=CookieScheme), EnableRateLimiting("account-writes")]
    public async Task<IActionResult> ChangeEmail(ChangeEmailInput input, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) return Forbid();
        var account = await _db.SallyAccounts.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (account is null) return NotFound();

        if (!ModelState.IsValid)
        {
            TempData["error"] = "Enter a valid email address and your current password.";
            return RedirectToAction(nameof(Index));
        }
        if (_passwordHasher.VerifyHashedPassword(account, account.PasswordHash, input.CurrentPassword) == PasswordVerificationResult.Failed)
        {
            TempData["error"] = "The current password is incorrect. Your email address has not been changed.";
            return RedirectToAction(nameof(Index));
        }

        var email = input.Email.Trim();
        var normalized = email.ToUpperInvariant();
        if (normalized == account.NormalizedEmail)
        {
            TempData["notice"] = "This is already the email address on your account.";
            return RedirectToAction(nameof(Index));
        }
        if (await _db.SallyAccounts.AnyAsync(x => x.Id != id && x.NormalizedEmail == normalized, ct))
        {
            TempData["error"] = "This email address is already in use.";
            return RedirectToAction(nameof(Index));
        }

        // Invalidate unsent verification messages for the old address. Pending order notices remain
        // queued and can be delivered to the new address only after it has been verified.
        var oldVerificationJobs = await _db.SallyEmailOutbox
            .Where(x => x.AccountId == id && x.Kind == "VerifyEmail" && x.SentUtc == null && x.Attempts < 10)
            .ToListAsync(ct);
        foreach (var old in oldVerificationJobs)
        {
            old.Attempts = 10;
            old.LastError = "Superseded by email address change";
        }

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        account.Email = email;
        account.NormalizedEmail = normalized;
        account.EmailVerified = false;
        account.VerificationTokenHash = Sha(rawToken);
        account.VerificationExpiresUtc = DateTime.UtcNow.AddHours(24);
        _db.SallyEmailOutbox.Add(new SallyEmailOutbox
        {
            AccountId = id,
            Kind = "VerifyEmail",
            ProtectedToken = _protector.Protect(rawToken)
        });

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData["error"] = "Your account changed while this request was being processed. Sign in again and retry.";
            return RedirectToAction(nameof(Login));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogInformation(ex, "Email address change failed (possibly duplicate email)");
            TempData["error"] = "This email address could not be saved. It may already be in use.";
            return RedirectToAction(nameof(Index));
        }

        await SignInUser(account);
        TempData["notice"] = "Your email address has been updated. Verify the new address before order emails can be sent.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("")]
    [Authorize(AuthenticationSchemes=CookieScheme)]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if(!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)) return Challenge();
        var account=await _db.SallyAccounts.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id,ct);
        if(account is null){await HttpContext.SignOutAsync(CookieScheme);return RedirectToAction(nameof(Login));}
        var orders=await _db.Orders.AsNoTracking().Where(x=>x.AccountId==id)
            .OrderByDescending(x=>x.OrderDate)
            .Select(x=>new AccountOrderView(x.Id,x.OrderDate,x.Status,x.TotalAmount,
                x.Items.Select(i=>new AccountOrderLineView(i.Product.Name,i.Size,i.Quantity)).ToList()))
            .ToListAsync(ct);
        return View(new AccountDashboardView(account.DisplayName,account.Email,account.EmailVerified,orders));
    }
    private async Task SignInUser(SallyAccount account)
    {
        var identity=new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,account.Id.ToString()),
            new Claim(ClaimTypes.Name,account.DisplayName),new Claim(ClaimTypes.Email,account.Email),
            new Claim(SallySecurity.CredentialVersionClaim,SallySecurity.CredentialVersion(account.PasswordHash,account.NormalizedEmail))},CookieScheme);
        await HttpContext.SignInAsync(CookieScheme,new ClaimsPrincipal(identity),new AuthenticationProperties{IsPersistent=false,ExpiresUtc=DateTimeOffset.UtcNow.AddHours(8)});
    }

    [HttpGet("forgot-password")]
    public IActionResult ForgotPassword() => View(new ForgotPasswordInput());

    [HttpPost("forgot-password"), ValidateAntiForgeryToken, EnableRateLimiting("account-writes")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordInput input, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(input);
        var email = input.Email.Trim().ToUpperInvariant();
        var account = await _db.SallyAccounts.AsNoTracking().SingleOrDefaultAsync(x => x.NormalizedEmail == email, ct);
        if (account is not null && account.EmailVerified)
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-10);
            if (!await _db.SallyEmailOutbox.AnyAsync(x => x.AccountId == account.Id
                    && x.Kind == "ResetPassword" && x.CreatedUtc > cutoff, ct))
            {
                _db.SallyEmailOutbox.Add(new SallyEmailOutbox { AccountId = account.Id, Kind = "ResetPassword",
                    ProtectedToken = _resetTokens.Create(account) });
                await _db.SaveChangesAsync(ct);
            }
        }
        // Same response for unknown, unverified and known addresses.
        ViewData["notice"] = "If this is a verified SALLY account, a password reset link will be sent. Check your inbox.";
        ModelState.Clear();
        return View(new ForgotPasswordInput());
    }

    [HttpGet("reset-password")]
    public IActionResult ResetPassword(Guid id, string? token)
    {
        if (id == Guid.Empty || string.IsNullOrWhiteSpace(token) || token.Length > 2048)
            return BadRequest("This password reset link is invalid or has expired.");
        return View(new ResetPasswordInput { Id = id, Token = token });
    }

    [HttpPost("reset-password"), ValidateAntiForgeryToken, EnableRateLimiting("account-writes")]
    public async Task<IActionResult> ResetPassword(ResetPasswordInput input, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(input);
        var account = await _db.SallyAccounts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == input.Id, ct);
        if (account is null || !account.EmailVerified || !_resetTokens.IsValid(account, input.Token))
        {
            ModelState.AddModelError("", "This password reset link is invalid or has expired. Request a new link.");
            return View(input);
        }
        var previousHash = account.PasswordHash;
        var previousEmail = account.NormalizedEmail;
        var nextHash = _passwordHasher.HashPassword(account, input.Password);
        // Compare-and-swap makes concurrent use of the same link succeed only once.
        var changed = await _db.SallyAccounts.Where(x => x.Id == account.Id && x.EmailVerified
                && x.PasswordHash == previousHash && x.NormalizedEmail == previousEmail)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.PasswordHash, nextHash), ct);
        if (changed != 1)
        {
            ModelState.AddModelError("", "This password reset link has already been used or is no longer valid.");
            return View(input);
        }
        await HttpContext.SignOutAsync(CookieScheme);
        TempData["notice"] = "Your password has been changed. Sign in with your new password.";
        return RedirectToAction(nameof(Login));
    }
    private string SafeReturn(string? value)=> !string.IsNullOrWhiteSpace(value) && Url.IsLocalUrl(value) ? value : "/account";
    private static string Sha(string s)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s)));
}

public sealed record AccountOrderLineView(string Name,string Size,int Quantity);
public sealed record AccountOrderView(int Id,DateTime DateUtc,string Status,decimal TotalToman,List<AccountOrderLineView> Items);
public sealed record AccountDashboardView(string Name,string Email,bool EmailVerified,List<AccountOrderView> Orders);
