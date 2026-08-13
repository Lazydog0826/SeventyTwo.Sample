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
            throw new OrderDomainException(MessageKeys.Paging.PageNumberMustBePositive);
        }

        if (request.Limit is <= 0 or > 1000)
        {
            throw new OrderDomainException(MessageKeys.Paging.PageSizeOutOfRange1000);
        }

        if (!request.IsOffsetWithinRange())
        {
            throw new OrderDomainException(MessageKeys.Paging.PageOffsetOutOfRange);
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
            throw new OrderDomainException(MessageKeys.Paging.PageNumberMustBePositive);
        }

        if (request.Limit is <= 0 or > 1000)
        {
            throw new OrderDomainException(MessageKeys.Paging.PageSizeOutOfRange1000);
        }

        if (!request.IsOffsetWithinRange())
        {
            throw new OrderDomainException(MessageKeys.Paging.PageOffsetOutOfRange);
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
            throw new OrderDomainException(MessageKeys.Paging.PageSizeOutOfRange1000);
        }

        if (request.LastDateTime.HasValue != (request.LastId is not null))
        {
            throw new OrderDomainException(MessageKeys.Paging.CursorFieldsMustBeProvidedTogether);
        }

        if (request.Direction is not CursorDirection.Next and not CursorDirection.Previous)
        {
            throw new OrderDomainException(MessageKeys.Paging.CursorDirectionInvalid);
        }

        if (request is { Direction: CursorDirection.Previous, LastDateTime: null })
        {
            throw new OrderDomainException(MessageKeys.Paging.PreviousPageCursorRequired);
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
