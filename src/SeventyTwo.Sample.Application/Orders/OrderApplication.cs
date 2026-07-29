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

        if (request.Limit is <= 0 or > 1000)
        {
            throw new OrderDomainException("每页数量必须在 1 到 1000 之间");
        }

        var page = await orderRepository.GetPageAsync(request, cancellationToken);
        return new PageResponse<OrderOutput> { List = mapper.Map<List<OrderOutput>>(page.Items), Total = page.Total };
    }

    public async Task<PageResponse<OrderOutput>> GetPageByIdsAsync(
        OrderPageRequest request,
        CancellationToken cancellationToken
    )
    {
        if (request.Index <= 0)
        {
            throw new OrderDomainException("页码必须大于 0");
        }

        if (request.Limit is <= 0 or > 1000)
        {
            throw new OrderDomainException("每页数量必须在 1 到 1000 之间");
        }

        var page = await orderRepository.GetPageByIdsAsync(request, cancellationToken);
        return new PageResponse<OrderOutput> { List = mapper.Map<List<OrderOutput>>(page.Items), Total = page.Total };
    }

    public async Task<CursorPageResponse<OrderOutput>> GetPageByCursorAsync(
        OrderPageRequest request,
        CancellationToken cancellationToken
    )
    {
        if (request.Limit is <= 0 or > 1000)
        {
            throw new OrderDomainException("每页数量必须在 1 到 1000 之间");
        }

        if (request.LastDateTime.HasValue != request.LastId.HasValue)
        {
            throw new OrderDomainException("最后时间和最后 ID 必须同时传入");
        }

        var page = await orderRepository.GetPageByCursorAsync(request, cancellationToken);
        return new CursorPageResponse<OrderOutput>
        {
            List = mapper.Map<List<OrderOutput>>(page.Items),
            HasNext = page.HasNext,
            LastDateTime = page.LastDateTime,
            LastId = page.LastId,
        };
    }
}
