using Mapster;
using SeventyTwo.InfraKit.Autofac;
using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.Orders;

namespace SeventyTwo.Sample.Application.Orders;

[AutofacDependency(typeof(IOrderApplication))]
public class OrderApplication(IOrderRepository orderRepository) : IOrderApplication
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
        return new PageResponse<OrderOutput> { List = page.Items.Adapt<List<OrderOutput>>(), Total = page.Total };
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
        return new PageResponse<OrderOutput> { List = page.Items.Adapt<List<OrderOutput>>(), Total = page.Total };
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

        if (request.LastDateTime.HasValue != (request.LastId is not null))
        {
            throw new OrderDomainException("最后时间和最后 ID 必须同时传入");
        }

        if (request.Direction is not CursorDirection.Next and not CursorDirection.Previous)
        {
            throw new OrderDomainException("游标翻页方向无效");
        }

        if (request is { Direction: CursorDirection.Previous, LastDateTime: null })
        {
            throw new OrderDomainException("查询上一页时必须传入游标");
        }

        var page = await orderRepository.GetPageByCursorAsync(request, cancellationToken);
        return new CursorPageResponse<OrderOutput>
        {
            List = page.Items.Adapt<List<OrderOutput>>(),
            HasPrevious = page.HasPrevious,
            HasNext = page.HasNext,
            FirstDateTime = page.FirstDateTime,
            FirstId = page.FirstId,
            LastDateTime = page.LastDateTime,
            LastId = page.LastId,
        };
    }
}
