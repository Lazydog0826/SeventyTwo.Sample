namespace SeventyTwo.Sample.Domain.Orders;

public enum OrderType : short
{
    Sales = 1,
    Return = 2,
    Transfer = 3,
}

public enum OrderStatus : short
{
    Pending = 0,
    Processing = 1,
    Processed = 2,
    Cancelled = 3,
}
