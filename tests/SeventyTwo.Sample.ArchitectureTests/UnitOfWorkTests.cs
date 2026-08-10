using System.Reflection;
using DotNetCore.CAP;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.DependencyInjection;
using SeventyTwo.Sample.Domain.Permissions;
using SeventyTwo.Sample.Infrastructure;
using SeventyTwo.Sample.Infrastructure.Permissions;
using SqlSugar;

namespace SeventyTwo.Sample.ArchitectureTests;

public sealed class UnitOfWorkTests
{
    [Fact]
    public async Task NestedUnitOfWork_ShouldCommitOnlyOnce()
    {
        using var fixture = CreateFixture();

        await fixture.UnitOfWork.ExecuteAsync(
            async () =>
            {
                await fixture.Database.Insertable(new TransactionRecord { Id = 1 }).ExecuteCommandAsync();
                await fixture.UnitOfWork.ExecuteAsync(
                    () => fixture.Database.Insertable(new TransactionRecord { Id = 2 }).ExecuteCommandAsync(),
                    CancellationToken.None
                );
            },
            CancellationToken.None
        );

        Assert.Equal(2, await fixture.Database.Queryable<TransactionRecord>().CountAsync());
        Assert.Null(fixture.Database.Ado.Transaction);
        Assert.Null(fixture.Publisher.Transaction);
    }

    [Fact]
    public async Task NestedUnitOfWorkFailure_ShouldRollbackEvenWhenCaughtByOuterAction()
    {
        using var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.UnitOfWork.ExecuteAsync(
                async () =>
                {
                    await fixture.Database.Insertable(new TransactionRecord { Id = 1 }).ExecuteCommandAsync();
                    try
                    {
                        await fixture.UnitOfWork.ExecuteAsync(
                            async () =>
                            {
                                await fixture.Database.Insertable(new TransactionRecord { Id = 2 })
                                    .ExecuteCommandAsync();
                                throw new TestTransactionException();
                            },
                            CancellationToken.None
                        );
                    }
                    catch (TestTransactionException)
                    {
                        // 外层即使处理了业务异常，事务也必须保持 rollback-only。
                    }
                },
                CancellationToken.None
            )
        );

        Assert.IsType<TestTransactionException>(exception.InnerException);
        Assert.Equal(0, await fixture.Database.Queryable<TransactionRecord>().CountAsync());
        Assert.Null(fixture.Database.Ado.Transaction);
        Assert.Null(fixture.Publisher.Transaction);
    }

    [Fact]
    public async Task PublishFailure_ShouldRollbackBusinessChanges()
    {
        using var fixture = CreateFixture();

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            fixture.UnitOfWork.ExecuteAsync(
                async () =>
                {
                    await fixture.Database.Insertable(new TransactionRecord { Id = 1 }).ExecuteCommandAsync();
                    await fixture.Publisher.PublishAsync("tests.publish.failure", new { Id = 1 });
                },
                CancellationToken.None
            )
        );

        Assert.Equal(0, await fixture.Database.Queryable<TransactionRecord>().CountAsync());
    }

    [Fact]
    public async Task PermissionDelete_ShouldJoinOuterTransactionWithoutCommittingIt()
    {
        using var fixture = CreateFixture();
        fixture.Database.CodeFirst.InitTables<PermissionRecord, UserPermissionRecord>();
        var permissionId = Guid.CreateVersion7();
        await fixture.Database.Insertable(CreatePermissionRecord(permissionId)).ExecuteCommandAsync();
        await fixture
            .Database.Insertable(
                new UserPermissionRecord { UserId = Guid.CreateVersion7(), PermissionId = permissionId }
            )
            .ExecuteCommandAsync();
        var repository = new PermissionRepository(fixture.Database);

        await Assert.ThrowsAsync<TestTransactionException>(() =>
            fixture.UnitOfWork.ExecuteAsync(
                async () =>
                {
                    await repository.DeleteAsync(permissionId, CancellationToken.None);
                    throw new TestTransactionException();
                },
                CancellationToken.None
            )
        );

        Assert.Equal(1, await fixture.Database.Queryable<PermissionRecord>().CountAsync());
        Assert.Equal(1, await fixture.Database.Queryable<UserPermissionRecord>().CountAsync());
    }

    [Fact]
    public async Task PreExistingTransaction_ShouldBeRejected()
    {
        using var fixture = CreateFixture();
        fixture.Database.Ado.BeginTran();
        try
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                fixture.UnitOfWork.ExecuteAsync(() => Task.CompletedTask, CancellationToken.None)
            );

            Assert.Contains("禁止在工作单元外部开启事务", exception.Message);
        }
        finally
        {
            fixture.Database.Ado.RollbackTran();
        }
    }

    private static UnitOfWorkFixture CreateFixture()
    {
        var database = new SqlSugarClient(
            new ConnectionConfig
            {
                DbType = DbType.Sqlite,
                ConnectionString = $"Data Source={Path.Combine(Path.GetTempPath(), $"unit-of-work-{Guid.NewGuid():N}.db")};Pooling=False",
                IsAutoCloseConnection = false,
            }
        );
        database.CodeFirst.InitTables<TransactionRecord>();

        var dispatcher = DispatchProxy.Create<IDispatcher, NoOpDispatchProxy>();
        var serviceProvider = new ServiceCollection().AddSingleton(dispatcher).BuildServiceProvider();
        var publisher = DispatchProxy.Create<ICapPublisher, CapPublisherDispatchProxy>();
        ((CapPublisherDispatchProxy)(object)publisher).ServiceProvider = serviceProvider;
        return new UnitOfWorkFixture(database, publisher, serviceProvider);
    }

    private static PermissionRecord CreatePermissionRecord(Guid permissionId)
    {
        return new PermissionRecord
        {
            Id = permissionId,
            Code = "Tests.Permission",
            Title = "测试权限",
            Type = PermissionType.Page,
            VueComponentPath = "/src/views/tests.vue",
            RoutePath = "/tests",
            RouteName = "Tests",
            MetaData = new PermissionMetaData(true),
        };
    }

    [SugarTable("unit_of_work_test")]
    private sealed class TransactionRecord
    {
        [SugarColumn(IsPrimaryKey = true)]
        public int Id { get; init; }
    }

    private sealed class TestTransactionException : Exception;

    private class NoOpDispatchProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            throw new NotSupportedException(targetMethod?.Name);
        }
    }

    private class CapPublisherDispatchProxy : DispatchProxy
    {
        public IServiceProvider ServiceProvider { get; set; } = null!;

        public ICapTransaction? Transaction { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            return targetMethod.Name switch
            {
                "get_ServiceProvider" => ServiceProvider,
                "get_Transaction" => Transaction,
                "set_Transaction" => Transaction = (ICapTransaction?)args?[0],
                _ => throw new NotSupportedException(targetMethod.Name),
            };
        }
    }

    private sealed class UnitOfWorkFixture(
        SqlSugarClient database,
        ICapPublisher publisher,
        ServiceProvider serviceProvider
    ) : IDisposable
    {
        private readonly string databasePath = database.CurrentConnectionConfig.ConnectionString
            .Split(';', StringSplitOptions.RemoveEmptyEntries)[0]["Data Source=".Length..];

        public SqlSugarClient Database { get; } = database;

        public ICapPublisher Publisher { get; } = publisher;

        public UnitOfWork UnitOfWork { get; } = new(database, publisher);

        public void Dispose()
        {
            Database.Ado.Close();
            Database.Dispose();
            serviceProvider.Dispose();
            File.Delete(databasePath);
        }
    }
}
