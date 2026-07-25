namespace SeventyTwo.Sample.Domain.Products;

public sealed class ProductDomainException(string message) : Exception(message);
