using Microsoft.AspNetCore.Mvc;
using SeventyTwo.Sample.Application.Inventories;
using SeventyTwo.Sample.Application.Inventories.ChangeInventory;

// ReSharper disable ClassNeverInstantiated.Global

namespace SeventyTwo.Sample.WebApi.Controllers;

[ApiController]
[Route("api/inventories")]
public sealed class InventoriesController(IInventoryApplication inventoryApplication) : ControllerBase
{
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

public sealed record ChangeInventoryRequest(
    Guid RequestNo,
    IReadOnlyCollection<InventoryIncreaseRequest> Increases,
    IReadOnlyCollection<InventoryDecreaseRequest> Decreases
);

public sealed record InventoryIncreaseRequest(
    Guid ProductId,
    Guid WarehouseId,
    Guid LocationId,
    int Quantity,
    string InboundBatchNo,
    DateTimeOffset ChangedAt
);

public sealed record InventoryDecreaseRequest(Guid ProductId, Guid WarehouseId, Guid LocationId, int Quantity);
