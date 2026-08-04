// ReSharper disable ConvertIfStatementToSwitchStatement

namespace SeventyTwo.Sample.Domain.Wallets;

public class Wallet : AggregateRoot
{
    public Wallet(Guid id, string customerId, WalletCurrency walletType, Money balance)
    {
        if (id == Guid.Empty)
        {
            throw new WalletDomainException("钱包 ID 不能为空");
        }

        if (string.IsNullOrWhiteSpace(customerId))
        {
            throw new WalletDomainException("客户 ID 不能为空");
        }

        if (!Enum.IsDefined(walletType))
        {
            throw new WalletDomainException("钱包类型无效");
        }

        Id = id;
        CustomerId = customerId;
        WalletType = walletType;
        Balance = balance;
    }

    public string CustomerId { get; private set; }

    public WalletCurrency WalletType { get; private set; }

    public Money Balance { get; private set; }

    public WalletBalanceChange ChangeBalance(Money amount, WalletChangeType changeType)
    {
        if (amount.IsZero)
        {
            throw new WalletDomainException("余额变更金额必须大于 0");
        }

        var beforeBalance = Balance;
        Balance = changeType switch
        {
            WalletChangeType.Increase => Balance.Add(amount),
            WalletChangeType.Decrease => Balance.Subtract(amount),
            _ => throw new ArgumentOutOfRangeException(nameof(changeType), changeType, null),
        };

        return new WalletBalanceChange(Id, changeType, amount, beforeBalance, Balance);
    }
}
