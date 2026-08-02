using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.Products;

namespace SeventyTwo.Sample.Domain.Tests;

public sealed class ProductTests
{
    [Fact]
    public void Create_ShouldTrimNameAndEnableProduct()
    {
        var product = new Product("01ARZ3NDEKTSV4RRFFQ69G5FAV", "  测试商品  ", 12.34m);

        Assert.Equal("测试商品", product.Name);
        Assert.Equal(12.34m, product.Price);
        Assert.True(product.Enable);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithInvalidId_ShouldThrowDomainException(string id)
    {
        var exception = Assert.Throws<ProductDomainException>(() => new Product(id, "测试商品", 1m));

        Assert.Equal("商品 ID 不能为空", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithEmptyName_ShouldThrowDomainException(string name)
    {
        var exception = Assert.Throws<ProductDomainException>(() =>
            new Product("01ARZ3NDEKTSV4RRFFQ69G5FAV", name, 1m)
        );

        Assert.Equal("商品名称不能为空", exception.Message);
    }

    [Fact]
    public void Create_WithTooLongName_ShouldThrowDomainException()
    {
        var exception = Assert.Throws<ProductDomainException>(() =>
            new Product("01ARZ3NDEKTSV4RRFFQ69G5FAV", new string('a', 256), 1m)
        );

        Assert.Equal("商品名称长度不能超过 255 个字符", exception.Message);
    }

    [Fact]
    public void Create_WithNonPositivePrice_ShouldThrowDomainException()
    {
        var exception = Assert.Throws<ProductDomainException>(() =>
            new Product("01ARZ3NDEKTSV4RRFFQ69G5FAV", "测试商品", 0m)
        );

        Assert.Equal("商品价格必须大于 0", exception.Message);
    }

    [Fact]
    public void Create_WithPriceOutOfRange_ShouldThrowDomainException()
    {
        var exception = Assert.Throws<ProductDomainException>(() =>
            new Product("01ARZ3NDEKTSV4RRFFQ69G5FAV", "测试商品", 10000000000000000m)
        );

        Assert.Equal("商品价格超出范围", exception.Message);
    }

    [Fact]
    public void Create_WithMoreThanTwoDecimalPlaces_ShouldThrowDomainException()
    {
        var exception = Assert.Throws<ProductDomainException>(() =>
            new Product("01ARZ3NDEKTSV4RRFFQ69G5FAV", "测试商品", 1.001m)
        );

        Assert.Equal("商品价格最多保留两位小数", exception.Message);
    }

    [Fact]
    public void Update_ShouldChangeInfoAndAuditFields()
    {
        var product = new Product("01ARZ3NDEKTSV4RRFFQ69G5FAV", "旧商品", 1m)
        {
            Version = "01ARZ3NDEKTSV4RRFFQ69G5FB0",
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
        var product = new Product("01ARZ3NDEKTSV4RRFFQ69G5FAV", "旧商品", 1m)
        {
            Version = "01ARZ3NDEKTSV4RRFFQ69G5FB0",
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
        var product = new Product("01ARZ3NDEKTSV4RRFFQ69G5FAV", "旧商品", 1m)
        {
            Version = "01ARZ3NDEKTSV4RRFFQ69G5FB0",
        };

        var exception = Assert.Throws<ProductDomainException>(() =>
            product.Update(
                "新商品",
                2m,
                "01ARZ3NDEKTSV4RRFFQ69G5FAW",
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
        var product = new Product("01ARZ3NDEKTSV4RRFFQ69G5FAV", "测试商品", 1m);
        var deletedAt = new DateTimeOffset(2026, 7, 26, 10, 0, 0, TimeSpan.Zero);

        product.Delete(SystemIds.System, deletedAt);

        Assert.False(product.Enable);
        Assert.Equal(SystemIds.System, product.DeleteBy);
        Assert.Equal(deletedAt, product.DeleteAt);
    }
}
