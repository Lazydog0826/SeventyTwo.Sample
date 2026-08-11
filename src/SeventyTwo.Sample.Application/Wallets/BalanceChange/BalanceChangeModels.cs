using SeventyTwo.Sample.Domain.Wallets;

namespace SeventyTwo.Sample.Application.Wallets.BalanceChange;

public sealed record BalanceChangeInput(
    Guid CustomerId,
    Guid RequestNo,
    IReadOnlyCollection<BalanceChangeDetailInput> Details
);

public sealed record BalanceChangeDetailInput(WalletCurrency Currency, WalletChangeType ChangeType, decimal Amount);

public sealed class BalanceChangeCommand
{
    public BalanceChangeCommand(
        Guid customerId,
        Guid requestNo,
        IReadOnlyCollection<BalanceChangeDetailCommand> details
    )
    {
        if (customerId == Guid.Empty)
        {
            throw new WalletDomainException(MessageKeys.Wallets.CustomerIdRequired);
        }

        if (requestNo == Guid.Empty)
        {
            throw new WalletDomainException(MessageKeys.Wallets.RequestNoRequired);
        }

        if (details.Count == 0)
        {
            throw new WalletDomainException(MessageKeys.Wallets.ChangeItemsRequired);
        }

        foreach (var detail in details)
        {
            if (!Enum.IsDefined(detail.WalletType))
            {
                throw new WalletDomainException(MessageKeys.Wallets.TypeInvalid);
            }

            if (!Enum.IsDefined(detail.ChangeType))
            {
                throw new WalletDomainException(MessageKeys.Wallets.ChangeTypeInvalid);
            }

            if (detail.Amount.IsZero)
            {
                throw new WalletDomainException(MessageKeys.Wallets.ChangeAmountMustBePositive);
            }
        }

        CustomerId = customerId;
        RequestNo = requestNo;
        Details = details.ToList().AsReadOnly();
    }

    public Guid CustomerId { get; }

    public Guid RequestNo { get; }

    public IReadOnlyList<BalanceChangeDetailCommand> Details { get; }
}

public sealed record BalanceChangeDetailCommand(WalletCurrency WalletType, WalletChangeType ChangeType, Money Amount);
