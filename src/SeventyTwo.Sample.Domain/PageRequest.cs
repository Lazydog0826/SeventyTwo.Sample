// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace SeventyTwo.Sample.Domain;

public class PageRequest
{
    /// <summary>
    /// 页码。
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// 每页数量。
    /// </summary>
    public int Limit { get; init; }

    /// <summary>
    /// 游标翻页方向。
    /// </summary>
    public CursorDirection Direction { get; set; }

    /// <summary>
    /// 最后时间（用于游标分页）
    /// </summary>
    public DateTimeOffset? LastDateTime { get; set; }

    /// <summary>
    /// 最后ID（用于游标分页）
    /// </summary>
    public Guid? LastId { get; set; }

    /// <summary>
    /// 分页偏移是否在底层查询支持的范围内。
    /// </summary>
    public bool IsOffsetWithinRange()
    {
        return Index > 0 && Limit > 0 && ((long)Index - 1) * Limit <= int.MaxValue;
    }
}

public enum CursorDirection
{
    Next,
    Previous,
}
