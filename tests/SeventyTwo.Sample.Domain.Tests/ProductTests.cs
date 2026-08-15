using SeventyTwo.Sample.Common.MessageKeys;
using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.Products;

namespace SeventyTwo.Sample.Domain.Tests;

public sealed class ProductTests
{
    [Fact]
    public void Create_ShouldTrimNameAndEnableProduct()
    {
        var product = new Product(Guid.CreateVersion7(), "  测试商品  ", 12.34m, "  SKU-001  ");

        Assert.Equal("测试商品", product.Name);
        Assert.Equal(12.34m, product.Price);
        Assert.Equal("SKU-001", product.Code);
        Assert.True(product.Enable);
    }

    [Fact]
    public void Create_WithOptionalInfo_ShouldNormalizeAndAssignFields()
    {
        var categoryId = Guid.CreateVersion7();
        var product = new Product(
            Guid.CreateVersion7(),
            "测试商品",
            1m,
            "SKU-001",
            "  测试描述  ",
            "  件  ",
            categoryId
        );

        Assert.Equal("测试描述", product.Description);
        Assert.Equal("件", product.Unit);
        Assert.Equal(categoryId, product.CategoryId);
    }

    [Fact]
    public void Create_WithBlankOptionalInfo_ShouldNormalizeToNull()
    {
        var product = new Product(Guid.CreateVersion7(), "测试商品", 1m, "SKU-001", "  ", " ");

        Assert.Null(product.Description);
        Assert.Null(product.Unit);
        Assert.Null(product.CategoryId);
    }

    [Fact]
    public void Create_WithoutStatus_ShouldDefaultToOffShelf()
    {
        var product = new Product(Guid.CreateVersion7(), "测试商品", 1m, "SKU-001");

        Assert.Equal(ProductStatus.OffShelf, product.Status);
    }

    [Fact]
    public void Create_WithOnShelfStatus_ShouldAssignStatus()
    {
        var product = new Product(Guid.CreateVersion7(), "测试商品", 1m, "SKU-001", status: ProductStatus.OnShelf);

        Assert.Equal(ProductStatus.OnShelf, product.Status);
    }

    [Fact]
    public void Create_WithInvalidStatus_ShouldThrowDomainException()
    {
        var exception = Assert.Throws<ProductDomainException>(() =>
            new Product(Guid.CreateVersion7(), "测试商品", 1m, "SKU-001", status: (ProductStatus)5)
        );

        Assert.Equal(MessageKeys.Products.StatusInvalid, exception.Message);
    }

