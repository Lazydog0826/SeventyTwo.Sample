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

    private Inventory CreateInventory(int quantity)
    {
        return new Inventory(
            1,
            2,
            3,
            4,
            "BATCH-1",
            new DateTimeOffset(2026, 7, 25, 0, 0, 0, TimeSpan.Zero),
            quantity
        );
    }
}
