using SeventyTwo.Sample.Common.MessageKeys;
using SeventyTwo.Sample.Domain.Inventories;

namespace SeventyTwo.Sample.Domain.Tests;

public sealed class InventoryTests
{
    private static readonly Guid ProductId = Guid.CreateVersion7();
    private static readonly Guid WarehouseId = Guid.CreateVersion7();
    private static readonly Guid LocationId = Guid.CreateVersion7();

    [Fact]
    public void Increase_ShouldReturnBeforeAndAfterQuantity()
    {
        var inventory = CreateInventory(10);

        var change = inventory.Increase(5);

        Assert.Equal(10, change.BeforeQuantity);
        Assert.Equal(15, change.AfterQuantity);
        Assert.Equal(15, inventory.Quantity);
    }

    [Fact]
    public void Decrease_ShouldReturnBeforeAndAfterQuantity()
    {
        var inventory = CreateInventory(10);

        var change = inventory.Decrease(4);

        Assert.Equal(10, change.BeforeQuantity);
        Assert.Equal(6, change.AfterQuantity);
        Assert.Equal(6, inventory.Quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Change_WithNonPositiveQuantity_ShouldThrowDomainException(int quantity)
    {
        var inventory = CreateInventory(10);

        var increaseException = Assert.Throws<InventoryDomainException>(() => inventory.Increase(quantity));
        var decreaseException = Assert.Throws<InventoryDomainException>(() => inventory.Decrease(quantity));

        Assert.Equal(MessageKeys.Inventories.ChangeQuantityMustBePositive, increaseException.Message);
        Assert.Equal(MessageKeys.Inventories.ChangeQuantityMustBePositive, decreaseException.Message);
    }

    [Fact]
    public void Decrease_WithInsufficientQuantity_ShouldNotChangeInventory()
    {
        var inventory = CreateInventory(10);

        var exception = Assert.Throws<InventoryDomainException>(() => inventory.Decrease(11));

        Assert.Equal(MessageKeys.Inventories.Insufficient, exception.Message);
        Assert.Equal(DomainErrorType.Conflict, exception.ErrorType);
        Assert.Equal(10, inventory.Quantity);
    }

    [Fact]
    public void Increase_WhenQuantityWouldOverflow_ShouldNotChangeInventory()
    {
        var inventory = CreateInventory(int.MaxValue);

        var exception = Assert.Throws<InventoryDomainException>(() => inventory.Increase(1));

        Assert.Equal(MessageKeys.Inventories.QuantityOutOfRange, exception.Message);
        Assert.Equal(int.MaxValue, inventory.Quantity);
    }

    [Fact]
    public void Change_ShouldDecreaseNewestInventoryFirst()
    {
        var olderInventoryId = Guid.CreateVersion7();
        var newerInventoryId = Guid.CreateVersion7();
        var olderInventory = CreateInventory(
            olderInventoryId,
            5,
            new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero)
        );
        var newerInventory = CreateInventory(
            newerInventoryId,
            6,
            new DateTimeOffset(2026, 7, 2, 0, 0, 0, TimeSpan.Zero)
        );
        var draft = new InventoryChangeDraft(
            Guid.CreateVersion7(),
            [],
            [new InventoryDecreaseDraft(ProductId, WarehouseId, LocationId, 8)]
        );
        var service = new InventoryChangeService();

        var batch = service.Change(
            [olderInventory, newerInventory],
            draft,
            Guid.CreateVersion7,
            new DateTimeOffset(2026, 7, 3, 0, 0, 0, TimeSpan.Zero)
        );

        Assert.Equal(3, olderInventory.Quantity);
        Assert.Equal(0, newerInventory.Quantity);
        Assert.Empty(batch.NewInventories);
        Assert.Equal(2, batch.ChangedInventories.Count);
        Assert.Collection(
            batch.Changes,
            change =>
            {
                Assert.Equal(newerInventoryId, change.InventoryId);
                Assert.Equal(6, change.Quantity);
            },
            change =>
            {
                Assert.Equal(olderInventoryId, change.InventoryId);
                Assert.Equal(2, change.Quantity);
            }
        );
    }

    [Fact]
    public void Change_ShouldIncludeNewInventoryInDecreaseAllocation()
    {
        var inventory = CreateInventory(
            Guid.CreateVersion7(),
            10,
            new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero)
        );
        var draft = new InventoryChangeDraft(
            Guid.CreateVersion7(),
            [
                new InventoryIncreaseDraft(
                    ProductId,
                    WarehouseId,
                    LocationId,
                    5,
                    "BATCH-2",
                    new DateTimeOffset(2026, 7, 2, 0, 0, 0, TimeSpan.Zero)
                ),
            ],
            [new InventoryDecreaseDraft(ProductId, WarehouseId, LocationId, 7)]
        );
        var service = new InventoryChangeService();
        var newInventoryId = Guid.CreateVersion7();
        var changedAt = new DateTimeOffset(2026, 7, 3, 0, 0, 0, TimeSpan.Zero);

        var batch = service.Change([inventory], draft, () => newInventoryId, changedAt);

        var newInventory = Assert.Single(batch.NewInventories);
        Assert.Equal(newInventoryId, newInventory.Id);
        Assert.Equal(0, newInventory.Quantity);
        Assert.Equal(8, inventory.Quantity);
        Assert.Same(inventory, Assert.Single(batch.ChangedInventories));
        Assert.Collection(
            batch.Changes,
            change =>
            {
                Assert.Equal(newInventoryId, change.InventoryId);
                Assert.Equal(InventoryChangeType.Increase, change.ChangeType);
                Assert.Equal(5, change.Quantity);
                Assert.Equal(0, change.BeforeQuantity);
                Assert.Equal(5, change.AfterQuantity);
                Assert.Equal(new DateTimeOffset(2026, 7, 2, 0, 0, 0, TimeSpan.Zero), change.ChangedAt);
            },
            change =>
            {
                Assert.Equal(newInventoryId, change.InventoryId);
                Assert.Equal(InventoryChangeType.Decrease, change.ChangeType);
                Assert.Equal(5, change.Quantity);
                Assert.Equal(5, change.BeforeQuantity);
                Assert.Equal(0, change.AfterQuantity);
                Assert.Equal(changedAt, change.ChangedAt);
            },
            change =>
            {
                Assert.Equal(inventory.Id, change.InventoryId);
                Assert.Equal(InventoryChangeType.Decrease, change.ChangeType);
                Assert.Equal(2, change.Quantity);
                Assert.Equal(10, change.BeforeQuantity);
                Assert.Equal(8, change.AfterQuantity);
                Assert.Equal(changedAt, change.ChangedAt);
            }
        );
    }

    private Inventory CreateInventory(int quantity)
    {
        return new Inventory(
            Guid.CreateVersion7(),
            ProductId,
            WarehouseId,
            LocationId,
            "BATCH-1",
            new DateTimeOffset(2026, 7, 25, 0, 0, 0, TimeSpan.Zero),
            quantity
        );
    }

    private Inventory CreateInventory(Guid id, int quantity, DateTimeOffset inboundAt)
    {
        return new Inventory(id, ProductId, WarehouseId, LocationId, $"BATCH-{id}", inboundAt, quantity);
    }
}
