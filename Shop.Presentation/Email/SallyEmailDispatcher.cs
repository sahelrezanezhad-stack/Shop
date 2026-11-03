using Hangfire;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;
using Shop.Infrastructure.Persistence;
using Shop.Presentation.Security;
using Shop.Domain.Entities;
using System.Security.Cryptography;
using System.Text;

namespace Shop.Presentation.Email;

/// <summary>Processes the durable SQL outbox. Hangfire job arguments contain no customer data.</summary>
public sealed class SallyEmailDispatcher
{
    private readonly ShopDbContext _db;
    private readonly SallyMailOptions _opt;
    private readonly IDataProtector _protector;
    private readonly ILogger<SallyEmailDispatcher> _logger;
    private readonly SallyPasswordResetTokens _resetTokens;

    public SallyEmailDispatcher(
        ShopDbContext db,
        IOptions<SallyMailOptions> opt,
        IDataProtectionProvider protection,
        ILogger<SallyEmailDispatcher> logger, SallyPasswordResetTokens resetTokens)
    {
        _db = db;
        _opt = opt.Value;
        _protector = protection.CreateProtector("SALLY.EmailVerification.v1");
        _logger = logger;
        _resetTokens = resetTokens;
    }

    [DisableConcurrentExecution(120)]
    [AutomaticRetry(Attempts = 2)]
    public async Task ProcessPendingAsync(CancellationToken ct)
    {
        if (!_opt.IsConfigured)
        {
            _logger.LogInformation("SALLY SMTP is not configured. Pending email outbox records were left untouched.");
            return;
        }

        // A SQL Server session lock coordinates background workers across app instances.
        // It uses an existing system procedure and does not create schema or delete messages.
        await _db.Database.OpenConnectionAsync(ct);
        var connection = _db.Database.GetDbConnection();
        var acquired = false;
        try
        {
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "DECLARE @result int; EXEC @result = sys.sp_getapplock @Resource = 'SALLY.EmailOutbox.v1', @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = 0; SELECT @result;";
            acquired = Convert.ToInt32(await command.ExecuteScalarAsync(ct)) >= 0;
        }
        if (!acquired) return;
        var pending = await _db.SallyEmailOutbox
            .Where(x => x.SentUtc == null && x.Attempts < 10
                && ((x.Kind != "OrderPlaced" && x.Kind != "OrderCancelled") || x.AccountId == Guid.Empty
                    || _db.SallyAccounts.Any(a => a.Id == x.AccountId && a.EmailVerified)))
            .OrderBy(x => x.CreatedUtc)
            .Take(20)
            .ToListAsync(ct);

        foreach (var job in pending)
        {
            ct.ThrowIfCancellationRequested();
            var account = await _db.SallyAccounts.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == job.AccountId, ct);

            Order? guestOrder = null;
            if (job.AccountId == Guid.Empty && job.OrderId.HasValue && (job.Kind == "OrderPlaced" || job.Kind == "OrderCancelled"))
                guestOrder = await _db.Orders.AsNoTracking().Include(x => x.Customer)
                    .SingleOrDefaultAsync(x => x.Id == job.OrderId && x.AccountId == null, ct);
            var recipientEmail = account?.Email ?? guestOrder?.Customer.Email;
            var recipientName = account?.DisplayName ?? guestOrder?.Customer.FirstName ?? "there";
            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                job.LastError = "Account not found";
                job.Attempts = 10;
                await _db.SaveChangesAsync(ct);
                continue;
            }

            // Keep order notifications pending until the recipient has verified the current address.
            if ((job.Kind == "OrderPlaced" || job.Kind == "OrderCancelled") && account is not null && !account.EmailVerified)
                continue;

