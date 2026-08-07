using SqlSugar;

namespace SeventyTwo.Sample.Infrastructure.Inventories;

internal static class InventoryLockInitializer
{
    public static Task EnsureCreatedAsync(
        ISqlSugarClient db,
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken
    )
    {
        var locks = keys.Select(x => new InventoryChangeLock { LockKey = x }).ToList();

        return db.Insertable(locks)
            .PostgreSQLConflictNothing(["lock_key"])
            .ExecuteCommandAsync(cancellationToken);
    }
}
