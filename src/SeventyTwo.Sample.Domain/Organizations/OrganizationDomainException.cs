namespace SeventyTwo.Sample.Domain.Organizations;

public sealed class OrganizationDomainException(
    string message,
    DomainErrorType errorType = DomainErrorType.Validation
) : DomainException(message, errorType);
