namespace SeventyTwo.Sample.Domain.DataDictionaries;

public sealed class DataDictionaryDomainException(
    string message,
    DomainErrorType errorType = DomainErrorType.Validation
) : DomainException(message, errorType);
