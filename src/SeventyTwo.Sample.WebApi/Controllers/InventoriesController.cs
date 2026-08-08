using Microsoft.AspNetCore.Mvc;
using SeventyTwo.Sample.Application.Inventories;
using SeventyTwo.Sample.Application.Inventories.ChangeInventory;

// ReSharper disable ClassNeverInstantiated.Global

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
        var input = new ChangeInventoryInput(
            request.RequestNo,
            [
                .. request.Increases.Select(x => new InventoryIncreaseInput(
                    x.ProductId,
                    x.WarehouseId,
                    x.LocationId,
                    x.Quantity,
                    x.InboundBatchNo,
                    x.ChangedAt
                )),
            ],
            [
                .. request.Decreases.Select(x => new InventoryDecreaseInput(
                    x.ProductId,
                    x.WarehouseId,
                    x.LocationId,
                    x.Quantity
                )),
            ]
        );

        await inventoryApplication.ChangeAsync(input, cancellationToken);
    }
}

/// <summary>
/// 库存变更请求。
/// </summary>
/// <param name="RequestNo">请求编号。</param>
/// <param name="Increases">库存增加明细。</param>
/// <param name="Decreases">库存扣减明细。</param>
public sealed record ChangeInventoryRequest(
    Guid RequestNo,
    IReadOnlyCollection<InventoryIncreaseRequest> Increases,
    IReadOnlyCollection<InventoryDecreaseRequest> Decreases
);

/// <summary>
/// 库存增加请求。
/// </summary>
/// <param name="ProductId">商品标识。</param>
/// <param name="WarehouseId">仓库标识。</param>
/// <param name="LocationId">库位标识。</param>
/// <param name="Quantity">增加数量。</param>
/// <param name="InboundBatchNo">入库批次号。</param>
/// <param name="ChangedAt">变更时间。</param>
public sealed record InventoryIncreaseRequest(
    Guid ProductId,
    Guid WarehouseId,
    Guid LocationId,
    int Quantity,
    string InboundBatchNo,
    DateTimeOffset ChangedAt
);

/// <summary>
/// 库存扣减请求。
/// </summary>
/// <param name="ProductId">商品标识。</param>
/// <param name="WarehouseId">仓库标识。</param>
/// <param name="LocationId">库位标识。</param>
/// <param name="Quantity">扣减数量。</param>
public sealed record InventoryDecreaseRequest(Guid ProductId, Guid WarehouseId, Guid LocationId, int Quantity);
