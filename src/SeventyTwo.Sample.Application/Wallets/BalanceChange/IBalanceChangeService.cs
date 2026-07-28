namespace SeventyTwo.Sample.Application.Wallets.BalanceChange;

public interface IBalanceChangeService
{
    /// <summary>
    /// 余额变更。
    /// </summary>
    /// <param name="command">余额变更命令。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task BalanceChangeAsync(BalanceChangeCommand command, CancellationToken cancellationToken);
}
