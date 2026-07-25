using SeventyTwo.Sample.Application.Orders.CreateOrder;

namespace SeventyTwo.Sample.Application.Orders;

public interface IOrderApplication
{
    Task<CreateOrderResult> CreateAsync(
        CreateOrderInput input,
        CancellationToken cancellationToken
    );
}
