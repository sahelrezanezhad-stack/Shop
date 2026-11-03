using Shop.Domain.Repositories;
using Shop.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Shop.Domain.Entities;
using Shop.Infrastructure.Persistence;

namespace Shop.Infrastructure.Repositories;

/// <summary>Atomic unpaid guest checkout; sizes are independently stocked in SQL Server.</summary>
public sealed class CheckoutRepository : ICheckoutRepository
{
    private readonly ShopDbContext _db;
    public CheckoutRepository(ShopDbContext db) => _db = db;

    public async Task<OrderOutcome> PlaceAsync(Guid cartId, ValidatedCheckout contact, Guid? accountId, Func<IReadOnlyList<ShoppingCartLine>, string?> validateLines, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct);

        // Serialise submissions of this cart; the SQL Server X lock lasts to commit.
        var locked = await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [ShoppingCarts] SET [UpdatedAtUtc] = [UpdatedAtUtc] WHERE [Id] = {cartId}", ct);
        if (locked != 1) return OrderOutcome.Fail("The shopping bag could not be found.", "CartNotFound");

        var previous = await _db.Orders.AsNoTracking().Where(o => o.SourceCartId == cartId)
            .Select(o => new { o.Id, o.Status, o.TotalAmount, o.AccountId }).FirstOrDefaultAsync(ct);
        if (previous is not null)
        {
            if(previous.AccountId!=accountId)
                return OrderOutcome.Fail("This bag belongs to an earlier checkout. Refresh the page to continue.","CartConsumed");
            var oldLines = await _db.OrderItems.AsNoTracking().Include(i => i.Product)
                .Where(i => i.OrderId == previous.Id)
                .Select(i => new PlacedOrderLine(i.ProductId, i.Product.Name, i.Size,
                    i.Quantity, i.UnitPrice, i.UnitPrice * i.Quantity)).ToListAsync(ct);
            return OrderOutcome.Ok(new PlacedOrder(previous.Id, previous.Status, previous.TotalAmount, oldLines));
        }

        var cart = await _db.ShoppingCarts.Include(c => c.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(c => c.Id == cartId, ct);
        if (cart is null || cart.Lines.Count == 0)
            return OrderOutcome.Fail("Your shopping bag is empty.", "EmptyCart");

        var lines = cart.Lines.OrderBy(l => l.ProductId).ThenBy(l => l.Size).ToList();
        var validationError = validateLines(lines);
        if (validationError is not null) return OrderOutcome.Fail(validationError);
        decimal total = 0;
        var resultLines = new List<PlacedOrderLine>();

        foreach (var line in lines)
        {
            if (line.Product.Price <= 0) return OrderOutcome.Fail("The product price is invalid.");

            // Both updates are conditional and share the transaction: no overselling.
            var totalRows = await _db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE [Products] SET [StockQuantity] = [StockQuantity] - {line.Quantity} WHERE [Id] = {line.ProductId} AND [StockQuantity] >= {line.Quantity}", ct);
            if (totalRows != 1)
                return OrderOutcome.Fail("The product stock total does not match the size inventory.", "InventoryMismatch");
            // The stock update now holds the product lock. Reload the current price rather
            // than using a value that an administrator could have changed since cart loading.
            await _db.Entry(line.Product).ReloadAsync(ct);
            if (line.Product.Price <= 0) return OrderOutcome.Fail("The product price is invalid.");
            var sizeRows = await _db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE [ProductSizeStocks] SET [Quantity] = [Quantity] - {line.Quantity} WHERE [ProductId] = {line.ProductId} AND [Size] = {line.Size} AND [Quantity] >= {line.Quantity}", ct);
            if (sizeRows != 1)
                return OrderOutcome.Fail($"There is not enough stock for size {line.Size} of {line.Product.Name}.", "OutOfStock");

            var value = line.Product.Price * line.Quantity;
            total += value;
            resultLines.Add(new PlacedOrderLine(line.ProductId, line.Product.Name,
                line.Size, line.Quantity, line.Product.Price, value));
        }

        var names = contact.FullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var order = new Order
        {
            AccountId = accountId,
            Customer = new Customer { FirstName = names[0],
                LastName = names.Length > 1 ? names[1] : "", PhoneNumber = contact.Phone, Email = contact.Email },
            SourceCartId = cartId, OrderDate = DateTime.UtcNow, Status = "PendingPayment",
            TotalAmount = total, ShippingCity = contact.City,
            ShippingAddress = contact.Address, ShippingPostalCode = contact.PostalCode,
            Items = lines.Select(l => new OrderItem { ProductId = l.ProductId, Size = l.Size,
                Quantity = l.Quantity, UnitPrice = l.Product.Price }).ToList()
        };
        _db.Orders.Add(order);
        // For registered accounts we persist email intent in this order transaction.
        // Dispatch is asynchronous; a database outage cannot silently lose the intent.
        _db.ShoppingCartLines.RemoveRange(lines);
        await _db.SaveChangesAsync(ct);
        if (accountId.HasValue || !string.IsNullOrWhiteSpace(contact.Email))
        {
            _db.SallyEmailOutbox.Add(new SallyEmailOutbox {
                AccountId = accountId ?? Guid.Empty, OrderId = order.Id, Kind = "OrderPlaced"
            });
            await _db.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct);
        return OrderOutcome.Ok(new PlacedOrder(order.Id, order.Status, total, resultLines, true));
    }
}
