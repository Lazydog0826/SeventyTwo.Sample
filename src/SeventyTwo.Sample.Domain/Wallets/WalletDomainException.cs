namespace SeventyTwo.Sample.Domain.Wallets;

public class WalletDomainException(string message, DomainErrorType errorType = DomainErrorType.Validation)
    : DomainException(message, errorType);
