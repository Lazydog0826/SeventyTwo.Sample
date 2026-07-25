using SeventyTwo.Sample.Application.Abstractions;
using SeventyTwo.Sample.Application.Orders.CreateOrder;
using SeventyTwo.Sample.Domain.Orders;

namespace SeventyTwo.Sample.Application.Orders;

public sealed class OrderApplication(IIdGenerator idGenerator, IOrderRepository orderRepository)
    : IOrderApplication
{
    public async Task<CreateOrderResult> CreateAsync(
        CreateOrderInput input,
        CancellationToken cancellationToken
    )
    {
        var orderId = idGenerator.NextId();
        var orderNo = $"SO{orderId}";
        var createdAt = DateTime.UtcNow;
        var items = input.Items
            .Select(item =>
                new OrderItemDraft(
                    item.ProductId,
                    item.SkuId,
                    item.SkuCode,
                    item.ProductName,
                    item.Quantity,
                    item.UnitPrice
                )
            )
            .ToList();
        var order = new Order(
            orderId,
            orderNo,
            input.CustomerId,
            input.WarehouseId,
            items,
            createdAt
        );

        await orderRepository.AddAsync(order, cancellationToken);

        return new CreateOrderResult(order.Id, order.OrderNo, order.PayableAmount);
    }
}
