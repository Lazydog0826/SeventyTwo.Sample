using SqlSugar;

namespace SeventyTwo.Sample.Infrastructure.Wallets;

internal static class WalletLockInitializer
{
    public static Task EnsureCreatedAsync(
        ISqlSugarClient db,
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken
    )
    {
        var locks = keys.Select(x => new WalletChangeLock { LockKey = x }).ToList();

        return db.Insertable(locks)
            .PostgreSQLConflictNothing(["lock_key"])
            .ExecuteCommandAsync(cancellationToken);
    }
}
