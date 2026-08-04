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

        Assert.Equal("商品 ID 不能为空", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithEmptyName_ShouldThrowDomainException(string name)
    {
        var exception = Assert.Throws<ProductDomainException>(() =>
            new Product(Guid.CreateVersion7(), name, 1m)
        );

        Assert.Equal("商品名称不能为空", exception.Message);
    }

    [Fact]
    public void Create_WithTooLongName_ShouldThrowDomainException()
    {
        var exception = Assert.Throws<ProductDomainException>(() =>
            new Product(Guid.CreateVersion7(), new string('a', 256), 1m)
        );

        Assert.Equal("商品名称长度不能超过 255 个字符", exception.Message);
    }

    [Fact]
    public void Create_WithNonPositivePrice_ShouldThrowDomainException()
    {
        var exception = Assert.Throws<ProductDomainException>(() =>
            new Product(Guid.CreateVersion7(), "测试商品", 0m)
        );

        Assert.Equal("商品价格必须大于 0", exception.Message);
    }

    [Fact]
    public void Create_WithPriceOutOfRange_ShouldThrowDomainException()
    {
        var exception = Assert.Throws<ProductDomainException>(() =>
            new Product(Guid.CreateVersion7(), "测试商品", 10000000000000000m)
        );

        Assert.Equal("商品价格超出范围", exception.Message);
    }

    [Fact]
    public void Create_WithMoreThanTwoDecimalPlaces_ShouldThrowDomainException()
    {
        var exception = Assert.Throws<ProductDomainException>(() =>
            new Product(Guid.CreateVersion7(), "测试商品", 1.001m)
        );

        Assert.Equal("商品价格最多保留两位小数", exception.Message);
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

        Assert.Equal("商品数据已变更，请刷新后重试", exception.Message);
        Assert.Equal("旧商品", product.Name);
        Assert.Equal(1m, product.Price);
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
}
