namespace SeventyTwo.Sample.Domain.Wallets;

public readonly record struct Money
{
    private const decimal MaxAmount = 9999999999999999.99m;

    public Money(decimal value)
    {
        // ReSharper disable once ConvertIfStatementToSwitchStatement
        if (value < 0)
        {
            throw new WalletDomainException(MessageKeys.Wallets.AmountMustNotBeNegative);
        }

        if (value > MaxAmount)
        {
            throw new WalletDomainException(MessageKeys.Wallets.AmountOutOfRange);
        }

        if (decimal.Round(value, 2) != value)
        {
            throw new WalletDomainException(MessageKeys.Wallets.AmountScaleInvalid);
        }

        Value = value;
    }

    public decimal Value { get; }

    public bool IsZero => Value == 0;

    public Money Add(Money amount)
    {
        return Value > MaxAmount - amount.Value
            ? throw new WalletDomainException(MessageKeys.Wallets.AmountOutOfRange)
            : new Money(Value + amount.Value);
    }

    public Money Subtract(Money amount)
    {
        return amount.Value > Value
            ? throw new WalletDomainException(MessageKeys.Wallets.InsufficientBalance, DomainErrorType.Conflict)
            : new Money(Value - amount.Value);
    }
}
