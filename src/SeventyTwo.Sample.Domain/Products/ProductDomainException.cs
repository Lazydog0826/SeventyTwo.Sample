namespace SeventyTwo.Sample.Domain.Products;

public sealed class ProductDomainException(string message, DomainErrorType errorType = DomainErrorType.Validation)
    : DomainException(message, errorType);

public sealed class ProductNotFoundException()
    : DomainException(MessageKeys.Products.NotFound, DomainErrorType.NotFound);
