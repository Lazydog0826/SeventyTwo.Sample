// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SeventyTwo.Sample.Domain.Orders;

public sealed class Order : AggregateRoot
{
    public Order(
        Guid id,
        string orderNo,
        string customerId,
        string warehouseId,
        OrderType orderType,
        OrderStatus orderStatus,
        string? receiverName,
        string? receiverPhone,
        string? province,
        string? city,
        string? district,
        string? detailAddress,
        string? remark,
        IReadOnlyCollection<OrderItem> items
    )
    {
        Id = id;
        OrderNo = orderNo;
        CustomerId = customerId;
        WarehouseId = warehouseId;
        OrderType = orderType;
        OrderStatus = orderStatus;
        ReceiverName = receiverName;
        ReceiverPhone = receiverPhone;
        Province = province;
        City = city;
        District = district;
        DetailAddress = detailAddress;
        Remark = remark;
        Items = items;
    }

    public string OrderNo { get; private set; }

    public string CustomerId { get; private set; }

    public string WarehouseId { get; private set; }

    public OrderType OrderType { get; private set; }

    public OrderStatus OrderStatus { get; private set; }

    public string? ReceiverName { get; private set; }

    public string? ReceiverPhone { get; private set; }

    public string? Province { get; private set; }

    public string? City { get; private set; }

    public string? District { get; private set; }

    public string? DetailAddress { get; private set; }

    public string? Remark { get; private set; }

    public IReadOnlyCollection<OrderItem> Items { get; private set; }
}

public sealed class OrderItem(
    string id,
    Guid orderId,
    int lineNo,
    string productId,
    string productName,
    string? unit,
    int quantity,
    decimal unitPrice,
    int shippedQuantity,
    int returnedQuantity,
    string? remark
)
{
    public string Id { get; } = id;

    public Guid OrderId { get; } = orderId;

    public int LineNo { get; } = lineNo;

    public string ProductId { get; } = productId;

    public string ProductName { get; } = productName;

    public string? Unit { get; } = unit;

    public int Quantity { get; } = quantity;

    public decimal UnitPrice { get; } = unitPrice;

    public int ShippedQuantity { get; } = shippedQuantity;

    public int ReturnedQuantity { get; } = returnedQuantity;

    public string? Remark { get; } = remark;
}
