using Mapster;
using SeventyTwo.Sample.Domain.Orders;

namespace SeventyTwo.Sample.Application.Orders;

public sealed class OrderMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Order, OrderOutput>();
    }
}
