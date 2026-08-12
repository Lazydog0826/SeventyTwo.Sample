using SeventyTwo.Sample.Common.MessageKeys;
using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.Orders;
using SeventyTwo.Sample.Infrastructure.Orders;

namespace SeventyTwo.Sample.ArchitectureTests;

public sealed class OrderMessageContractTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task RandomOrderDataService_ShouldRejectNonPositiveCountWithMessageKey(int count)
    {
        var service = new RandomOrderDataService(null!, null!);

        var exception = await Assert.ThrowsAsync<OrderDomainException>(() =>
            service.AddAsync(count, CancellationToken.None)
        );

        Assert.Equal(MessageKeys.Orders.RandomCountMustBePositive, exception.Message);
        Assert.Equal(DomainErrorType.Validation, exception.ErrorType);
    }
}
