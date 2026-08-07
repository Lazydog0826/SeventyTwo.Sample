namespace SeventyTwo.Sample.Domain.Organizations;

public sealed class OrganizationDomainException(string message) : Exception(message);
