// ReSharper disable NotAccessedPositionalProperty.Global
namespace SeventyTwo.Sample.Domain.Orders;

public interface IOrderRepository
{
    Task<OrderPage> GetPageAsync(OrderPageRequest request, CancellationToken cancellationToken);

    Task<OrderPage> GetPageByIdsAsync(OrderPageRequest request, CancellationToken cancellationToken);

    Task<OrderCursorPage> GetPageByCursorAsync(OrderPageRequest request, CancellationToken cancellationToken);
}

public sealed record OrderPage(IReadOnlyCollection<Order> Items, int Total, DateTimeOffset? LastDateTime, long? LastId);

public sealed record OrderCursorPage(
    IReadOnlyCollection<Order> Items,
    bool HasPrevious,
    bool HasNext,
    DateTimeOffset? FirstDateTime,
    long? FirstId,
    DateTimeOffset? LastDateTime,
    long? LastId
);
