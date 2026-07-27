using SeventyTwo.Sample.Domain.Wallets;

namespace SeventyTwo.Sample.Application.Wallets.BalanceChange;

public sealed record BalanceChangeInput(
    long CustomerId,
    string RequestNo,
    IReadOnlyCollection<BalanceChangeDetailInput> Details
);

public sealed record BalanceChangeDetailInput(WalletCurrency Currency, WalletChangeType ChangeType, decimal Amount);
