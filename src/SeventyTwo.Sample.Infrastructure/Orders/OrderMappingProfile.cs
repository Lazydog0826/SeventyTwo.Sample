using AutoMapper;
using SeventyTwo.Sample.Domain.Orders;

namespace SeventyTwo.Sample.Infrastructure.Orders;

public sealed class OrderMappingProfile : Profile
{
    public OrderMappingProfile()
    {
        CreateMap<OrderRecord, Order>()
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
