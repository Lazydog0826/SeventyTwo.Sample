// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace SeventyTwo.Sample.Domain.Orders;

public sealed class OrderPageRequest : PageRequest
{
    /// <summary>
    /// 收货人手机号前缀。
    /// </summary>
    public string ReceiverPhone { get; set; } = string.Empty;
}
