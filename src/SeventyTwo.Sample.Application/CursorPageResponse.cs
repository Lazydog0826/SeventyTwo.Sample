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
    /// 是否还有上一页。
    /// </summary>
    public bool HasPrevious { get; set; }

    /// <summary>
    /// 是否还有下一页。
    /// </summary>
    public bool HasNext { get; set; }

    /// <summary>
    /// 当前页第一条数据的创建时间。
    /// </summary>
    public DateTimeOffset? FirstDateTime { get; set; }

    /// <summary>
    /// 当前页第一条数据的 ID。
    /// </summary>
    public long? FirstId { get; set; }

    /// <summary>
    /// 当前页最后一条数据的创建时间。
    /// </summary>
    public DateTimeOffset? LastDateTime { get; set; }

    /// <summary>
    /// 当前页最后一条数据的 ID。
    /// </summary>
    public long? LastId { get; set; }
}
