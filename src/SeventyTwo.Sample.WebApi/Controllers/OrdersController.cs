using Microsoft.AspNetCore.Mvc;
using SeventyTwo.Sample.Application;
using SeventyTwo.Sample.Application.Orders;
using SeventyTwo.Sample.Domain.Orders;
using SeventyTwo.Sample.WebApi.Contracts.Orders;

namespace SeventyTwo.Sample.WebApi.Controllers;

/// <summary>
/// 订单接口。
/// </summary>
/// <param name="randomOrderDataService">随机订单数据服务。</param>
/// <param name="orderApplication">订单应用服务。</param>
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

    /// <summary>
    /// 索引分页后回表查询订单。
    /// </summary>
    /// <param name="request">分页请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>订单分页数据。</returns>
    [HttpPost("page/ids")]
    public Task<PageResponse<OrderOutput>> GetPageByIds(OrderPageRequest request, CancellationToken cancellationToken)
    {
        return orderApplication.GetPageByIdsAsync(request, cancellationToken);
    }

    /// <summary>
    /// 使用游标分页查询订单。
    /// </summary>
    /// <param name="request">游标分页请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>订单分页数据及上一页、下一页游标。</returns>
    [HttpPost("page/cursor")]
    public Task<CursorPageResponse<OrderOutput>> GetPageByCursor(
        OrderPageRequest request,
        CancellationToken cancellationToken
    )
    {
        return orderApplication.GetPageByCursorAsync(request, cancellationToken);
    }
}
