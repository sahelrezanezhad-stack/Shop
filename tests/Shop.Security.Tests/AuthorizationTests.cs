using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shop.Domain.Entities;
using Shop.Infrastructure.Persistence;
using Shop.Presentation;
using Shop.Presentation.Security;
using Xunit;

namespace Shop.Security.Tests;

public sealed class AuthorizationTests
{
    [Fact]
    public async Task AnonymousVisitorCannotUseProductAdministration()
    {
        using var factory = new TestStore();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
        var response = await client.GetAsync("/Products/Create");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("https://localhost/admin/login", response.Headers.Location!.ToString());
        var post = await client.PostAsync("/api/products", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
    }

    [Fact]
    public async Task CustomerCookieCannotBecomeAdministratorCookie()
    {
        using var factory = new TestStore();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        var account = new SallyAccount { Email = "customer@example.test", NormalizedEmail = "CUSTOMER@EXAMPLE.TEST", PasswordHash = "hash", DisplayName = "Customer" };
        db.SallyAccounts.Add(account); await db.SaveChangesAsync();
        var options = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>>()
            .Get(SallySecurity.CustomerScheme);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier,account.Id.ToString()),
            new Claim(SallySecurity.CredentialVersionClaim,SallySecurity.CredentialVersion(account.PasswordHash,account.NormalizedEmail)),
            new Claim(ClaimTypes.Role,"Administrator") // A role in the customer scheme grants no admin access.
        },SallySecurity.CustomerScheme));
        var ticket = new AuthenticationTicket(principal, new AuthenticationProperties {
            IssuedUtc=DateTimeOffset.UtcNow,ExpiresUtc=DateTimeOffset.UtcNow.AddMinutes(5)
        },SallySecurity.CustomerScheme);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add("Cookie", "sally_auth="+options.TicketDataFormat.Protect(ticket));
        var response = await client.GetAsync("/Products/Create");
        Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);
        Assert.Contains("/admin/login",response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task RevokedCustomerCredentialCannotUsePreviousSession()
    {
        using var factory = new TestStore();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        var account = new SallyAccount { Email="customer@example.test",NormalizedEmail="CUSTOMER@EXAMPLE.TEST",PasswordHash="old-hash",DisplayName="Customer" };
        db.SallyAccounts.Add(account);await db.SaveChangesAsync();
        var options=scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>>().Get(SallySecurity.CustomerScheme);
        var identity=new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,account.Id.ToString()),new Claim(SallySecurity.CredentialVersionClaim,SallySecurity.CredentialVersion(account.PasswordHash,account.NormalizedEmail))},SallySecurity.CustomerScheme);
        var ticket=new AuthenticationTicket(new ClaimsPrincipal(identity),new AuthenticationProperties{IssuedUtc=DateTimeOffset.UtcNow,ExpiresUtc=DateTimeOffset.UtcNow.AddMinutes(5)},SallySecurity.CustomerScheme);
        var cookie=options.TicketDataFormat.Protect(ticket);
        account.PasswordHash="replacement-hash";await db.SaveChangesAsync();
        using var client=factory.CreateClient(new WebApplicationFactoryClientOptions{AllowAutoRedirect=false,BaseAddress=new Uri("https://localhost")});
        client.DefaultRequestHeaders.Add("Cookie","sally_auth="+cookie);
        var response=await client.GetAsync("/account");
        Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);
        Assert.Contains("/account/login",response.Headers.Location!.ToString());
    }

    private sealed class TestStore : WebApplicationFactory<Program>
    {
        private readonly string database = Guid.NewGuid().ToString("N");
        private readonly Microsoft.EntityFrameworkCore.Storage.InMemoryDatabaseRoot root = new();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_,config)=>config.AddInMemoryCollection(new Dictionary<string,string?> {
                ["Hangfire:Enabled"]="false",["SallyMail:Host"]="",["SallyAdmin:Email"]="",["SallyAdmin:PasswordHash"]=""
            }));
            builder.ConfigureServices(services=>{
                services.RemoveAll<ShopDbContext>();
                services.AddScoped(_=>new ShopDbContext(new DbContextOptionsBuilder<ShopDbContext>()
                    .UseInMemoryDatabase(database,root).Options));
                services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
            });
        }
    }
}
