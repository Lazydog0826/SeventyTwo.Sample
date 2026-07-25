namespace SeventyTwo.Sample.Domain.Orders;

public enum OrderType : short
{
    Sales = 1,
    Return = 2,
    Transfer = 3,
}

public enum OrderStatus : short
{
    PendingReview = 0,
    Approved = 1,
    Processing = 2,
    Completed = 3,
    Cancelled = 4,
}

public enum PaymentStatus : short
{
    Unpaid = 0,
    PartiallyPaid = 1,
    Paid = 2,
}

public enum ShippingStatus : short
{
    Unshipped = 0,
    PartiallyShipped = 1,
    Shipped = 2,
}
