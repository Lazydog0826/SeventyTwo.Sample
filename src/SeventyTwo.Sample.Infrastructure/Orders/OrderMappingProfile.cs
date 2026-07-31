using Mapster;
using SeventyTwo.Sample.Domain.Orders;

namespace SeventyTwo.Sample.Infrastructure.Orders;

public sealed class OrderMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<OrderRecord, Order>()
            .ConstructUsing(x => new Order(
                x.Id,
                x.OrderNo,
                x.CustomerId,
                x.WarehouseId,
                x.OrderType,
                x.OrderStatus,
                x.ReceiverName,
                x.ReceiverPhone,
                x.Province,
                x.City,
                x.District,
                x.DetailAddress,
                x.Remark,
                x.Items.Adapt<List<OrderItem>>()
            ));

        config
            .NewConfig<OrderItemRecord, OrderItem>()
            .ConstructUsing(x => new OrderItem(
                x.Id,
                x.OrderId,
                x.LineNo,
                x.ProductId,
                x.ProductName,
                x.Unit,
                x.Quantity,
                x.UnitPrice,
                x.ShippedQuantity,
                x.ReturnedQuantity,
                x.Remark
            ));
    }
}
