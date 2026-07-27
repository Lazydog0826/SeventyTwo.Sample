using SeventyTwo.Sample.Application.Wallets.BalanceChange;

namespace SeventyTwo.Sample.Application.Wallets;

public interface IWalletApplication
{
    /// <summary>
    /// 变更钱包余额。
    /// </summary>
    /// <param name="input">余额变更参数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task BalanceChangeAsync(BalanceChangeInput input, CancellationToken cancellationToken);
}
