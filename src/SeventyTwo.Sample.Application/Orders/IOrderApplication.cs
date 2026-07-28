using SeventyTwo.Sample.Domain;

namespace SeventyTwo.Sample.Application.Orders;

public interface IOrderApplication
{
    Task<PageResponse<OrderOutput>> GetPageAsync(PageRequest request, CancellationToken cancellationToken);
}
