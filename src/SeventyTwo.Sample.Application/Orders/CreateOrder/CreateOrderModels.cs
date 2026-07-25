namespace SeventyTwo.Sample.Application.Orders.CreateOrder;

public sealed record CreateOrderInput(
    long CustomerId,
    int WarehouseId,
    IReadOnlyCollection<CreateOrderItemInput> Items
);

public sealed record CreateOrderItemInput(
    long ProductId,
    long SkuId,
    string SkuCode,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);

public sealed record CreateOrderResult(long OrderId, string OrderNo, decimal PayableAmount);
