using SeventyTwo.Sample.Common.MessageKeys;
using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.Wallets;

namespace SeventyTwo.Sample.Domain.Tests;

public sealed class WalletTests
{
    [Fact]
    public void ChangeBalance_ShouldReturnCompleteChangeAndUpdateBalance()
    {
        var wallet = new Wallet(Guid.CreateVersion7(), Guid.CreateVersion7(), WalletCurrency.CNY, new Money(10m));

        var change = wallet.ChangeBalance(new Money(2.5m), WalletChangeType.Decrease);

        Assert.Equal(wallet.Id, change.WalletId);
        Assert.Equal(WalletChangeType.Decrease, change.ChangeType);
        Assert.Equal(new Money(2.5m), change.Amount);
        Assert.Equal(new Money(10m), change.BeforeBalance);
        Assert.Equal(new Money(7.5m), change.AfterBalance);
        Assert.Equal(new Money(7.5m), wallet.Balance);
    }

    [Fact]
    public void ChangeBalance_WithInsufficientBalance_ShouldNotChangeWallet()
    {
        var wallet = new Wallet(Guid.CreateVersion7(), Guid.CreateVersion7(), WalletCurrency.CNY, new Money(10m));

        var exception = Assert.Throws<WalletDomainException>(() =>
            wallet.ChangeBalance(new Money(10.01m), WalletChangeType.Decrease)
        );

        Assert.Equal(MessageKeys.Wallets.InsufficientBalance, exception.Message);
        Assert.Equal(DomainErrorType.Conflict, exception.ErrorType);
        Assert.Equal(new Money(10m), wallet.Balance);
    }

    [Theory]
    [InlineData(-1, MessageKeys.Wallets.AmountMustNotBeNegative)]
    [InlineData(1.001, MessageKeys.Wallets.AmountScaleInvalid)]
    [InlineData(10000000000000000d, MessageKeys.Wallets.AmountOutOfRange)]
    public void Money_WithInvalidValue_ShouldFail(double value, string expectedMessage)
    {
        var exception = Assert.Throws<WalletDomainException>(() => new Money((decimal)value));

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void BatchChange_ShouldAggregateRequestsAndApplyIncreaseBeforeDecrease()
    {
        var customerId = Guid.CreateVersion7();
        var wallet = new Wallet(Guid.CreateVersion7(), customerId, WalletCurrency.CNY, new Money(5m));
        var service = new WalletBalanceChangeService();

        var batch = service.Change(
            customerId,
            [wallet],
            [
                new(WalletCurrency.CNY, WalletChangeType.Decrease, new Money(8m)),
                new(WalletCurrency.CNY, WalletChangeType.Increase, new Money(2m)),
                new(WalletCurrency.CNY, WalletChangeType.Increase, new Money(3m)),
            ],
            Guid.CreateVersion7
        );

        Assert.Empty(batch.NewWallets);
        Assert.Same(wallet, Assert.Single(batch.ChangedWallets));
        Assert.Equal(new Money(2m), wallet.Balance);
        Assert.Collection(
            batch.Changes,
            change =>
            {
                Assert.Equal(WalletChangeType.Increase, change.ChangeType);
                Assert.Equal(new Money(5m), change.Amount);
                Assert.Equal(new Money(5m), change.BeforeBalance);
                Assert.Equal(new Money(10m), change.AfterBalance);
            },
            change =>
            {
                Assert.Equal(WalletChangeType.Decrease, change.ChangeType);
                Assert.Equal(new Money(8m), change.Amount);
                Assert.Equal(new Money(10m), change.BeforeBalance);
                Assert.Equal(new Money(2m), change.AfterBalance);
            }
        );
    }
}
