namespace SeventyTwo.Sample.Application.Orders;

public interface IRandomOrderDataService
{
    Task AddAsync(int count, CancellationToken cancellationToken);
}
