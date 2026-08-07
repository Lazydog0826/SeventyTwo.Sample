namespace SeventyTwo.Sample.Domain.Wallets;

public interface IWalletRepository
{
    Task<bool> TryRegisterBalanceChangeAsync(Guid requestNo, CancellationToken cancellationToken);

    Task EnsureChangeLocksAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken);

    Task<IReadOnlyList<Wallet>> GetForBalanceChangeAsync(
        Guid customerId,
        IReadOnlyCollection<WalletCurrency> walletCurrencies,
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken
    );

    Task SaveBalanceChangeAsync(
        Guid requestNo,
        IReadOnlyCollection<Wallet> newWallets,
        IReadOnlyCollection<Wallet> changedWallets,
        IReadOnlyCollection<WalletBalanceChange> changes,
        CancellationToken cancellationToken
    );
}
