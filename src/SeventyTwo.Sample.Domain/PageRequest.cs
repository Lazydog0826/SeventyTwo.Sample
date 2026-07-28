// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace SeventyTwo.Sample.Domain;

public sealed class PageRequest
{
    /// <summary>
    /// 页码。
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// 每页数量。
    /// </summary>
    public int Limit { get; set; }
}
