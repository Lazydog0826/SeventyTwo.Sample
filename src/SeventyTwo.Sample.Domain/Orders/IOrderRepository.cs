namespace SeventyTwo.Sample.Domain.Orders;

public interface IOrderRepository
{
    Task<OrderPage> GetPageAsync(OrderPageRequest request, CancellationToken cancellationToken);

    Task<OrderPage> GetPageByIdsAsync(OrderPageRequest request, CancellationToken cancellationToken);

    Task<OrderPage> GetPageByCursorAsync(OrderPageRequest request, CancellationToken cancellationToken);
}

public sealed record OrderPage(IReadOnlyCollection<Order> Items, int Total);
