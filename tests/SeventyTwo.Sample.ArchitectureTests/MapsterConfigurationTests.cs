using Mapster;
using SeventyTwo.Sample.Application.Orders;
using SeventyTwo.Sample.Application.Products;
using SeventyTwo.Sample.Domain.Orders;
using SeventyTwo.Sample.Domain.Products;
using SeventyTwo.Sample.WebApi.Controllers;
using SeventyTwo.Sample.WebApi.Contracts.Products;

namespace SeventyTwo.Sample.ArchitectureTests;

public sealed class MapsterConfigurationTests
{
    [Fact]
    public void Mappings_ShouldBeValid()
    {
        var configuration = new TypeAdapterConfig();
        configuration.Scan(
            typeof(Application.AssemblyMarker).Assembly,
            typeof(Infrastructure.AssemblyMarker).Assembly,
            typeof(WebApi.AssemblyMarker).Assembly
        );
        configuration.Compile();

        var createInput = new CreateProductRequest("商品", 1m).Adapt<CreateProductInput>(configuration);
        var version = Guid.CreateVersion7();
        var productId = Guid.CreateVersion7();
        var orderId = Guid.CreateVersion7();
        var orderItemId = Guid.CreateVersion7();
        var product = new Product(productId, "商品", 1m) { Version = version };
        var updateInput = new UpdateProductRequest(productId, "商品", 2m, version).Adapt<UpdateProductInput>(
            configuration
        );
        var output = product.Adapt<ProductOutput>(configuration);
        var orderOutput = new Order(
            orderId,
            "ORDER-1",
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            OrderType.Sales,
            OrderStatus.Pending,
            "收货人",
            "13800000000",
            "省",
            "市",
            "区",
            "地址",
            "备注",
            [new OrderItem(orderItemId, orderId, 1, Guid.CreateVersion7(), "商品", "件", 3, 4m, 2, 1, "明细备注")]
        ).Adapt<OrderOutput>(configuration);

        Assert.Equal(new CreateProductInput("商品", 1m), createInput);
        Assert.Equal(new UpdateProductInput("商品", 2m, version), updateInput);
        Assert.Equal(productId, output.Id);
        Assert.Equal("商品", output.Name);
        Assert.Equal(1m, output.Price);
        Assert.Equal(product.Version, output.Version);
        Assert.Equal(orderId, orderOutput.Id);
        Assert.Equal("ORDER-1", orderOutput.OrderNo);
        var orderItemOutput = Assert.Single(orderOutput.Items);
        Assert.Equal(orderItemId, orderItemOutput.Id);
        Assert.Equal("商品", orderItemOutput.ProductName);
    }
}
