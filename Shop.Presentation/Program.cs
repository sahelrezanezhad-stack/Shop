using System.Net;
using System.Threading.RateLimiting;
using Hangfire;
using Hangfire.SqlServer;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Shop.Application;
using Shop.Infrastructure;
using Shop.Presentation.Email;
using Shop.Presentation.Middleware;
using Shop.Domain.Entities;
using Microsoft.AspNetCore.DataProtection;
using Shop.Presentation.Security;

namespace Shop.Presentation;
public class Program
{
    public static void Main(string[] args)
    {
        var builder=WebApplication.CreateBuilder(args);
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddApplication();
        builder.Services.AddControllersWithViews();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.Configure<SallyAdminOptions>(builder.Configuration.GetSection("SallyAdmin"));
        builder.Services.AddScoped<IPasswordHasher<SallyAdminOptions>, PasswordHasher<SallyAdminOptions>>();
        builder.Services.AddScoped<SallyCustomerCookieEvents>();
        builder.Services.AddScoped<SallyAdminCookieEvents>();
        builder.Services.AddSingleton<SallyPasswordResetTokens>();
        // Customer and administrator credentials and cookies are independent.
        builder.Services.AddAuthentication("SallyCookie")
            .AddCookie("SallyCookie", c=>{
                c.Cookie.Name="sally_auth";c.Cookie.HttpOnly=true;
                c.Cookie.SecurePolicy=builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
                c.Cookie.SameSite=SameSiteMode.Lax;
                c.LoginPath="/account/login";c.AccessDeniedPath="/account/login";
                c.SlidingExpiration=false;c.ExpireTimeSpan=TimeSpan.FromHours(8);
                c.EventsType=typeof(SallyCustomerCookieEvents);
            })
            .AddCookie(SallySecurity.AdminScheme, c=>{
                c.Cookie.Name="sally_admin_auth";c.Cookie.HttpOnly=true;c.Cookie.SameSite=SameSiteMode.Strict;
                c.Cookie.SecurePolicy=builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
                c.LoginPath="/admin/login";c.AccessDeniedPath="/admin/denied";
                c.SlidingExpiration=false;c.ExpireTimeSpan=TimeSpan.FromMinutes(20);
                c.EventsType=typeof(SallyAdminCookieEvents);
            });
        builder.Services.AddAuthorization(c=>c.AddPolicy(SallySecurity.AdminPolicy,
            p=>p.AddAuthenticationSchemes(SallySecurity.AdminScheme).RequireAuthenticatedUser().RequireRole("Administrator")));
        builder.Services.AddScoped<IPasswordHasher<SallyAccount>,PasswordHasher<SallyAccount>>();
        var protection=builder.Services.AddDataProtection().SetApplicationName("SALLY-STORE");
        var keyPath=builder.Configuration["SallySecurity:DataProtectionKeysPath"];
        if(!string.IsNullOrWhiteSpace(keyPath))protection.PersistKeysToFileSystem(new DirectoryInfo(keyPath));
        builder.Services.AddRateLimiter(c=>{
            c.RejectionStatusCode=429;
            c.AddPolicy("account-writes", ctx=>RateLimitPartition.GetFixedWindowLimiter(
                ctx.Connection.RemoteIpAddress?.ToString()??"unknown",
                _=>new FixedWindowRateLimiterOptions { PermitLimit=6,Window=TimeSpan.FromMinutes(10),
                    QueueLimit=0,AutoReplenishment=true }));
            c.AddPolicy("admin-login", ctx=>RateLimitPartition.GetFixedWindowLimiter(
                ctx.Connection.RemoteIpAddress?.ToString()??"unknown",
                _=>new FixedWindowRateLimiterOptions { PermitLimit=5,Window=TimeSpan.FromMinutes(15),QueueLimit=0,AutoReplenishment=true }));
            c.AddPolicy("checkout-writes", ctx=>RateLimitPartition.GetFixedWindowLimiter(
                ctx.Connection.RemoteIpAddress?.ToString()??"unknown",
                _=>new FixedWindowRateLimiterOptions { PermitLimit=15,Window=TimeSpan.FromMinutes(1),QueueLimit=0,AutoReplenishment=true }));
            c.AddPolicy("cart-requests", ctx=>RateLimitPartition.GetFixedWindowLimiter(
                ctx.Connection.RemoteIpAddress?.ToString()??"unknown",
                _=>new FixedWindowRateLimiterOptions { PermitLimit=60,Window=TimeSpan.FromMinutes(1),QueueLimit=0,AutoReplenishment=true }));
        });
        builder.Services.Configure<SallyMailOptions>(builder.Configuration.GetSection("SallyMail"));
        builder.Services.AddScoped<SallyEmailDispatcher>();
        var hangfireEnabled=builder.Configuration.GetValue<bool>("Hangfire:Enabled");
        if(hangfireEnabled)
        {
            var connection=builder.Configuration.GetConnectionString("DefaultConnection")
                ??throw new InvalidOperationException("ShopDb connection string missing");
            builder.Services.AddHangfire(config=>config.UseSqlServerStorage(connection,
                new SqlServerStorageOptions { PrepareSchemaIfNecessary=false,
                    SlidingInvisibilityTimeout=TimeSpan.FromMinutes(5), QueuePollInterval=TimeSpan.FromSeconds(15) }));
            builder.Services.AddHangfireServer();
        }
        else builder.Services.AddHostedService<SallyEmailWorker>();
        var app=builder.Build();
        if(app.Environment.IsDevelopment()) {app.UseSwagger();app.UseSwaggerUI();}
        else app.UseHsts();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.Use(async (context,next)=>{
            context.Response.Headers["X-Content-Type-Options"]="nosniff";
            context.Response.Headers["X-Frame-Options"]="DENY";
            context.Response.Headers["Referrer-Policy"]="no-referrer";
            context.Response.Headers["Permissions-Policy"]="camera=(), microphone=(), geolocation=()";
            await next(context);
        });
        app.UseHttpsRedirection();app.UseStaticFiles();app.UseRouting();
        app.UseRateLimiter();app.UseAuthentication();app.UseAuthorization();
        if(hangfireEnabled)
        {
            var manager=app.Services.GetRequiredService<IRecurringJobManager>();
            manager.AddOrUpdate<SallyEmailDispatcher>("sally-email-outbox",
                x=>x.ProcessPendingAsync(CancellationToken.None),Cron.Minutely());
            // Dashboard only in development AND only from local loopback, never public production.
            if(app.Environment.IsDevelopment())
                app.UseHangfireDashboard("/hangfire",new DashboardOptions {
                    Authorization=new[] {new LocalOnlyDashboardAuthorization()}
                });
        }
        app.MapControllers();
        app.MapControllerRoute("default","{controller=Home}/{action=Index}/{id?}");
        app.Run();
    }
}
internal sealed class LocalOnlyDashboardAuthorization : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var http=context.GetHttpContext();
        return http.Connection.RemoteIpAddress is IPAddress address && IPAddress.IsLoopback(address);
    }
}
