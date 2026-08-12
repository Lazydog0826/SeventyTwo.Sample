using SeventyTwo.Sample.Common.MessageKeys;
using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.Products;

namespace SeventyTwo.Sample.Domain.Tests;

public sealed class ProductTests
{
    [Fact]
    public void Create_ShouldTrimNameAndEnableProduct()
    {
        var product = new Product(Guid.CreateVersion7(), "  测试商品  ", 12.34m);

        Assert.Equal("测试商品", product.Name);
        Assert.Equal(12.34m, product.Price);
        Assert.True(product.Enable);
    }

    [Fact]
    public void Create_WithInvalidId_ShouldThrowDomainException()
    {
        var exception = Assert.Throws<ProductDomainException>(() => new Product(Guid.Empty, "测试商品", 1m));

        Assert.Equal(MessageKeys.Products.IdRequired, exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithEmptyName_ShouldThrowDomainException(string name)
    {
        var exception = Assert.Throws<ProductDomainException>(() =>
            new Product(Guid.CreateVersion7(), name, 1m)
        );

        Assert.Equal(MessageKeys.Products.NameRequired, exception.Message);
    }

    [Fact]
    public void Create_WithTooLongName_ShouldThrowDomainException()
    {
        var exception = Assert.Throws<ProductDomainException>(() =>
            new Product(Guid.CreateVersion7(), new string('a', 256), 1m)
        );

        Assert.Equal(MessageKeys.Products.NameTooLong, exception.Message);
    }

    [Fact]
    public void Create_WithNonPositivePrice_ShouldThrowDomainException()
    {
        var exception = Assert.Throws<ProductDomainException>(() =>
            new Product(Guid.CreateVersion7(), "测试商品", 0m)
        );

        Assert.Equal(MessageKeys.Products.PriceMustBePositive, exception.Message);
    }

    [Fact]
    public void Create_WithPriceOutOfRange_ShouldThrowDomainException()
    {
        var exception = Assert.Throws<ProductDomainException>(() =>
            new Product(Guid.CreateVersion7(), "测试商品", 10000000000000000m)
        );

        Assert.Equal(MessageKeys.Products.PriceOutOfRange, exception.Message);
    }

    [Fact]
    public void Create_WithMoreThanTwoDecimalPlaces_ShouldThrowDomainException()
    {
        var exception = Assert.Throws<ProductDomainException>(() =>
            new Product(Guid.CreateVersion7(), "测试商品", 1.001m)
        );

        Assert.Equal(MessageKeys.Products.PriceScaleInvalid, exception.Message);
    }

    [Fact]
    public void Update_ShouldChangeInfoAndAuditFields()
    {
        var product = new Product(Guid.CreateVersion7(), "旧商品", 1m)
        {
            Version = Guid.CreateVersion7(),
        };
        var updatedAt = new DateTimeOffset(2026, 7, 26, 10, 0, 0, TimeSpan.Zero);

        product.Update("  新商品  ", 2.5m, product.Version, SystemIds.System, updatedAt);

        Assert.Equal("新商品", product.Name);
        Assert.Equal(2.5m, product.Price);
        Assert.Equal(SystemIds.System, product.UpdatedBy);
        Assert.Equal(updatedAt, product.UpdatedAt);
    }

    [Fact]
    public void Update_WithInvalidInfo_ShouldNotChangeProduct()
    {
        var product = new Product(Guid.CreateVersion7(), "旧商品", 1m)
        {
            Version = Guid.CreateVersion7(),
        };

        _ = Assert.Throws<ProductDomainException>(() =>
            product.Update(
                "新商品",
                1.001m,
                product.Version,
                SystemIds.System,
                new DateTimeOffset(2026, 7, 26, 0, 0, 0, TimeSpan.Zero)
            )
        );

        Assert.Equal("旧商品", product.Name);
        Assert.Equal(1m, product.Price);
        Assert.Null(product.UpdatedAt);
    }

    [Fact]
    public void Update_WithExpiredVersion_ShouldNotChangeProduct()
    {
        var product = new Product(Guid.CreateVersion7(), "旧商品", 1m)
        {
            Version = Guid.CreateVersion7(),
        };

        var exception = Assert.Throws<ProductDomainException>(() =>
            product.Update(
                "新商品",
                2m,
                Guid.CreateVersion7(),
                SystemIds.System,
                new DateTimeOffset(2026, 7, 26, 0, 0, 0, TimeSpan.Zero)
            )
        );

        Assert.Equal(MessageKeys.Products.DataChanged, exception.Message);
        Assert.Equal(DomainErrorType.Conflict, exception.ErrorType);
        Assert.Equal("旧商品", product.Name);
        Assert.Equal(1m, product.Price);
    }

    [Fact]
    public void Update_WithMissingUpdatedAt_ShouldNotChangeProduct()
    {
        var product = new Product(Guid.CreateVersion7(), "旧商品", 1m)
        {
            Version = Guid.CreateVersion7(),
        };

        var exception = Assert.Throws<ProductDomainException>(() =>
            product.Update("新商品", 2m, product.Version, SystemIds.System, default)
        );

        Assert.Equal(MessageKeys.Products.ModifiedAtRequired, exception.Message);
        Assert.Equal("旧商品", product.Name);
        Assert.Equal(1m, product.Price);
        Assert.Null(product.UpdatedAt);
    }

    [Fact]
    public void Delete_ShouldDisableProductAndSetAuditFields()
    {
        var product = new Product(Guid.CreateVersion7(), "测试商品", 1m);
        var deletedAt = new DateTimeOffset(2026, 7, 26, 10, 0, 0, TimeSpan.Zero);

        product.Delete(SystemIds.System, deletedAt);

        Assert.False(product.Enable);
        Assert.Equal(SystemIds.System, product.DeleteBy);
        Assert.Equal(deletedAt, product.DeleteAt);
    }

    [Fact]
    public void Delete_WithMissingDeletedAt_ShouldNotChangeProduct()
    {
        var product = new Product(Guid.CreateVersion7(), "测试商品", 1m);

        var exception = Assert.Throws<ProductDomainException>(() => product.Delete(SystemIds.System, default));

        Assert.Equal(MessageKeys.Products.DeletedAtRequired, exception.Message);
        Assert.True(product.Enable);
        Assert.Null(product.DeleteAt);
    }
}
