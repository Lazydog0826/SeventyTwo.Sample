using SeventyTwo.Sample.Domain.Wallets;

namespace SeventyTwo.Sample.Application.Wallets.BalanceChange;

public sealed record BalanceChangeInput(
    long CustomerId,
    string RequestNo,
    IReadOnlyCollection<BalanceChangeDetailInput> Details
);

public sealed record BalanceChangeDetailInput(WalletCurrency Currency, WalletChangeType ChangeType, decimal Amount);

public sealed class BalanceChangeCommand
{
    public BalanceChangeCommand(
        long customerId,
        string requestNo,
        IReadOnlyCollection<BalanceChangeDetailCommand> details
    )
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

        if (details.Count == 0)
        {
            throw new WalletDomainException("余额变更明细不能为空");
        }

        foreach (var detail in details)
        {
            if (!Enum.IsDefined(detail.WalletType))
            {
                throw new WalletDomainException("钱包类型无效");
            }

            if (!Enum.IsDefined(detail.ChangeType))
            {
                throw new WalletDomainException("钱包变更类型无效");
            }

            if (detail.Amount.IsZero)
            {
                throw new WalletDomainException("余额变更金额必须大于 0");
            }
        }

        CustomerId = customerId;
        RequestNo = requestNo;
        Details = details.ToList().AsReadOnly();
    }

    public long CustomerId { get; }

    public string RequestNo { get; }

    public IReadOnlyList<BalanceChangeDetailCommand> Details { get; }
}

public sealed record BalanceChangeDetailCommand(WalletCurrency WalletType, WalletChangeType ChangeType, Money Amount);
