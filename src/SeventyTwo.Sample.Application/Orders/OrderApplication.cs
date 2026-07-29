using AutoMapper;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.Sample.Domain.Orders;

namespace SeventyTwo.Sample.Application.Orders;

[AutofacDependency(typeof(IOrderApplication))]
public class OrderApplication(IOrderRepository orderRepository, IMapper mapper) : IOrderApplication
{
    public async Task<PageResponse<OrderOutput>> GetPageAsync(
        OrderPageRequest request,
        CancellationToken cancellationToken
    )
    {
        if (request.Index <= 0)
        {
            throw new OrderDomainException("页码必须大于 0");
        }

        if (request.Limit is <= 0 or > 100)
        {
            throw new OrderDomainException("每页数量必须在 1 到 100 之间");
        }

        if (request.FuncType is not (1 or 2 or 3))
        {
            throw new OrderDomainException("查询实现类型必须为 1、2 或 3");
        }

        var page = request.FuncType switch
        {
            1 => await orderRepository.GetPageAsync(request, cancellationToken),
            2 => await orderRepository.GetPageByIdsAsync(request, cancellationToken),
            _ => await orderRepository.GetPageByCursorAsync(request, cancellationToken),
        };
        return new PageResponse<OrderOutput> { List = mapper.Map<List<OrderOutput>>(page.Items), Total = page.Total };
    }
}
