// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ConvertIfStatementToSwitchStatement
namespace SeventyTwo.Sample.Domain.Wallets;

public sealed class BalanceChangeDraft
{
    private const decimal MaxAmount = 9999999999999999.99m;

    public BalanceChangeDraft(long customerId, string requestNo, List<BalanceChangeDetailDraft> drafts)
    {
        if (customerId <= 0)
        {
            throw new WalletDomainException("客户 ID 必须大于 0");
        }

        if (string.IsNullOrWhiteSpace(requestNo))
        {
            throw new WalletDomainException("请求号不能为空");
        }

        requestNo = requestNo.Trim();
        if (requestNo.Length > 255)
        {
            throw new WalletDomainException("请求号长度不能超过 255 个字符");
        }

        if (drafts is null || drafts.Count == 0)
        {
            throw new WalletDomainException("余额变更明细不能为空");
        }

        foreach (var draft in drafts)
        {
            if (draft is null)
            {
                throw new WalletDomainException("余额变更明细不能为空");
            }

            if (!Enum.IsDefined(draft.Currency))
            {
                throw new WalletDomainException("钱包币种无效");
            }

            if (!Enum.IsDefined(draft.ChangeType))
            {
                throw new WalletDomainException("钱包变更类型无效");
            }

            if (draft.Amount <= 0)
            {
                throw new WalletDomainException("余额变更金额必须大于 0");
            }

            if (draft.Amount > MaxAmount)
            {
                throw new WalletDomainException("余额变更金额超出范围");
            }

            if (decimal.Round(draft.Amount, 2) != draft.Amount)
            {
                throw new WalletDomainException("余额变更金额最多保留两位小数");
            }
        }

        CustomerId = customerId;
        RequestNo = requestNo;
        Drafts = drafts.ToList().AsReadOnly();
    }

    /// <summary>
    /// 客户 ID。
    /// </summary>
    public long CustomerId { get; }

    /// <summary>
    /// 业务请求号。
    /// </summary>
    public string RequestNo { get; }

    /// <summary>
    /// 余额变更明细。
    /// </summary>
    public IReadOnlyList<BalanceChangeDetailDraft> Drafts { get; }
}

public sealed record BalanceChangeDetailDraft(WalletCurrency Currency, WalletChangeType ChangeType, decimal Amount);

public sealed class WalletChangeRecordDraft
{
    /// <summary>
    /// 钱包变更记录 ID。
    /// </summary>
    public long ChangeId { get; set; }

    /// <summary>
    /// 业务请求号。
    /// </summary>
    public string RequestNo { get; set; } = string.Empty;

    /// <summary>
    /// 钱包 ID。
    /// </summary>
    public long WalletId { get; set; }

    /// <summary>
    /// 钱包变更类型。
    /// </summary>
    public WalletChangeType ChangeType { get; set; }

    /// <summary>
    /// 本次变更金额。
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// 变更前余额。
    /// </summary>
    public decimal BeforeBalanceAmount { get; set; }

    /// <summary>
    /// 变更后余额。
    /// </summary>
    public decimal AfterBalanceAmount { get; set; }

    /// <summary>
    /// 变更时间。
    /// </summary>
    public DateTimeOffset ChangedAt { get; set; }
}
