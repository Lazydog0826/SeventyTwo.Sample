using SeventyTwo.InfraKit.Core.DomainAggregateRoot;

namespace SeventyTwo.Sample.Domain.Orders;

public sealed class Order : AggregateRoot
{
    public Order(
        long id,
        string orderNo,
        long customerId,
        int warehouseId,
        OrderType orderType,
        OrderStatus orderStatus,
        string? receiverName,
        string? receiverPhone,
        string? province,
        string? city,
        string? district,
        string? detailAddress,
        string? remark
    )
    {
        Id = id;
        OrderNo = orderNo;
        CustomerId = customerId;
        WarehouseId = warehouseId;
        OrderType = orderType;
        OrderStatus = orderStatus;
        ReceiverName = receiverName;
        ReceiverPhone = receiverPhone;
        Province = province;
        City = city;
        District = district;
        DetailAddress = detailAddress;
        Remark = remark;
    }

    public string OrderNo { get; private set; }

    public long CustomerId { get; private set; }

    public int WarehouseId { get; private set; }

    public OrderType OrderType { get; private set; }

    public OrderStatus OrderStatus { get; private set; }

    public string? ReceiverName { get; private set; }

    public string? ReceiverPhone { get; private set; }

    public string? Province { get; private set; }

    public string? City { get; private set; }

    public string? District { get; private set; }

    public string? DetailAddress { get; private set; }

    public string? Remark { get; private set; }
}
