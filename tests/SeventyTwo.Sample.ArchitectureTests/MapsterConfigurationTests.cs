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
        const string version = "01ARZ3NDEKTSV4RRFFQ69G5FB0";
        var product = new Product("01ARZ3NDEKTSV4RRFFQ69G5FAV", "商品", 1m) { Version = version };
        var updateInput = new UpdateProductRequest("01ARZ3NDEKTSV4RRFFQ69G5FAV", "商品", 2m, version)
            .Adapt<UpdateProductInput>(configuration);
        var output = product.Adapt<ProductOutput>(configuration);
        var orderOutput = new Order(
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "ORDER-1",
            "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            3,
            OrderType.Sales,
            OrderStatus.Pending,
            "收货人",
            "13800000000",
            "省",
            "市",
            "区",
            "地址",
            "备注",
            [
                new OrderItem(
                    "01ARZ3NDEKTSV4RRFFQ69G5FAX",
                    "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                    1,
                    "01ARZ3NDEKTSV4RRFFQ69G5FAZ",
                    "商品",
                    "件",
                    3,
                    4m,
                    2,
                    1,
                    "明细备注"
                ),
            ]
        ).Adapt<OrderOutput>(configuration);

        Assert.Equal(new CreateProductInput("商品", 1m), createInput);
        Assert.Equal(new UpdateProductInput("商品", 2m, version), updateInput);
        Assert.Equal("01ARZ3NDEKTSV4RRFFQ69G5FAV", output.Id);
        Assert.Equal("商品", output.Name);
        Assert.Equal(1m, output.Price);
        Assert.Equal(product.Version, output.Version);
        Assert.Equal("01ARZ3NDEKTSV4RRFFQ69G5FAW", orderOutput.Id);
        Assert.Equal("ORDER-1", orderOutput.OrderNo);
        var orderItemOutput = Assert.Single(orderOutput.Items);
        Assert.Equal("01ARZ3NDEKTSV4RRFFQ69G5FAX", orderItemOutput.Id);
        Assert.Equal("商品", orderItemOutput.ProductName);
    }
}
