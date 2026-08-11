// ReSharper disable MemberCanBeMadeStatic.Global
namespace SeventyTwo.Sample.Domain.Wallets;

public sealed record WalletBalanceChange(
    Guid WalletId,
    WalletChangeType ChangeType,
    Money Amount,
    Money BeforeBalance,
    Money AfterBalance
);

public sealed record WalletBalanceChangeRequest(WalletCurrency WalletType, WalletChangeType ChangeType, Money Amount);

public sealed record WalletBalanceChangeBatch(
    IReadOnlyCollection<Wallet> NewWallets,
    IReadOnlyCollection<Wallet> ChangedWallets,
    IReadOnlyCollection<WalletBalanceChange> Changes
);

public sealed class WalletBalanceChangeService
{
    public WalletBalanceChangeBatch Change(
        Guid customerId,
        IReadOnlyCollection<Wallet> wallets,
        IReadOnlyCollection<WalletBalanceChangeRequest> requests,
        Func<Guid> nextWalletId
    )
    {
        if (customerId == Guid.Empty)
        {
            throw new WalletDomainException(MessageKeys.Wallets.CustomerIdRequired);
        }

        if (requests.Count == 0)
        {
            throw new WalletDomainException(MessageKeys.Wallets.ChangeItemsRequired);
        }

        var walletDictionary = new Dictionary<WalletCurrency, Wallet>();
        foreach (var wallet in wallets)
        {
            if (wallet.CustomerId != customerId)
            {
                throw new WalletDomainException(MessageKeys.Wallets.NotOwnedByCustomer);
            }

            if (!walletDictionary.TryAdd(wallet.WalletType, wallet))
            {
                throw new WalletDomainException(MessageKeys.Wallets.DuplicateTypeForCustomer);
            }
        }

        var aggregatedRequests = requests
            .GroupBy(x => new { x.WalletType, x.ChangeType })
            .Select(x => new WalletBalanceChangeRequest(
                x.Key.WalletType,
                x.Key.ChangeType,
                x.Aggregate(new Money(0), (total, item) => total.Add(item.Amount))
            ))
            .OrderBy(x => x.ChangeType == WalletChangeType.Increase ? 0 : 1)
            .ToList();

        var newWallets = new List<Wallet>();
        var changedWallets = new List<Wallet>();
        var changes = new List<WalletBalanceChange>();
        var changedWalletTypes = new HashSet<WalletCurrency>();

        foreach (var request in aggregatedRequests)
        {
            if (!Enum.IsDefined(request.WalletType))
            {
                throw new WalletDomainException(MessageKeys.Wallets.TypeInvalid);
            }

            if (!Enum.IsDefined(request.ChangeType))
            {
                throw new WalletDomainException(MessageKeys.Wallets.ChangeTypeInvalid);
            }

            if (!walletDictionary.TryGetValue(request.WalletType, out var wallet))
            {
                wallet = new Wallet(nextWalletId(), customerId, request.WalletType, new Money(0));
                walletDictionary.Add(request.WalletType, wallet);
                newWallets.Add(wallet);
                changedWalletTypes.Add(request.WalletType);
            }
            else if (changedWalletTypes.Add(request.WalletType))
            {
                changedWallets.Add(wallet);
            }

            changes.Add(wallet.ChangeBalance(request.Amount, request.ChangeType));
        }

        return new WalletBalanceChangeBatch(newWallets, changedWallets, changes);
    }
}
