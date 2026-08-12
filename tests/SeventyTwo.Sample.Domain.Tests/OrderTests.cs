using SeventyTwo.Sample.Domain.Orders;

namespace SeventyTwo.Sample.Domain.Tests;

public sealed class OrderTests
{
    [Fact]
    public void Constructor_ShouldPreserveOrderAndItemData()
    {
        var orderId = Guid.CreateVersion7();
        var itemId = Guid.CreateVersion7();
        var customerId = Guid.CreateVersion7();
        var warehouseId = Guid.CreateVersion7();
        var productId = Guid.CreateVersion7();
        var item = new OrderItem(itemId, orderId, 1, productId, "商品", "件", 3, 4.5m, 2, 1, "明细备注");

        var order = new Order(
            orderId,
            "ORDER-1",
            customerId,
            warehouseId,
            OrderType.Sales,
            OrderStatus.Processing,
            "张三",
            "13800000000",
            "省",
            "市",
            "区",
            "详细地址",
            "订单备注",
            [item]
        );

        Assert.Equal(orderId, order.Id);
        Assert.Equal("ORDER-1", order.OrderNo);
        Assert.Equal(customerId, order.CustomerId);
        Assert.Equal(warehouseId, order.WarehouseId);
        Assert.Equal(OrderType.Sales, order.OrderType);
        Assert.Equal(OrderStatus.Processing, order.OrderStatus);
        Assert.Equal("张三", order.ReceiverName);
        Assert.Equal("13800000000", order.ReceiverPhone);
        Assert.Equal("省", order.Province);
        Assert.Equal("市", order.City);
        Assert.Equal("区", order.District);
        Assert.Equal("详细地址", order.DetailAddress);
        Assert.Equal("订单备注", order.Remark);
        var actualItem = Assert.Single(order.Items);
        Assert.Same(item, actualItem);
        Assert.Equal(itemId, actualItem.Id);
        Assert.Equal(orderId, actualItem.OrderId);
        Assert.Equal(1, actualItem.LineNo);
        Assert.Equal(productId, actualItem.ProductId);
        Assert.Equal("商品", actualItem.ProductName);
        Assert.Equal("件", actualItem.Unit);
        Assert.Equal(3, actualItem.Quantity);
        Assert.Equal(4.5m, actualItem.UnitPrice);
        Assert.Equal(2, actualItem.ShippedQuantity);
        Assert.Equal(1, actualItem.ReturnedQuantity);
        Assert.Equal("明细备注", actualItem.Remark);
    }
}
