using Mapster;
using Microsoft.AspNetCore.Mvc;
using SeventyTwo.InfraKit.Core;
using SeventyTwo.Sample.Application.Wallets;
using SeventyTwo.Sample.Application.Wallets.BalanceChange;
using SeventyTwo.Sample.WebApi.Contracts.Wallets;

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
    public async Task<IActionResult> BalanceChange(BalanceChangeRequest request, CancellationToken cancellationToken)
    {
        await walletApplication.BalanceChangeAsync(request.Adapt<BalanceChangeInput>(), cancellationToken);
        return WebApiResponse.Operate(message: MessageKeys.Common.Success);
    }
}