    [Fact]
    public void Create_WithInvalidId_ShouldThrowDomainException()
    {
        var exception = Assert.Throws<ProductDomainException>(() => new Product(Guid.Empty, "测试商品", 1m, "SKU-001"));

        Assert.Equal(MessageKeys.Products.IdRequired, exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithEmptyName_ShouldThrowDomainException(string name)
    {
        var exception = Assert.Throws<ProductDomainException>(() =>
            new Product(Guid.CreateVersion7(), name, 1m, "SKU-001")
        );

        Assert.Equal(MessageKeys.Products.NameRequired, exception.Message);
    }

    [Fact]
    public void Create_WithTooLongName_ShouldThrowDomainException()
    {
        var exception = Assert.Throws<ProductDomainException>(() =>
            new Product(Guid.CreateVersion7(), new string('a', 256), 1m, "SKU-001")
        );

        Assert.Equal(MessageKeys.Products.NameTooLong, exception.Message);
    }

    [Fact]
    public void Create_WithNonPositivePrice_ShouldThrowDomainException()
    {
        var exception = Assert.Throws<ProductDomainException>(() =>
            new Product(Guid.CreateVersion7(), "测试商品", 0m, "SKU-001")
        );

        Assert.Equal(MessageKeys.Products.PriceMustBePositive, exception.Message);
    }

    [Fact]
    public void Create_WithPriceOutOfRange_ShouldThrowDomainException()
    {
        var exception = Assert.Throws<ProductDomainException>(() =>
            new Product(Guid.CreateVersion7(), "测试商品", 10000000000000000m, "SKU-001")
        );

        Assert.Equal(MessageKeys.Products.PriceOutOfRange, exception.Message);
    }

    [Fact]
    public void Create_WithMoreThanTwoDecimalPlaces_ShouldThrowDomainException()
    {
        var exception = Assert.Throws<ProductDomainException>(() =>
            new Product(Guid.CreateVersion7(), "测试商品", 1.001m, "SKU-001")
        );

        Assert.Equal(MessageKeys.Products.PriceScaleInvalid, exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithEmptyCode_ShouldThrowDomainException(string code)
    {
        var exception = Assert.Throws<ProductDomainException>(() =>
            new Product(Guid.CreateVersion7(), "测试商品", 1m, code)
        );

        Assert.Equal(MessageKeys.Products.CodeRequired, exception.Message);
    }

    [Fact]
    public void Create_WithTooLongCode_ShouldThrowDomainException()
    {
        var exception = Assert.Throws<ProductDomainException>(() =>
            new Product(Guid.CreateVersion7(), "测试商品", 1m, new string('a', 65))
        );

        Assert.Equal(MessageKeys.Products.CodeTooLong, exception.Message);
    }

    [Fact]
    public void Create_WithTooLongDescription_ShouldThrowDomainException()
    {
        var exception = Assert.Throws<ProductDomainException>(() =>
            new Product(Guid.CreateVersion7(), "测试商品", 1m, "SKU-001", new string('a', 2001))
        );

        Assert.Equal(MessageKeys.Products.DescriptionTooLong, exception.Message);
    }

    [Fact]
    public void Create_WithTooLongUnit_ShouldThrowDomainException()
    {
        var exception = Assert.Throws<ProductDomainException>(() =>
            new Product(Guid.CreateVersion7(), "测试商品", 1m, "SKU-001", unit: new string('a', 21))
        );

        Assert.Equal(MessageKeys.Products.UnitTooLong, exception.Message);
    }

    [Fact]
    public void Update_ShouldChangeInfoAndAuditFields()
    {
        var categoryId = Guid.CreateVersion7();
        var product = new Product(Guid.CreateVersion7(), "旧商品", 1m, "SKU-001") { Version = Guid.CreateVersion7() };
        var updatedAt = new DateTimeOffset(2026, 7, 26, 10, 0, 0, TimeSpan.Zero);

        product.Update(
            "  新商品  ",
            2.5m,
            "SKU-002",
            "新描述",
            "箱",
            categoryId,
            ProductStatus.OnShelf,
            product.Version,
            SystemIds.System,
            updatedAt
        );

        Assert.Equal("新商品", product.Name);
        Assert.Equal(2.5m, product.Price);
        Assert.Equal("SKU-002", product.Code);
        Assert.Equal("新描述", product.Description);
        Assert.Equal("箱", product.Unit);
        Assert.Equal(categoryId, product.CategoryId);
        Assert.Equal(ProductStatus.OnShelf, product.Status);
        Assert.Equal(SystemIds.System, product.UpdatedBy);
        Assert.Equal(updatedAt, product.UpdatedAt);
    }

    [Fact]
    public void Update_WithInvalidInfo_ShouldNotChangeProduct()
    {
        var product = new Product(Guid.CreateVersion7(), "旧商品", 1m, "SKU-001") { Version = Guid.CreateVersion7() };

        _ = Assert.Throws<ProductDomainException>(() =>
            product.Update(
                "新商品",
                1.001m,
                "SKU-002",
                null,
                null,
                null,
                ProductStatus.OnShelf,
                product.Version,
                SystemIds.System,
                new DateTimeOffset(2026, 7, 26, 0, 0, 0, TimeSpan.Zero)
            )
        );

        Assert.Equal("旧商品", product.Name);
        Assert.Equal(1m, product.Price);
        Assert.Equal("SKU-001", product.Code);
        Assert.Equal(ProductStatus.OffShelf, product.Status);
        Assert.Null(product.UpdatedAt);
    }

    [Fact]
    public void Update_WithExpiredVersion_ShouldNotChangeProduct()
    {
        var product = new Product(Guid.CreateVersion7(), "旧商品", 1m, "SKU-001") { Version = Guid.CreateVersion7() };

        var exception = Assert.Throws<ProductDomainException>(() =>
            product.Update(
                "新商品",
                2m,
                "SKU-002",
                null,
                null,
                null,
                ProductStatus.OnShelf,
                Guid.CreateVersion7(),
                SystemIds.System,
                new DateTimeOffset(2026, 7, 26, 0, 0, 0, TimeSpan.Zero)
            )
        );

        Assert.Equal(MessageKeys.Products.DataChanged, exception.Message);
        Assert.Equal(DomainErrorType.Conflict, exception.ErrorType);
        Assert.Equal("旧商品", product.Name);
        Assert.Equal(1m, product.Price);
        Assert.Equal("SKU-001", product.Code);
        Assert.Equal(ProductStatus.OffShelf, product.Status);
    }

    [Fact]
    public void Update_WithMissingUpdatedAt_ShouldNotChangeProduct()
    {
        var product = new Product(Guid.CreateVersion7(), "旧商品", 1m, "SKU-001") { Version = Guid.CreateVersion7() };

        var exception = Assert.Throws<ProductDomainException>(() =>
            product.Update(
                "新商品",
                2m,
                "SKU-002",
                null,
                null,
                null,
                ProductStatus.OnShelf,
                product.Version,
                SystemIds.System,
                default
            )
        );

        Assert.Equal(MessageKeys.Products.ModifiedAtRequired, exception.Message);
        Assert.Equal("旧商品", product.Name);
        Assert.Equal(1m, product.Price);
        Assert.Equal("SKU-001", product.Code);
        Assert.Equal(ProductStatus.OffShelf, product.Status);
        Assert.Null(product.UpdatedAt);
    }

    [Fact]
    public void ChangeStatus_ShouldChangeStatusAndAuditFields()
    {
        var product = new Product(Guid.CreateVersion7(), "测试商品", 1m, "SKU-001") { Version = Guid.CreateVersion7() };
        var updatedAt = new DateTimeOffset(2026, 7, 26, 10, 0, 0, TimeSpan.Zero);

        product.ChangeStatus(ProductStatus.OnShelf, product.Version, SystemIds.System, updatedAt);

        Assert.Equal(ProductStatus.OnShelf, product.Status);
        Assert.Equal(SystemIds.System, product.UpdatedBy);
        Assert.Equal(updatedAt, product.UpdatedAt);
    }

    [Fact]
    public void ChangeStatus_WithInvalidStatus_ShouldNotChangeProduct()
    {
        var product = new Product(Guid.CreateVersion7(), "测试商品", 1m, "SKU-001") { Version = Guid.CreateVersion7() };

        var exception = Assert.Throws<ProductDomainException>(() =>
            product.ChangeStatus((ProductStatus)5, product.Version, SystemIds.System, DateTimeOffset.UtcNow)
        );

        Assert.Equal(MessageKeys.Products.StatusInvalid, exception.Message);
        Assert.Equal(ProductStatus.OffShelf, product.Status);
    }

    [Fact]
    public void ChangeStatus_WithExpiredVersion_ShouldNotChangeProduct()
    {
        var product = new Product(Guid.CreateVersion7(), "测试商品", 1m, "SKU-001") { Version = Guid.CreateVersion7() };

        var exception = Assert.Throws<ProductDomainException>(() =>
            product.ChangeStatus(ProductStatus.OnShelf, Guid.CreateVersion7(), SystemIds.System, DateTimeOffset.UtcNow)
        );

        Assert.Equal(MessageKeys.Products.DataChanged, exception.Message);
        Assert.Equal(ProductStatus.OffShelf, product.Status);
    }

    [Fact]
    public void EnsureCanDelete_WithCurrentVersion_ShouldNotThrow()
    {
        var product = new Product(Guid.CreateVersion7(), "测试商品", 1m, "SKU-001") { Version = Guid.CreateVersion7() };

        product.EnsureCanDelete(product.Version);

        Assert.True(product.Enable);
    }

    [Fact]
    public void EnsureCanDelete_WithExpiredVersion_ShouldThrowDomainException()
    {
        var product = new Product(Guid.CreateVersion7(), "测试商品", 1m, "SKU-001") { Version = Guid.CreateVersion7() };

        var exception = Assert.Throws<ProductDomainException>(() => product.EnsureCanDelete(Guid.CreateVersion7()));

        Assert.Equal(MessageKeys.Products.DataChanged, exception.Message);
        Assert.Equal(DomainErrorType.Conflict, exception.ErrorType);
    }
}
