namespace SeventyTwo.Sample.Domain.Wallets;

public interface IWalletRepository
{
    Task<bool> TryRegisterBalanceChangeAsync(string requestNo, CancellationToken cancellationToken);

    Task<IReadOnlyList<Wallet>> GetForBalanceChangeAsync(
        string customerId,
        IReadOnlyCollection<WalletCurrency> walletCurrencies,
        CancellationToken cancellationToken
    );

    Task SaveBalanceChangeAsync(
        string requestNo,
        IReadOnlyCollection<Wallet> newWallets,
        IReadOnlyCollection<Wallet> changedWallets,
        IReadOnlyCollection<WalletBalanceChange> changes,
        CancellationToken cancellationToken
    );
}
