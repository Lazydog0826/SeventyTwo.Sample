// ReSharper disable ClassNeverInstantiated.Global
namespace SeventyTwo.Sample.Domain.Orders;

public sealed class OrderDomainException(string message, DomainErrorType errorType = DomainErrorType.Validation)
    : DomainException(message, errorType);
