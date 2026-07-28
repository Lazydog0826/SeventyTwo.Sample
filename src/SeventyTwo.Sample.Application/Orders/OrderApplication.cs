using AutoMapper;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.Orders;

namespace SeventyTwo.Sample.Application.Orders;

[AutofacDependency(typeof(IOrderApplication))]
public class OrderApplication(IOrderRepository orderRepository, IMapper mapper) : IOrderApplication
{
    public async Task<PageResponse<OrderOutput>> GetPageAsync(PageRequest request, CancellationToken cancellationToken)
    {
        if (request.Index <= 0)
        {
            throw new OrderDomainException("页码必须大于 0");
        }

        if (request.Limit is <= 0 or > 100)
        {
            throw new OrderDomainException("每页数量必须在 1 到 100 之间");
        }

        var page = await orderRepository.GetPageAsync(request, cancellationToken);
        return new PageResponse<OrderOutput> { List = mapper.Map<List<OrderOutput>>(page.Items), Total = page.Total };
    }
}
