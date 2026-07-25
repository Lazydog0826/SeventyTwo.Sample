using SeventyTwo.Sample.Domain.Orders;

namespace SeventyTwo.Sample.Domain.Tests;

public sealed class OrderTests
{
    [Fact]
    public void Create_ShouldCalculateOrderAmountAndItemCount()
    {
        var createdAt = new DateTime(2026, 7, 25, 0, 0, 0, DateTimeKind.Utc);
        var order = new Order(
            1,
            "SO1",
            2,
            3,
            [
                new OrderItemDraft(4, 5, "SKU-1", "商品一", 2, 10.5m),
                new OrderItemDraft(6, 7, "SKU-2", "商品二", 1, 20m),
            ],
            createdAt
        );

        Assert.Equal(3, order.ItemCount);
        Assert.Equal(41m, order.TotalAmount);
        Assert.Equal(41m, order.PayableAmount);
        Assert.Equal(2, order.Items.Count);
        Assert.Equal([1, 2], order.Items.Select(item => item.LineNo));
    }

    [Fact]
    public void Create_WithoutItems_ShouldThrowDomainException()
    {
        var createdAt = new DateTime(2026, 7, 25, 0, 0, 0, DateTimeKind.Utc);

        var exception = Assert.Throws<OrderDomainException>(() =>
            new Order(1, "SO1", 2, 3, [], createdAt)
        );

        Assert.Equal("订单至少包含一条明细", exception.Message);
    }

    [Fact]
    public void Create_WithNonPositiveQuantity_ShouldThrowDomainException()
    {
        var createdAt = new DateTime(2026, 7, 25, 0, 0, 0, DateTimeKind.Utc);

        var exception = Assert.Throws<OrderDomainException>(() =>
            new Order(
                1,
                "SO1",
                2,
                3,
                [new OrderItemDraft(4, 5, "SKU-1", "商品一", 0, 10.5m)],
                createdAt
            )
        );

        Assert.Equal("购买数量必须大于 0", exception.Message);
    }
}
