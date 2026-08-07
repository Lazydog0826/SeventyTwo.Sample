namespace SeventyTwo.Sample.Domain.Products;

public sealed class ProductDomainException(string message) : DomainException(message);

public sealed class ProductNotFoundException() : DomainException("商品不存在");
