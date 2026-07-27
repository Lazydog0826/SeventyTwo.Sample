using SeventyTwo.InfraKit.Core.DomainAggregateRoot;
using SeventyTwo.Sample.Domain.Wallets;
using SqlSugar;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SeventyTwo.Sample.Infrastructure.Wallets;

[SugarTable("wallet_record")]
internal sealed class WalletRecord : BaseEntity
{
    /// <summary>
    /// 客户 ID。
    /// </summary>
    [SugarColumn(ColumnName = "customer_id")]
    public long CustomerId { get; init; }

    /// <summary>
    /// 钱包币种。
    /// </summary>
    [SugarColumn(ColumnName = "currency")]
    public WalletCurrency Currency { get; init; }

    /// <summary>
    /// 当前余额。
    /// </summary>
    [SugarColumn(ColumnName = "balance_amount")]
    public decimal BalanceAmount { get; set; }
}

[SugarTable("wallet_change_record")]
internal sealed class WalletChangeRecord
{
    /// <summary>
    /// 钱包变更记录 ID。
    /// </summary>
    [SugarColumn(ColumnName = "change_id", IsPrimaryKey = true)]
    public long ChangeId { get; set; }

    /// <summary>
    /// 业务请求号。
    /// </summary>
    [SugarColumn(ColumnName = "request_no")]
    public string RequestNo { get; set; } = string.Empty;

    /// <summary>
    /// 钱包 ID。
    /// </summary>
    [SugarColumn(ColumnName = "wallet_id")]
    public long WalletId { get; set; }

    /// <summary>
    /// 钱包变更类型。
    /// </summary>
    [SugarColumn(ColumnName = "change_type")]
    public WalletChangeType ChangeType { get; set; }

    /// <summary>
    /// 本次变更金额。
    /// </summary>
    [SugarColumn(ColumnName = "amount")]
    public decimal Amount { get; set; }

    /// <summary>
    /// 变更前余额。
    /// </summary>
    [SugarColumn(ColumnName = "before_balance_amount")]
    public decimal BeforeBalanceAmount { get; set; }

    /// <summary>
    /// 变更后余额。
    /// </summary>
    [SugarColumn(ColumnName = "after_balance_amount")]
    public decimal AfterBalanceAmount { get; set; }

    /// <summary>
    /// 变更时间。
    /// </summary>
    [SugarColumn(ColumnName = "changed_at")]
    public DateTimeOffset ChangedAt { get; set; }
}

[SugarTable("wallet_change_request")]
internal sealed class WalletChangeRequest
{
    /// <summary>
    /// 业务请求号（唯一约束）。
    /// </summary>
    [SugarColumn(ColumnName = "request_no", IsPrimaryKey = true, Length = 255)]
    public string RequestNo { get; set; } = string.Empty;

    /// <summary>
    /// 请求时间。
    /// </summary>
    [SugarColumn(ColumnName = "request_at")]
    public DateTimeOffset RequestAt { get; set; }
}

[SugarTable("wallet_change_lock")]
internal sealed class WalletChangeLock
{
    /// <summary>
    /// 锁KEY
    /// </summary>
    [SugarColumn(ColumnName = "lock_key", IsPrimaryKey = true, Length = 255)]
    public string LockKey { get; init; } = string.Empty;
}
