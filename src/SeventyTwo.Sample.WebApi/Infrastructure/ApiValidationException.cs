namespace SeventyTwo.Sample.WebApi.Infrastructure;

public sealed class ApiValidationException(string message) : Exception(message);
