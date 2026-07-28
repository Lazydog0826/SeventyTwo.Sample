using AutoMapper;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.Orders;
using SqlSugar;

namespace SeventyTwo.Sample.Infrastructure.Orders;

[AutofacDependency(typeof(IOrderRepository))]
public class OrderRepository(ISqlSugarClient db, IMapper mapper) : IOrderRepository
{
    public async Task<OrderPage> GetPageAsync(PageRequest request, CancellationToken cancellationToken)
    {
        var query = db.Queryable<OrderRecord>().Where(x => x.DeleteAt == null).OrderByDescending(x => x.Id);
        var total = await query.CountAsync(cancellationToken);
        var records = await query
            .Skip((request.Index - 1) * request.Limit)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);
        return new OrderPage([.. records.Select(mapper.Map<Order>)], total);
    }
}
