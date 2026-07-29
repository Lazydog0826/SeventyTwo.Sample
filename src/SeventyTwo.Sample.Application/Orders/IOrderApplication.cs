using SeventyTwo.Sample.Domain.Orders;

namespace SeventyTwo.Sample.Application.Orders;

public interface IOrderApplication
{
    Task<PageResponse<OrderOutput>> GetPageAsync(OrderPageRequest request, CancellationToken cancellationToken);

    Task<PageResponse<OrderOutput>> GetPageByIdsAsync(OrderPageRequest request, CancellationToken cancellationToken);

    Task<CursorPageResponse<OrderOutput>> GetPageByCursorAsync(
        OrderPageRequest request,
        CancellationToken cancellationToken
    );
}
