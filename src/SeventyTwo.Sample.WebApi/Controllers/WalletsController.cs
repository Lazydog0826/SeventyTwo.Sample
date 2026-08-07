using Microsoft.AspNetCore.Mvc;
using SeventyTwo.Sample.Application.Wallets;
using SeventyTwo.Sample.Application.Wallets.BalanceChange;
using SeventyTwo.Sample.Domain.Wallets;

// ReSharper disable ClassNeverInstantiated.Global

namespace SeventyTwo.Sample.WebApi.Controllers;

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

public sealed record BalanceChangeRequest(
    Guid CustomerId,
    Guid RequestNo,
    IReadOnlyCollection<BalanceChangeDetailRequest> Details
);

public sealed record BalanceChangeDetailRequest(WalletCurrency Currency, WalletChangeType ChangeType, decimal Amount);
