namespace SeventyTwo.Sample.Domain.Permissions;

public sealed class PermissionDomainException(string message, DomainErrorType errorType = DomainErrorType.Validation)
    : DomainException(message, errorType);
