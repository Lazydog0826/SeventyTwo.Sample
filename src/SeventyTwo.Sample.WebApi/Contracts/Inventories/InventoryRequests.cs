// ReSharper disable ClassNeverInstantiated.Global
namespace SeventyTwo.Sample.WebApi.Contracts.Inventories;

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
