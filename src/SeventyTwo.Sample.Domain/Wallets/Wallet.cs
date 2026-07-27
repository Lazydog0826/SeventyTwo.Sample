using SeventyTwo.InfraKit.Core.DomainAggregateRoot;

// ReSharper disable ConvertIfStatementToSwitchStatement

namespace SeventyTwo.Sample.Domain.Wallets;

public class Wallet : AggregateRoot
{
    private const decimal MaxBalanceAmount = 9999999999999999.99m;

    public Wallet(long id, long customerId, WalletCurrency currency, decimal balanceAmount)
    {
        if (id <= 0)
        {
            throw new WalletDomainException("钱包 ID 必须大于 0");
        }

        if (customerId <= 0)
        {
            throw new WalletDomainException("客户 ID 必须大于 0");
        }

        if (!Enum.IsDefined(currency))
        {
            throw new WalletDomainException("钱包币种无效");
        }

        if (balanceAmount < 0)
        {
            throw new WalletDomainException("钱包余额不能小于 0");
        }

        if (balanceAmount > MaxBalanceAmount)
        {
            throw new WalletDomainException("钱包余额超出范围");
        }

        if (decimal.Round(balanceAmount, 2) != balanceAmount)
        {
            throw new WalletDomainException("钱包余额最多保留两位小数");
        }

        Id = id;
        CustomerId = customerId;
        Currency = currency;
        BalanceAmount = balanceAmount;
    }

    public long CustomerId { get; private set; }

    public WalletCurrency Currency { get; private set; }

    public decimal BalanceAmount { get; private set; }

    public (decimal oldBalanceAmount, decimal newBalanceAmount) ChangeBalance(
        decimal amount,
        WalletChangeType changeType
    )
    {
        if (amount <= 0)
        {
            throw new WalletDomainException("余额变更金额必须大于 0");
        }

        if (amount > MaxBalanceAmount)
        {
            throw new WalletDomainException("余额变更金额超出范围");
        }

        if (decimal.Round(amount, 2) != amount)
        {
            throw new WalletDomainException("余额变更金额最多保留两位小数");
        }

        var oldBalanceAmount = BalanceAmount;
        switch (changeType)
        {
            case WalletChangeType.Increase:
                if (BalanceAmount > MaxBalanceAmount - amount)
                {
                    throw new WalletDomainException("钱包余额超出范围");
                }
                BalanceAmount += amount;
                break;
            case WalletChangeType.Decrease:
                if (BalanceAmount - amount < 0)
                {
                    throw new WalletDomainException("余额不足");
                }
                BalanceAmount -= amount;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(changeType), changeType, null);
        }
        var newBalanceAmount = BalanceAmount;

        return (oldBalanceAmount, newBalanceAmount);
    }
}
