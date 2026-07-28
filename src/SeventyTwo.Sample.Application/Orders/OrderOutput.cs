using SeventyTwo.Sample.Domain.Orders;

namespace SeventyTwo.Sample.Application.Orders;

/// <summary>
/// 订单输出。
/// </summary>
public sealed record OrderOutput
{
    /// <summary>
    /// 订单 ID。
    /// </summary>
    public long Id { get; init; }

    /// <summary>
    /// 订单编号。
    /// </summary>
    public string OrderNo { get; init; } = string.Empty;

    /// <summary>
    /// 客户 ID。
    /// </summary>
    public long CustomerId { get; init; }

    /// <summary>
    /// 仓库 ID。
    /// </summary>
    public int WarehouseId { get; init; }

    /// <summary>
    /// 订单类型。
    /// </summary>
    public OrderType OrderType { get; init; }

    /// <summary>
    /// 订单状态。
    /// </summary>
    public OrderStatus OrderStatus { get; init; }

    /// <summary>
    /// 收货人姓名。
    /// </summary>
    public string? ReceiverName { get; init; }

    /// <summary>
    /// 收货人手机号。
    /// </summary>
    public string? ReceiverPhone { get; init; }

    /// <summary>
    /// 收货地址所在省份。
    /// </summary>
    public string? Province { get; init; }

    /// <summary>
    /// 收货地址所在城市。
    /// </summary>
    public string? City { get; init; }

    /// <summary>
    /// 收货地址所在区县。
    /// </summary>
    public string? District { get; init; }

    /// <summary>
    /// 收货详细地址。
    /// </summary>
    public string? DetailAddress { get; init; }

    /// <summary>
    /// 订单备注。
    /// </summary>
    public string? Remark { get; init; }

    /// <summary>
    /// 乐观锁版本号。
    /// </summary>
    public long Version { get; init; }
}
