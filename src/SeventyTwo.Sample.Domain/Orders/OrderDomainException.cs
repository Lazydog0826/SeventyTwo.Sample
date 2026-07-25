namespace SeventyTwo.Sample.Domain.Orders;

public sealed class OrderDomainException(string message) : Exception(message);
