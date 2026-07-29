// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace SeventyTwo.Sample.Domain;

public class PageRequest
{
    /// <summary>
    /// 页码。
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// 每页数量。
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// 最后时间（用于游标分页）
    /// </summary>
    public DateTimeOffset LastDateTime { get; set; }

    /// <summary>
    /// 最后ID（用于游标分页）
    /// </summary>
    public long LastId { get; set; }
}
