// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace SeventyTwo.Sample.Application;

public sealed class PageResponse<T>
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
}
