// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace SeventyTwo.Sample.Domain.Orders;

public sealed class OrderPageRequest : PageRequest
{
    /// <summary>
    /// 查询实现类型：1 原始分页查询，2 索引分页后回表查询，3 游标分页查询。
    /// </summary>
    public int FuncType { get; set; }

    /// <summary>
    /// 收货人手机号前缀。
    /// </summary>
    public string ReceiverPhone { get; set; } = string.Empty;
}
