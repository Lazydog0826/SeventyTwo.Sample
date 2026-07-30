using Mapster;
using SeventyTwo.Sample.Application.Orders;
using SeventyTwo.Sample.Application.Products;
using SeventyTwo.Sample.Domain.Orders;
using SeventyTwo.Sample.Domain.Products;
using SeventyTwo.Sample.WebApi.Controllers;

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
        var updateInput = new UpdateProductRequest(1, "商品", 2m, 3).Adapt<UpdateProductInput>(configuration);
        var output = new Product(1, "商品", 1m).Adapt<ProductOutput>(configuration);
        var orderOutput = new Order(
            1,
            "ORDER-1",
            2,
            3,
            OrderType.Sales,
            OrderStatus.Pending,
            "收货人",
            "13800000000",
            "省",
            "市",
            "区",
            "地址",
            "备注"
        ).Adapt<OrderOutput>(configuration);

        Assert.Equal(new CreateProductInput("商品", 1m), createInput);
        Assert.Equal(new UpdateProductInput("商品", 2m, 3), updateInput);
        Assert.Equal(1, output.Id);
        Assert.Equal("商品", output.Name);
        Assert.Equal(1m, output.Price);
        Assert.Equal(0, output.Version);
        Assert.Equal(1, orderOutput.Id);
        Assert.Equal("ORDER-1", orderOutput.OrderNo);
    }
}
