// ReSharper disable MemberCanBeMadeStatic.Global
namespace SeventyTwo.Sample.Domain.Wallets;

public sealed record WalletBalanceChange(
    long WalletId,
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
        long customerId,
        IReadOnlyCollection<Wallet> wallets,
        IReadOnlyCollection<WalletBalanceChangeRequest> requests,
        Func<long> nextWalletId
    )
    {
        if (customerId <= 0)
        {
            throw new WalletDomainException("客户 ID 必须大于 0");
        }

        if (requests.Count == 0)
        {
            throw new WalletDomainException("余额变更明细不能为空");
        }

        var walletDictionary = new Dictionary<WalletCurrency, Wallet>();
        foreach (var wallet in wallets)
        {
            if (wallet.CustomerId != customerId)
            {
                throw new WalletDomainException("钱包不属于当前客户");
            }

            if (!walletDictionary.TryAdd(wallet.WalletType, wallet))
            {
                throw new WalletDomainException("客户存在重复的钱包类型");
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
                throw new WalletDomainException("钱包类型无效");
            }

            if (!Enum.IsDefined(request.ChangeType))
            {
                throw new WalletDomainException("钱包变更类型无效");
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
