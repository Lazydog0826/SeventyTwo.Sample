using AutoMapper;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.Sample.Domain.Orders;
using SqlSugar;

namespace SeventyTwo.Sample.Infrastructure.Orders;

[AutofacDependency(typeof(IOrderRepository))]
public class OrderRepository(ISqlSugarClient db, IMapper mapper) : IOrderRepository
{
    public async Task<OrderPage> GetPageAsync(OrderPageRequest request, CancellationToken cancellationToken)
    {
        var query = db.Queryable<OrderRecord>()
            .Where(x => x.DeleteAt == null)
            .WhereIF(
                !string.IsNullOrWhiteSpace(request.ReceiverPhone),
                x => x.ReceiverPhone != null && x.ReceiverPhone.StartsWith(request.ReceiverPhone)
            )
            .OrderBy(x => new { x.CreatedAt, x.Id }, OrderByType.Desc);
        var total = await query.CountAsync(cancellationToken);
        var records = await query
            .Skip((request.Index - 1) * request.Limit)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);
        return new OrderPage([.. records.Select(mapper.Map<Order>)], total, null, null);
    }

    public async Task<OrderPage> GetPageByIdsAsync(OrderPageRequest request, CancellationToken cancellationToken)
    {
        // 索引分页后回表查询
        var query1 = db.Queryable<OrderRecord>()
            .Where(x => x.DeleteAt == null)
            .WhereIF(
                !string.IsNullOrWhiteSpace(request.ReceiverPhone),
                x => x.ReceiverPhone != null && x.ReceiverPhone.StartsWith(request.ReceiverPhone)
            )
            .OrderBy(x => new { x.CreatedAt, x.Id }, OrderByType.Desc)
            .Select(x => x.Id);
        var total = await query1.CountAsync(cancellationToken);
        var query1DataIds = await query1
            .Skip((request.Index - 1) * request.Limit)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        var query2DataList = await db.Queryable<OrderRecord>()
            .Where(x => query1DataIds.Contains(x.Id))
            .OrderBy(x => new { x.CreatedAt, x.Id }, OrderByType.Desc)
            .ToListAsync(cancellationToken);

        return new OrderPage([.. query2DataList.Select(mapper.Map<Order>)], total, null, null);
    }

    public async Task<OrderCursorPage> GetPageByCursorAsync(
        OrderPageRequest request,
        CancellationToken cancellationToken
    )
    {
        var queryable = db.Queryable<OrderRecord>()
            .WhereIF(
                !string.IsNullOrWhiteSpace(request.ReceiverPhone),
                x => x.ReceiverPhone != null && x.ReceiverPhone.StartsWith(request.ReceiverPhone)
            )
            .Where(x => x.DeleteAt == null)
            .OrderBy(x => new { x.CreatedAt, x.Id }, OrderByType.Desc);

        queryable = queryable.WhereIF(
            request is { LastDateTime: not null, LastId: not null },
            "(created_at, id) < (@LastDateTime, @LastId)",
            new { request.LastDateTime, request.LastId }
        );

        var dataList = await queryable.Take(request.Limit + 1).ToListAsync(cancellationToken);
        var hasNext = dataList.Count > request.Limit;
        if (hasNext)
        {
            dataList.RemoveAt(request.Limit);
        }

        var lastDateTime = dataList.LastOrDefault()?.CreatedAt;
        var lastId = dataList.LastOrDefault()?.Id;

        return new OrderCursorPage([.. dataList.Select(mapper.Map<Order>)], hasNext, lastDateTime, lastId);
    }
}
