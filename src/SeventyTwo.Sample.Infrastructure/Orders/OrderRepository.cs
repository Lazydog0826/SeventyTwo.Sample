using Mapster;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.Orders;
using SqlSugar;

namespace SeventyTwo.Sample.Infrastructure.Orders;

[AutofacDependency(typeof(IOrderRepository))]
public class OrderRepository(ISqlSugarClient db) : IOrderRepository
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
        return new OrderPage(records.Adapt<List<Order>>(), total, null, null);
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

        return new OrderPage(query2DataList.Adapt<List<Order>>(), total, null, null);
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
            .Where(x => x.DeleteAt == null);

        if (request.Direction == CursorDirection.Previous)
        {
            queryable = queryable
                .Where("(created_at, id) > (@LastDateTime, @LastId)", new { request.LastDateTime, request.LastId })
                .OrderBy(x => new { x.CreatedAt, x.Id });
        }
        else
        {
            queryable = queryable
                .WhereIF(
                    request is { LastDateTime: not null, LastId: not null },
                    "(created_at, id) < (@LastDateTime, @LastId)",
                    new { request.LastDateTime, request.LastId }
                )
                .OrderBy(x => new { x.CreatedAt, x.Id }, OrderByType.Desc);
        }

        var dataList = await queryable.Take(request.Limit + 1).ToListAsync(cancellationToken);
        var hasMore = dataList.Count > request.Limit;
        if (hasMore)
        {
            dataList.RemoveAt(request.Limit);
        }

        if (request.Direction == CursorDirection.Previous)
        {
            dataList.Reverse();
        }

        bool hasPrevious;
        bool hasNext;

        if (request.Direction == CursorDirection.Previous)
        {
            hasNext = true;
            hasPrevious = hasMore;
        }
        else
        {
            hasNext = hasMore;
            hasPrevious = request is { LastDateTime: not null, LastId: not null };
        }

        var firstDateTime = dataList.FirstOrDefault()?.CreatedAt;
        var firstId = dataList.FirstOrDefault()?.Id;
        var lastDateTime = dataList.LastOrDefault()?.CreatedAt;
        var lastId = dataList.LastOrDefault()?.Id;

        return new OrderCursorPage(
            dataList.Adapt<List<Order>>(),
            hasPrevious,
            hasNext,
            firstDateTime,
            firstId,
            lastDateTime,
            lastId
        );
    }
}
