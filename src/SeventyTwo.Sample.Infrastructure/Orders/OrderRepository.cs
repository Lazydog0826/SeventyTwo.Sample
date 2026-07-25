using SeventyTwo.Sample.Domain.Orders;
using SqlSugar;

namespace SeventyTwo.Sample.Infrastructure.Orders;

public sealed class OrderRepository(ISqlSugarClient db) : IOrderRepository
{
    public async Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        var orderRecord = ToRecord(order);
        var itemRecords = order.Items.Select(ToRecord).ToList();
        var result = await db.Ado.UseTranAsync(async () =>
        {
            await db.Insertable(orderRecord).ExecuteCommandAsync(cancellationToken);
            await db.Insertable(itemRecords).ExecuteCommandAsync(cancellationToken);
        });

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException("新增订单失败", result.ErrorException);
        }
    }

    private OrderRecord ToRecord(Order order)
    {
        return new OrderRecord
        {
            Id = order.Id,
            OrderNo = order.OrderNo,
            CustomerId = order.CustomerId,
            WarehouseId = order.WarehouseId,
            OrderType = (short)order.OrderType,
            OrderStatus = (short)order.OrderStatus,
            PaymentStatus = (short)order.PaymentStatus,
            ShippingStatus = (short)order.ShippingStatus,
            TotalAmount = order.TotalAmount,
            DiscountAmount = order.DiscountAmount,
            FreightAmount = order.FreightAmount,
            PayableAmount = order.PayableAmount,
            ItemCount = order.ItemCount,
            ReceiverName = order.ReceiverName,
            ReceiverPhone = order.ReceiverPhone,
            Province = order.Province,
            City = order.City,
            District = order.District,
            DetailAddress = order.DetailAddress,
            Remark = order.Remark,
            Version = order.Version,
            CreatedBy = order.CreatedBy,
            CreatedAt = order.CreatedAt,
            UpdatedBy = order.UpdatedBy,
            UpdatedAt = order.UpdatedAt,
        };
    }

    private OrderItemRecord ToRecord(OrderItem item)
    {
        return new OrderItemRecord
        {
            Id = item.Id,
            OrderId = item.OrderId,
            LineNo = item.LineNo,
            ProductId = item.ProductId,
            SkuId = item.SkuId,
            SkuCode = item.SkuCode,
            ProductName = item.ProductName,
            Specification = item.Specification,
            Unit = item.Unit,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            DiscountAmount = item.DiscountAmount,
            LineAmount = item.LineAmount,
            ShippedQuantity = item.ShippedQuantity,
            ReturnedQuantity = item.ReturnedQuantity,
            Remark = item.Remark,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
        };
    }
}
