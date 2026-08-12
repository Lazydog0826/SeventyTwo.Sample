using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.InfraKit.Extension;
using SeventyTwo.Sample.Application;
using SeventyTwo.Sample.Application.Orders;
using SeventyTwo.Sample.Common.MessageKeys;
using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.Orders;
using SqlSugar;

// ReSharper disable MemberCanBeMadeStatic.Local

namespace SeventyTwo.Sample.Infrastructure.Orders;

[AutofacDependency(typeof(IRandomOrderDataService))]
public sealed class RandomOrderDataService(ISqlSugarClient db, IUnitOfWork unitOfWork) : IRandomOrderDataService
{
    public async Task AddAsync(int count, CancellationToken cancellationToken)
    {
        if (count <= 0)
        {
            throw new OrderDomainException(MessageKeys.Orders.RandomCountMustBePositive);
        }

        var orders = new List<OrderRecord>(count);
        var orderItems = new List<OrderItemRecord>();
        var now = DateTimeExtension.Now();

        for (var index = 0; index < count; index++)
        {
            var orderId = Guid.CreateVersion7();
            orders.Add(CreateOrder(orderId, now));

            var itemCount = Random.Shared.Next(1, 6);
            for (var lineNo = 1; lineNo <= itemCount; lineNo++)
            {
                orderItems.Add(CreateOrderItem(orderId, lineNo));
            }
        }

        await unitOfWork.ExecuteAsync(
            async () =>
            {
                await db.Insertable(orders).ExecuteCommandAsync(cancellationToken);
                await db.Insertable(orderItems).ExecuteCommandAsync(cancellationToken);
            },
            cancellationToken
        );
    }

    private OrderRecord CreateOrder(Guid orderId, DateTimeOffset now)
    {
        return new OrderRecord
        {
            Id = orderId,
            OrderNo = orderId.ToString("N"),
            CustomerId = Guid.CreateVersion7(),
            WarehouseId = Guid.CreateVersion7(),
            OrderType = (OrderType)Random.Shared.Next(1, 4),
            OrderStatus = (OrderStatus)Random.Shared.Next(0, 4),
            ReceiverName = $"收货人{Random.Shared.Next(1, 10_001)}",
            ReceiverPhone = $"1{Random.Shared.NextInt64(3_000_000_000, 10_000_000_000)}",
            Province = "浙江省",
            City = "杭州市",
            District = "西湖区",
            DetailAddress = $"随机路{Random.Shared.Next(1, 1001)}号",
            Remark = "接口生成的随机订单",
            Enable = true,
            CreatedBy = SystemIds.System,
            CreatedAt = now,
            OrgId = Guid.Empty,
            Version = Guid.CreateVersion7(),
        };
    }

    private OrderItemRecord CreateOrderItem(Guid orderId, int lineNo)
    {
        var quantity = Random.Shared.Next(1, 101);
        return new OrderItemRecord
        {
            Id = Guid.CreateVersion7(),
            OrderId = orderId,
            LineNo = lineNo,
            ProductId = Guid.CreateVersion7(),
            ProductName = $"随机商品{Random.Shared.Next(1, 10_001)}",
            Unit = "件",
            Quantity = quantity,
            UnitPrice = Random.Shared.Next(1, 100_001) / 100m,
            ShippedQuantity = Random.Shared.Next(0, quantity + 1),
            ReturnedQuantity = 0,
            Remark = "接口生成的随机订单明细",
        };
    }
}
