using SeventyTwo.Sample.Domain.Inventories;

namespace SeventyTwo.Sample.Domain.Tests;

public sealed class InventoryTests
{
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

        var increaseException = Assert.Throws<InventoryDomainException>(() =>
            inventory.Increase(quantity)
        );
        var decreaseException = Assert.Throws<InventoryDomainException>(() =>
            inventory.Decrease(quantity)
        );

        Assert.Equal("库存变更数量必须大于 0", increaseException.Message);
        Assert.Equal("库存变更数量必须大于 0", decreaseException.Message);
    }

    [Fact]
    public void Decrease_WithInsufficientQuantity_ShouldNotChangeInventory()
    {
        var inventory = CreateInventory(10);

        var exception = Assert.Throws<InventoryDomainException>(() => inventory.Decrease(11));

        Assert.Equal("库存不足", exception.Message);
        Assert.Equal(10, inventory.Quantity);
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
            "REQUEST-1",
            [],
            [
                new InventoryDecreaseDraft(
                    "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                    "01ARZ3NDEKTSV4RRFFQ69G5FAZ",
                    "01ARZ3NDEKTSV4RRFFQ69G5FB0",
                    8
                ),
            ]
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
            "REQUEST-1",
            [
                new InventoryIncreaseDraft(
                    "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                    "01ARZ3NDEKTSV4RRFFQ69G5FAZ",
                    "01ARZ3NDEKTSV4RRFFQ69G5FB0",
                    5,
                    "BATCH-2",
                    new DateTimeOffset(2026, 7, 2, 0, 0, 0, TimeSpan.Zero)
                ),
            ],
            [
                new InventoryDecreaseDraft(
                    "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                    "01ARZ3NDEKTSV4RRFFQ69G5FAZ",
                    "01ARZ3NDEKTSV4RRFFQ69G5FB0",
                    7
                ),
            ]
        );
        var service = new InventoryChangeService();

        var batch = service.Change(
            [inventory],
            draft,
            Guid.CreateVersion7,
            new DateTimeOffset(2026, 7, 3, 0, 0, 0, TimeSpan.Zero)
        );

        var newInventory = Assert.Single(batch.NewInventories);
        Assert.Equal(0, newInventory.Quantity);
        Assert.Equal(8, inventory.Quantity);
        Assert.Single(batch.ChangedInventories);
        Assert.Equal(3, batch.Changes.Count);
    }

    private Inventory CreateInventory(int quantity)
    {
        return new Inventory(
            Guid.CreateVersion7(),
            "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            "01ARZ3NDEKTSV4RRFFQ69G5FAZ",
            "01ARZ3NDEKTSV4RRFFQ69G5FB0",
            "BATCH-1",
            new DateTimeOffset(2026, 7, 25, 0, 0, 0, TimeSpan.Zero),
            quantity
        );
    }

    private Inventory CreateInventory(Guid id, int quantity, DateTimeOffset inboundAt)
    {
        return new Inventory(
            id,
            "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            "01ARZ3NDEKTSV4RRFFQ69G5FAZ",
            "01ARZ3NDEKTSV4RRFFQ69G5FB0",
            $"BATCH-{id}",
            inboundAt,
            quantity
        );
    }
}
