namespace SeventyTwo.Sample.Domain.Users;

public sealed class UserDomainException(string message, DomainErrorType errorType = DomainErrorType.Validation)
    : DomainException(message, errorType);
