// ReSharper disable ClassNeverInstantiated.Global
namespace SeventyTwo.Sample.Domain.Orders;

public sealed class OrderDomainException(string message) : Exception(message);
