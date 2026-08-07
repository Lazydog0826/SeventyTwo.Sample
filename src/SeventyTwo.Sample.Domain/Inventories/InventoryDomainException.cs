namespace SeventyTwo.Sample.Domain.Inventories;

public sealed class InventoryDomainException(string message) : DomainException(message);
