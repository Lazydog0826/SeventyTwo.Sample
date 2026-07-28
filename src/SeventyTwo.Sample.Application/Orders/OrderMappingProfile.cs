using AutoMapper;
using SeventyTwo.Sample.Domain.Orders;

namespace SeventyTwo.Sample.Application.Orders;

public sealed class OrderMappingProfile : Profile
{
    public OrderMappingProfile()
    {
        CreateMap<Order, OrderOutput>();
    }
}
