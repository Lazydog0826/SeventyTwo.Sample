// ReSharper disable ConvertIfStatementToSwitchStatement

namespace SeventyTwo.Sample.Domain.Wallets;

public class Wallet : AggregateRoot
{
    public Wallet(Guid id, Guid customerId, WalletCurrency walletType, Money balance)
    {
        if (id == Guid.Empty)
        {
            throw new WalletDomainException(MessageKeys.Wallets.IdRequired);
        }

        if (customerId == Guid.Empty)
        {
            throw new WalletDomainException(MessageKeys.Wallets.CustomerIdRequired);
        }

        if (!Enum.IsDefined(walletType))
        {
            throw new WalletDomainException(MessageKeys.Wallets.TypeInvalid);
        }

        Id = id;
        Enable = true;
        Version = Guid.CreateVersion7();
        CustomerId = customerId;
        WalletType = walletType;
        Balance = balance;
    }

    public Guid CustomerId { get; private set; }

    public WalletCurrency WalletType { get; private set; }

    public Money Balance { get; private set; }

    public WalletBalanceChange ChangeBalance(Money amount, WalletChangeType changeType)
    {
        if (amount.IsZero)
        {
            throw new WalletDomainException(MessageKeys.Wallets.ChangeAmountMustBePositive);
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
