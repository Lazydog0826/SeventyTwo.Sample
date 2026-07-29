using Microsoft.AspNetCore.Mvc;
using SeventyTwo.Sample.Application;
using SeventyTwo.Sample.Application.Orders;
using SeventyTwo.Sample.Domain.Orders;

// ReSharper disable ClassNeverInstantiated.Global

namespace SeventyTwo.Sample.WebApi.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(IRandomOrderDataService randomOrderDataService, IOrderApplication orderApplication)
    : ControllerBase
{
    /// <summary>
    /// 批量新增随机订单及订单明细。
    /// </summary>
    /// <param name="request">新增数量。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    [HttpPost("random")]
    public Task AddRandom(RandomOrdersRequest request, CancellationToken cancellationToken)
    {
        return randomOrderDataService.AddAsync(request.Count, cancellationToken);
    }

    /// <summary>
    /// 分页查询订单。
    /// </summary>
    /// <param name="request">分页请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>订单分页数据。</returns>
    [HttpPost("page")]
    public Task<PageResponse<OrderOutput>> GetPage(OrderPageRequest request, CancellationToken cancellationToken)
    {
        return orderApplication.GetPageAsync(request, cancellationToken);
    }
}

public sealed record RandomOrdersRequest(int Count);
