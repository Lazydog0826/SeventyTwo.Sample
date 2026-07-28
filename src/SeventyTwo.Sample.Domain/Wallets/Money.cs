namespace SeventyTwo.Sample.Domain.Wallets;

public readonly record struct Money
{
    private const decimal MaxAmount = 9999999999999999.99m;

    public Money(decimal value)
    {
        // ReSharper disable once ConvertIfStatementToSwitchStatement
        if (value < 0)
        {
            throw new WalletDomainException("金额不能小于 0");
        }

        if (value > MaxAmount)
        {
            throw new WalletDomainException("金额超出范围");
        }

        if (decimal.Round(value, 2) != value)
        {
            throw new WalletDomainException("金额最多保留两位小数");
        }

        Value = value;
    }

    public decimal Value { get; }

    public bool IsZero => Value == 0;

    public Money Add(Money amount)
    {
        return Value > MaxAmount - amount.Value
            ? throw new WalletDomainException("金额超出范围")
            : new Money(Value + amount.Value);
    }

    public Money Subtract(Money amount)
    {
        return amount.Value > Value ? throw new WalletDomainException("余额不足") : new Money(Value - amount.Value);
    }
}
