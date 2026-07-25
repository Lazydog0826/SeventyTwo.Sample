using Microsoft.AspNetCore.Mvc;
using SeventyTwo.Sample.Application.Inventories;
using SeventyTwo.Sample.Application.Inventories.ChangeInventory;
using SeventyTwo.Sample.Domain.Inventories;

namespace SeventyTwo.Sample.WebApi.Controllers;

[ApiController]
[Route("api/inventories")]
public sealed class InventoriesController(IInventoryApplication inventoryApplication)
    : ControllerBase
{
    [HttpPost("{inventoryId:long}/changes")]
    public async Task<ActionResult<ChangeInventoryResult>> Change(
        long inventoryId,
        ChangeInventoryRequest request,
        CancellationToken cancellationToken
    )
    {
        var input = new ChangeInventoryInput(
            request.RequestNo,
            inventoryId,
            request.ChangeType,
            request.Quantity
        );
        var result = await inventoryApplication.ChangeAsync(input, cancellationToken);

        return Ok(result);
    }
}

public sealed record ChangeInventoryRequest(
    string RequestNo,
    InventoryChangeType ChangeType,
    int Quantity
);
