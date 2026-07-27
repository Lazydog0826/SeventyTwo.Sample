namespace SeventyTwo.Sample.Domain.Wallets;

public interface IWalletRepository
{
    /// <summary>
    /// 余额变更
    /// </summary>
    /// <param name="draft"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task BalanceChangeAsync(BalanceChangeDraft draft, CancellationToken cancellationToken);
}
