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

    public long CustomerId { get; }

    public string RequestNo { get; }

    public IReadOnlyList<BalanceChangeDetailDraft> Drafts { get; }
}

public sealed record BalanceChangeDetailDraft(WalletCurrency Currency, WalletChangeType ChangeType, decimal Amount);
