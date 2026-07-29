using SeventyTwo.Sample.Domain.Orders;

namespace SeventyTwo.Sample.Application.Orders;

public interface IOrderApplication
{
    Task<PageResponse<OrderOutput>> GetPageAsync(OrderPageRequest request, CancellationToken cancellationToken);
}
