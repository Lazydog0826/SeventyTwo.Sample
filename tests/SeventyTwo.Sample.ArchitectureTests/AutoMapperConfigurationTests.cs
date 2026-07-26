using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using SeventyTwo.Sample.Application.Products;
using SeventyTwo.Sample.Domain.Products;
using SeventyTwo.Sample.WebApi.Controllers;

namespace SeventyTwo.Sample.ArchitectureTests;

public sealed class AutoMapperConfigurationTests
{
    [Fact]
    public void ProductMappings_ShouldBeValid()
    {
        var configuration = new MapperConfiguration(
            expression =>
            {
                expression.AddProfile<Application.Products.ProductMappingProfile>();
                expression.AddProfile<WebApi.Controllers.ProductMappingProfile>();
            },
            NullLoggerFactory.Instance
        );
        configuration.AssertConfigurationIsValid();
        var mapper = configuration.CreateMapper();

        var createInput = mapper.Map<CreateProductInput>(new CreateProductRequest("商品", 1m));
        var updateInput = mapper.Map<UpdateProductInput>(new UpdateProductRequest(1, "商品", 2m, 3));
        var output = mapper.Map<ProductOutput>(new Product(1, "商品", 1m));

        Assert.Equal(new CreateProductInput("商品", 1m), createInput);
        Assert.Equal(new UpdateProductInput("商品", 2m, 3), updateInput);
        Assert.Equal(1, output.Id);
        Assert.Equal("商品", output.Name);
        Assert.Equal(1m, output.Price);
        Assert.Equal(0, output.Version);
    }
}
