namespace SeventyTwo.Sample.Domain.Users;

public sealed class UserDomainException(string message) : Exception(message);
