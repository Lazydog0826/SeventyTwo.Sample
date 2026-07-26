namespace SeventyTwo.Sample.Domain.Products;

public sealed class ProductDomainException(string message) : Exception(message);

public sealed class ProductNotFoundException() : Exception("商品不存在");
