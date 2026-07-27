using SeventyTwo.InfraKit.Core.DomainAggregateRoot;

namespace SeventyTwo.Sample.Domain.Wallets;

public class Wallet : AggregateRoot
{
    public long CustomerId { get; set; }

    public WalletCurrency Currency { get; set; }

    public decimal BalanceAmount { get; set; }
}