            try
            {
                string subject;
                string body;

                if (job.Kind == "VerifyEmail")
                {
                    if(account is null)throw new InvalidOperationException("Verification account missing");
                    if (account.EmailVerified)
                    {
                        job.SentUtc = DateTime.UtcNow;
                        job.LastError = null;
                        await _db.SaveChangesAsync(ct);
                        continue;
                    }

                    if (job.ProtectedToken is null || account.VerificationExpiresUtc is null ||
                        account.VerificationExpiresUtc < DateTime.UtcNow)
                    {
                        job.Attempts = 10;
                        job.LastError = "Verification link expired";
                        await _db.SaveChangesAsync(ct);
                        continue;
                    }

                    var rawToken = _protector.Unprotect(job.ProtectedToken);
                    var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
                    if (!SallySecurity.EqualsSecret(account.VerificationTokenHash, hash))
                    {
                        job.Attempts = 10; job.LastError = "Verification link superseded";
                        await _db.SaveChangesAsync(ct); continue;
                    }
                    var link = $"{_opt.PublicBaseUrl.TrimEnd('/')}/account/verify?id={account.Id}&token={Uri.EscapeDataString(rawToken)}";
                    subject = "Verify your SALLY email address";
                    body = $"Hi {account.DisplayName},\n\nPlease verify your SALLY account by opening this link:\n{link}\n\nThis link expires in 24 hours. If you did not create this account, you can ignore this email.\n\nSALLY";
                }
                else if(job.Kind == "ResetPassword")
                {
                    if(account is null || !account.EmailVerified || !_resetTokens.IsValid(account,job.ProtectedToken))
                    {
                        job.Attempts=10; job.LastError="Reset link expired or revoked";
                        await _db.SaveChangesAsync(ct); continue;
                    }
                    var link=$"{_opt.PublicBaseUrl.TrimEnd('/')}/account/reset-password?id={account.Id}&token={Uri.EscapeDataString(job.ProtectedToken!)}";
                    subject="Reset your SALLY password";
                    body=$"Hi {recipientName},\n\nTo choose a new password, open this link:\n{link}\n\nThe link expires in 30 minutes and can be used once. If you did not request it, you can ignore this email.\n\nSALLY";
                }
                else if ((job.Kind == "OrderPlaced" || job.Kind == "OrderCancelled") && job.OrderId.HasValue)
                {
                    var order = guestOrder;
                    if(order is null && account is not null)
                        order=await _db.Orders.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==job.OrderId.Value && x.AccountId==account.Id,ct);
                    if (order is null)
                    {
                        job.Attempts = 10;
                        job.LastError = "Order not found for account";
                        await _db.SaveChangesAsync(ct);
                        continue;
                    }

                    var cancelled=order.Status=="Cancelled";
                    subject = $"SALLY order #{order.Id} {(cancelled ? "cancelled" : "received")}";
                    body = $"Hi {recipientName},\n\nYour order #{order.Id} {(cancelled ? "has been cancelled" : "has been received")}.\n\nTotal: {order.TotalAmount.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("en-US"))} TOMAN\nStatus: {order.Status}\n\nNo online payment has been processed or confirmed by SALLY.\n\nThank you,\nSALLY";
                }
                else
                {
                    job.Attempts = 10;
                    job.LastError = "Unknown email message type";
                    await _db.SaveChangesAsync(ct);
                    continue;
                }

                var mail = new MimeMessage();
                mail.From.Add(new MailboxAddress(_opt.FromName, _opt.FromEmail));
                mail.To.Add(MailboxAddress.Parse(recipientEmail));
                mail.MessageId=$"sally-outbox-{job.Id}@{new System.Net.Mail.MailAddress(_opt.FromEmail).Host}";
                mail.Subject = subject;
                mail.Body = new TextPart("plain") { Text = body };

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(_opt.Host, _opt.Port, SecureSocketOptions.StartTls, ct);
                await smtp.AuthenticateAsync(_opt.Username, _opt.Password, ct);
                await smtp.SendAsync(mail, ct);

                // Persist acceptance before attempting a graceful SMTP disconnect. A disconnect
                // failure after SMTP accepted the message must not make this row eligible again.
                job.SentUtc = DateTime.UtcNow;
                job.LastError = null;
                job.ProtectedToken = null;
                await _db.SaveChangesAsync(ct);
                try
                {
                    await smtp.DisconnectAsync(true, ct);
                }
                catch (Exception disconnectError) when (disconnectError is not OperationCanceledException)
                {
                    _logger.LogDebug(disconnectError, "SMTP disconnect failed after message acceptance for outbox record {OutboxId}", job.Id);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                job.Attempts++;
                job.LastError = ex.GetType().Name;
                _logger.LogWarning(ex, "SALLY email delivery failed for outbox record {OutboxId}", job.Id);
            }

            await _db.SaveChangesAsync(ct);
        }
        }
        finally
        {
            if(acquired && connection.State == System.Data.ConnectionState.Open)
            {
                await using var release=connection.CreateCommand();
                release.CommandText="EXEC sys.sp_releaseapplock @Resource = 'SALLY.EmailOutbox.v1', @LockOwner = 'Session';";
                try { await release.ExecuteNonQueryAsync(CancellationToken.None); }
                catch(Exception ex) { _logger.LogWarning(ex,"Could not release email worker lock; closing the SQL connection."); }
            }
            await _db.Database.CloseConnectionAsync();
        }
    }
}
