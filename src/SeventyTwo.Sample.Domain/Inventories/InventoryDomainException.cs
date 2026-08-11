namespace SeventyTwo.Sample.Domain.Inventories;

public sealed class InventoryDomainException(string message, DomainErrorType errorType = DomainErrorType.Validation)
    : DomainException(message, errorType);
