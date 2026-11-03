using Shop.Application.Inventory.Commands;
using Shop.Application.Orders.Commands;
using Shop.Application.ShoppingCarts;
using Shop.Domain.Entities;
using Shop.Domain.Repositories;
using Xunit;

namespace Shop.Security.Tests;

public sealed class ManagementCommandTests
{
    [Fact]
    public async Task StaleInventoryFormCannotOverwriteNewStock()
    {
        var repository = new InventoryFake();
        var command = new InventoryCommands(repository);
        var changes = new[] { new InventoryChange("S", 0, 0), new InventoryChange("M", 10, 12),
            new InventoryChange("L", 0, 0), new InventoryChange("XL", 0, 0) };
        var result = await command.SetAsync(3003, 10, changes, default);
        Assert.False(result.Success);
        Assert.False(repository.Committed);
        Assert.Equal(8, repository.Product.StockQuantity);
        Assert.Equal(8, repository.Product.SizeStocks.Single().Quantity);
    }

    [Fact]
    public async Task SavingInventoryUpdatesTotalFromApprovedSizes()
    {
        var repository = new InventoryFake();
        var command = new InventoryCommands(repository);
        var changes = new[] { new InventoryChange("S", 0, 2), new InventoryChange("M", 8, 6),
            new InventoryChange("L", 0, 1), new InventoryChange("XL", 0, 1) };
        var result = await command.SetAsync(3003, 8, changes, default);
        Assert.True(result.Success);
        Assert.True(repository.Committed);
        Assert.Equal(10, repository.Product.StockQuantity);
        Assert.Equal(6, repository.Product.SizeStocks.Single(x => x.Size == "M").Quantity);
    }

    [Fact]
    public async Task RepeatedCancellationDoesNotRestoreStockTwice()
    {
        var repository = new OrderFake();
        var command = new OrderCommands(repository);
        Assert.True((await command.CancelUnpaidAsync(1, default)).Success);
        Assert.True((await command.CancelUnpaidAsync(1, default)).Success);
        Assert.Equal("Cancelled", repository.Order.Status);
        Assert.Equal(1, repository.RestoreCalls);
        Assert.Equal(1, repository.QueueCalls);
    }

    [Fact]
    public async Task PaidOrderCannotBeCancelledByUnpaidWorkflow()
    {
        var repository = new OrderFake();
        repository.Order.Status = "Paid";
        Assert.False((await new OrderCommands(repository).CancelUnpaidAsync(1, default)).Success);
        Assert.Equal(0, repository.RestoreCalls);
        Assert.Equal(0, repository.QueueCalls);
    }

    private sealed class InventoryFake : IInventoryRepository
    {
        public Product Product { get; } = new() { Id = 3003, Name = "CAMEL SUEDE JACKET", StockQuantity = 8,
            SizeStocks = new List<ProductSizeStock> { new() { Size = "M", Quantity = 8 } } };
        public bool Committed { get; private set; }
        public Task<IReadOnlyList<Product>> ListAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<Product>>(new[] { Product });
        public Task<T> UpdateLockedAsync<T>(int id, Func<Product?, CartMutation<T>> change, CancellationToken ct)
        {
            var result = change(Product);
            Committed = result.Commit;
            return Task.FromResult(result.Value);
        }
    }

    private sealed class OrderFake : IOrderRepository
    {
        public Order Order { get; } = new() { Id = 1, Status = "PendingPayment", SourceCartId = Guid.NewGuid(),
            Items = new List<OrderItem> { new() { ProductId = 3002, Size = "M", Quantity = 2 } } };
        public int RestoreCalls { get; private set; }
        public int QueueCalls { get; private set; }
        public Task<OrderPage> ListAsync(string? status, int page, CancellationToken ct)
            => Task.FromResult(new OrderPage(new[] { Order }, 1, 1));
        public Task<Order?> GetAsync(int id, CancellationToken ct) => Task.FromResult<Order?>(Order);
        public async Task<T> WithLockAsync<T>(int id, Func<Order?, Task<CartMutation<T>>> action, CancellationToken ct)
            => (await action(Order)).Value;
        public Task<bool> RestoreStockAsync(IReadOnlyList<OrderItem> items, CancellationToken ct)
        {
            RestoreCalls++;
            return Task.FromResult(true);
        }
        public void QueueCancellation(Order order) => QueueCalls++;
    }
}
