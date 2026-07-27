using SeventyTwo.Sample.Domain.Wallets;

namespace SeventyTwo.Sample.Application.Wallets.BalanceChange;

public interface IBalanceChangeService
{
    /// <summary>
    /// 余额变更。
    /// </summary>
    /// <param name="draft">余额变更草稿。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task BalanceChangeAsync(BalanceChangeDraft draft, CancellationToken cancellationToken);
}
