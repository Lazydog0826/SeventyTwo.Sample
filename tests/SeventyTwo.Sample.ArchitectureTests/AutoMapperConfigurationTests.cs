using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using SeventyTwo.Sample.Application.Orders;
using SeventyTwo.Sample.Application.Products;
using SeventyTwo.Sample.Domain.Orders;
using SeventyTwo.Sample.Domain.Products;
using SeventyTwo.Sample.WebApi.Controllers;

namespace SeventyTwo.Sample.ArchitectureTests;

public sealed class AutoMapperConfigurationTests
{
    [Fact]
    public void Mappings_ShouldBeValid()
    {
        var configuration = new MapperConfiguration(
            expression =>
            {
                expression.AddProfile<Application.Products.ProductMappingProfile>();
                expression.AddProfile<Application.Orders.OrderMappingProfile>();
                expression.AddProfile<Infrastructure.Orders.OrderMappingProfile>();
                expression.AddProfile<WebApi.Controllers.ProductMappingProfile>();
            },
            NullLoggerFactory.Instance
        );
        configuration.AssertConfigurationIsValid();
        var mapper = configuration.CreateMapper();

        var createInput = mapper.Map<CreateProductInput>(new CreateProductRequest("商品", 1m));
        var updateInput = mapper.Map<UpdateProductInput>(new UpdateProductRequest(1, "商品", 2m, 3));
        var output = mapper.Map<ProductOutput>(new Product(1, "商品", 1m));
        var orderOutput = mapper.Map<OrderOutput>(
            new Order(
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
            )
        );

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
