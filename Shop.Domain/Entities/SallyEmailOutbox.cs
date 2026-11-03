namespace Shop.Domain.Entities;

// Persisted in the SAME database transaction as the order or account creation.
public sealed class SallyEmailOutbox
{
    public long Id { get; set; }
    public Guid AccountId { get; set; }
    public int? OrderId { get; set; }
    public string Kind { get; set; } = ""; // VerifyEmail | OrderPlaced
    public string? ProtectedToken { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SentUtc { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}
