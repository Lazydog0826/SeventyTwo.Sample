namespace SeventyTwo.Sample.Domain.Permissions;

public sealed class PermissionDomainException(string message) : DomainException(message);
