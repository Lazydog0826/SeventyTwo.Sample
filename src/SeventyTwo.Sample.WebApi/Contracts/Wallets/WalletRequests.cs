using SeventyTwo.Sample.Domain.Wallets;

// ReSharper disable ClassNeverInstantiated.Global
namespace SeventyTwo.Sample.WebApi.Contracts.Wallets;

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
