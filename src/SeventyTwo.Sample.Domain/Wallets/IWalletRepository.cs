namespace SeventyTwo.Sample.Domain.Wallets;

public interface IWalletRepository
{
    Task<bool> TryRegisterBalanceChangeAsync(string requestNo, CancellationToken cancellationToken);

    Task<IReadOnlyList<Wallet>> GetForBalanceChangeAsync(
        long customerId,
        IReadOnlyCollection<WalletCurrency> walletCurrencies,
        CancellationToken cancellationToken
    );

    Task SaveBalanceChangeAsync(
        IReadOnlyCollection<Wallet> newWallets,
        IReadOnlyCollection<Wallet> changedWallets,
        IReadOnlyCollection<WalletChangeRecordDraft> changeRecords,
        CancellationToken cancellationToken
    );
}
