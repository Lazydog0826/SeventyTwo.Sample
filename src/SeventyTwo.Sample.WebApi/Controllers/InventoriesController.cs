using Mapster;
using Microsoft.AspNetCore.Mvc;
using SeventyTwo.Sample.Application.Inventories;
using SeventyTwo.Sample.Application.Inventories.ChangeInventory;
using SeventyTwo.Sample.WebApi.Contracts.Inventories;

namespace SeventyTwo.Sample.WebApi.Controllers;

/// <summary>
/// 库存接口。
/// </summary>
/// <param name="inventoryApplication">库存应用服务。</param>
[ApiController]
[Route("api/inventories")]
public sealed class InventoriesController(IInventoryApplication inventoryApplication) : ControllerBase
{
    /// <summary>
    /// 批量变更库存。
    /// </summary>
    /// <param name="request">库存变更请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    [HttpPost("changes")]
    public async Task Change(ChangeInventoryRequest request, CancellationToken cancellationToken)
    {
        await inventoryApplication.ChangeAsync(request.Adapt<ChangeInventoryInput>(), cancellationToken);
    }
}
