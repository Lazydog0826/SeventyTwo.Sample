// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace SeventyTwo.Sample.Application;

public sealed class CursorPageResponse<T>
    where T : class
{
    /// <summary>
    /// 当前页数据。
    /// </summary>
    public List<T> List { get; set; } = [];

    /// <summary>
    /// 总数。
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// 当前页最后一条数据的创建时间。
    /// </summary>
    public DateTimeOffset? LastDateTime { get; set; }

    /// <summary>
    /// 当前页最后一条数据的 ID。
    /// </summary>
    public long? LastId { get; set; }
}
