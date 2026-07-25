using Microsoft.AspNetCore.Mvc;
using SeventyTwo.Sample.Application.Orders;
using SeventyTwo.Sample.Application.Orders.CreateOrder;

namespace SeventyTwo.Sample.WebApi.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(IOrderApplication orderApplication) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreateOrderResult>> Create(
        CreateOrderRequest request,
        CancellationToken cancellationToken
    )
    {
        var input = new CreateOrderInput(
            request.CustomerId,
            request.WarehouseId,
            request.Items
                .Select(item =>
                    new CreateOrderItemInput(
                        item.ProductId,
                        item.SkuId,
                        item.SkuCode,
                        item.ProductName,
                        item.Quantity,
                        item.UnitPrice
                    )
                )
                .ToList()
        );
        var result = await orderApplication.CreateAsync(input, cancellationToken);

        return Created($"/api/orders/{result.OrderId}", result);
    }
}

public sealed record CreateOrderRequest(
    long CustomerId,
    int WarehouseId,
    IReadOnlyCollection<CreateOrderItemRequest> Items
);

public sealed record CreateOrderItemRequest(
    long ProductId,
    long SkuId,
    string SkuCode,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);
