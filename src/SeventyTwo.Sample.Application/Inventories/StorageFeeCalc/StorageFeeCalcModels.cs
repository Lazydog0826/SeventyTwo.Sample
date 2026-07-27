namespace SeventyTwo.Sample.Application.Inventories.StorageFeeCalc;

public sealed class StockAge
{
    /// <summary>
    /// 批次
    /// </summary>
    public string BatchNo { get; init; } = string.Empty;

    /// <summary>
    /// 库龄
    /// </summary>
    public int Days { get; init; }

    /// <summary>
    /// 库存
    /// </summary>
    public int Qty { get; init; }
}

public sealed class Interval
{
    /// <summary>
    /// 最小库龄（包头）
    /// </summary>
    public int MinDays { get; init; }

    /// <summary>
    /// 最大库龄（不包尾）
    /// </summary>
    public int MaxDays { get; init; }

    /// <summary>
    /// 金额
    /// </summary>
    public decimal Amount { get; init; }
}
