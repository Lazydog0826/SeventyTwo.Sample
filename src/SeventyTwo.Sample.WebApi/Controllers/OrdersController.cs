using Microsoft.AspNetCore.Mvc;
using SeventyTwo.Sample.Application.Orders;

// ReSharper disable ClassNeverInstantiated.Global

namespace SeventyTwo.Sample.WebApi.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(IRandomOrderDataService randomOrderDataService) : ControllerBase
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
}

public sealed record RandomOrdersRequest(int Count);
