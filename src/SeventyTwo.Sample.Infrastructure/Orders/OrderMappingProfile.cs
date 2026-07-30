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
                x.Remark
            ));
    }
}
