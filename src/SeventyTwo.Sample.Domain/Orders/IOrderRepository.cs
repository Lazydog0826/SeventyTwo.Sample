namespace SeventyTwo.Sample.Domain.Orders;

public interface IOrderRepository
{
    Task<OrderPage> GetPageAsync(PageRequest request, CancellationToken cancellationToken);
}

public sealed record OrderPage(IReadOnlyCollection<Order> Items, int Total);
