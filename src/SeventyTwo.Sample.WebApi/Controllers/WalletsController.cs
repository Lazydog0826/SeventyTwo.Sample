using Microsoft.AspNetCore.Mvc;
using SeventyTwo.Sample.Application.Wallets;
using SeventyTwo.Sample.Application.Wallets.BalanceChange;
using SeventyTwo.Sample.Domain.Wallets;

// ReSharper disable ClassNeverInstantiated.Global

namespace SeventyTwo.Sample.WebApi.Controllers;

/// <summary>
/// 钱包接口。
/// </summary>
/// <param name="walletApplication">钱包应用服务。</param>
[ApiController]
[Route("api/wallets")]
public sealed class WalletsController(IWalletApplication walletApplication) : ControllerBase
{
    /// <summary>
    /// 变更钱包余额。
    /// </summary>
    /// <param name="request">余额变更请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    [HttpPost("changes")]
    public Task BalanceChange(BalanceChangeRequest request, CancellationToken cancellationToken)
    {
        var details = request
            .Details.Select(x => new BalanceChangeDetailInput(x.Currency, x.ChangeType, x.Amount))
            .ToList();
        var input = new BalanceChangeInput(request.CustomerId, request.RequestNo, details);

        return walletApplication.BalanceChangeAsync(input, cancellationToken);
    }
}

/// <summary>
/// 钱包余额变更请求。
/// </summary>
/// <param name="CustomerId">客户标识。</param>
/// <param name="RequestNo">请求编号。</param>
/// <param name="Details">余额变更明细。</param>
public sealed record BalanceChangeRequest(
    Guid CustomerId,
    Guid RequestNo,
    IReadOnlyCollection<BalanceChangeDetailRequest> Details
);

/// <summary>
/// 钱包余额变更明细请求。
/// </summary>
/// <param name="Currency">币种。</param>
/// <param name="ChangeType">变更类型。</param>
/// <param name="Amount">变更金额。</param>
public sealed record BalanceChangeDetailRequest(WalletCurrency Currency, WalletChangeType ChangeType, decimal Amount);
