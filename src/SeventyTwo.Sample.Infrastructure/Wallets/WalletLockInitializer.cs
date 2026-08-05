using System.Text;
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

        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        return db.CurrentConnectionConfig.DbType switch
        {
            DbType.PostgreSQL => db.Insertable(locks)
                .PostgreSQLConflictNothing(["lock_key"])
                .ExecuteCommandAsync(cancellationToken),
            DbType.MySql => db.Insertable(locks).MySqlIgnore().ExecuteCommandAsync(cancellationToken),
            DbType.SqlServer => EnsureSqlServerLocksCreatedAsync(db, locks, cancellationToken),
            _ => throw new NotSupportedException($"不支持的数据库类型：{db.CurrentConnectionConfig.DbType}"),
        };
    }

    private static async Task EnsureSqlServerLocksCreatedAsync(
        ISqlSugarClient db,
        List<WalletChangeLock> locks,
        CancellationToken cancellationToken
    )
    {
        var sql = new StringBuilder(
            """
            set xact_abort on;
            begin try
                begin transaction;
            """
        );
        var parameters = new List<SugarParameter>();

        for (var index = 0; index < locks.Count; index++)
        {
            var parameterName = $"LockKey{index}";
            parameters.Add(
                new SugarParameter($"@{parameterName}", locks[index].LockKey, System.Data.DbType.AnsiString)
                {
                    Size = 255,
                }
            );
            sql.AppendLine(
                $"""
                    if not exists (
                        select 1
                        from wallet_change_lock with (updlock, holdlock)
                        where lock_key = @{parameterName}
                    )
                    begin
                        insert into wallet_change_lock (lock_key) values (@{parameterName});
                    end;
                """
            );
        }

        sql.AppendLine(
            """
                commit transaction;
            end try
            begin catch
                if @@trancount > 0
                    rollback transaction;
                throw;
            end catch;
            """
        );

        await db.Ado.ExecuteCommandAsync(sql.ToString(), parameters.ToArray(), cancellationToken);
    }
}
