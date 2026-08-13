using System.Reflection;
using System.Text.Json;
using DotNetCore.CAP;
using DotNetCore.CAP.Transport;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SeventyTwo.Sample.Application.Authentication;
using SeventyTwo.Sample.Application.Users;
using SeventyTwo.Sample.Domain.Permissions;
using SeventyTwo.Sample.Domain.Users;
using SeventyTwo.Sample.Infrastructure;
using SeventyTwo.Sample.Infrastructure.Permissions;
using SeventyTwo.Sample.Infrastructure.Users;
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

    [Fact]
    public async Task UserDelete_ShouldHoldPostgreSqlSecurityLockUntilCommitAndRejectWaitingLogin()
    {
        var connectionString = GetRemotePostgreSqlConnectionString();
        using var setup = CreatePostgreSqlDatabase(connectionString);
        using var deleteFixture = CreatePostgreSqlFixture(connectionString);
        using var loginFixture = CreatePostgreSqlFixture(connectionString);
        setup.CodeFirst.InitTables<UserAccountRecord>();
        var userId = Guid.CreateVersion7();
        var version = Guid.CreateVersion7();
        var username = $"integration-delete-{userId:N}";
        const string password = "integration-password";
        var passwordHash = new PasswordHasher<string>().HashPassword(username, password);
        var organizationId = await setup.Queryable<OrganizationIdRecord>()
            .Where(x => x.DeleteAt == null)
            .Select(x => x.Id)
            .FirstAsync();
        await setup.Insertable(
                new UserAccountRecord
                {
                    Id = userId,
                    Username = username,
                    PasswordHash = passwordHash,
                    DisplayName = "并发删除集成测试用户",
                    Phone = "13800000000",
                    Email = $"{userId:N}@example.com",
                    OrgId = organizationId,
                    Version = version,
                }
            )
            .ExecuteCommandAsync();

        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        BlockingTokenCacheService? deletionGate = null;
        Task? deleteTask = null;
        Task? loginTask = null;
        try
        {
            deletionGate = new BlockingTokenCacheService();
            var deleteApplication = new UserApplication(
                new UserRepository(deleteFixture.Database),
                null!,
                null!,
                deleteFixture.UnitOfWork,
                null!,
                deletionGate,
                new NoOpUserInfoCacheInvalidationPublisher(),
                null!
            );
            var loginRepository = new LockRequestSignalingUserRepository(
                new UserRepository(loginFixture.Database)
            );
            var loginApplication = new UserApplication(
                loginRepository,
                null!,
                null!,
                loginFixture.UnitOfWork,
                new FixedTokenService(),
                new SuccessfulTokenCacheService(),
                null!,
                null!
            );

            deleteTask = deleteApplication.DeleteAsync(userId, version, cancellationTokenSource.Token);
            await deletionGate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            loginTask = loginApplication.LoginAsync(
                new(username, password),
                cancellationTokenSource.Token
            );
            await loginRepository.LockRequested.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(loginTask.IsCompleted);

            deletionGate.Release();
            await deleteTask.WaitAsync(TimeSpan.FromSeconds(10));
            var exception = await Assert.ThrowsAsync<UserDomainException>(() =>
                loginTask.WaitAsync(TimeSpan.FromSeconds(10))
            );
            Assert.Equal(Common.MessageKeys.MessageKeys.Users.CredentialsInvalid, exception.Message);
        }
        finally
        {
            deletionGate?.Release();
            await cancellationTokenSource.CancelAsync();
            await ObserveCompletionAsync(deleteTask);
            await ObserveCompletionAsync(loginTask);
            await setup.Deleteable<UserPermissionRecord>().Where(x => x.UserId == userId).ExecuteCommandAsync();
            await setup.Deleteable<UserAccountRecord>().Where(x => x.Id == userId).ExecuteCommandAsync();
        }
    }

    private static async Task ObserveCompletionAsync(Task? task)
    {
        if (task is null)
            return;
        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (Exception)
        {
            // 主测试路径负责断言异常；清理阶段只确保后台事务退出并释放数据库锁。
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

    private static UnitOfWorkFixture CreatePostgreSqlFixture(string connectionString)
    {
        var database = CreatePostgreSqlDatabase(connectionString);
        var dispatcher = DispatchProxy.Create<IDispatcher, NoOpDispatchProxy>();
        var serviceProvider = new ServiceCollection().AddSingleton(dispatcher).BuildServiceProvider();
        var publisher = DispatchProxy.Create<ICapPublisher, CapPublisherDispatchProxy>();
        ((CapPublisherDispatchProxy)(object)publisher).ServiceProvider = serviceProvider;
        return new UnitOfWorkFixture(database, publisher, serviceProvider);
    }

    private static SqlSugarClient CreatePostgreSqlDatabase(string connectionString) =>
        new(
            new ConnectionConfig
            {
                DbType = DbType.PostgreSQL,
                ConnectionString = connectionString,
                IsAutoCloseConnection = false,
            }
        );

    private static string GetRemotePostgreSqlConnectionString()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var path = Path.Combine(directory.FullName, "src", "SeventyTwo.Sample.WebApi", "appsettings.json");
            if (File.Exists(path))
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                return document.RootElement.GetProperty("ConnectionStrings").GetProperty("PostgreSQL").GetString()
                    ?? throw new InvalidOperationException("未配置远程 PostgreSQL 连接字符串");
            }
            directory = directory.Parent;
        }
        throw new InvalidOperationException("未找到 WebApi appsettings.json");
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
            Path = permissionId.ToString(),
        };
    }

    [SugarTable("unit_of_work_test")]
    private sealed class TransactionRecord
    {
        [SugarColumn(IsPrimaryKey = true)]
        public int Id { get; init; }
    }

    private sealed class TestTransactionException : Exception;

    [SugarTable("organization")]
    private sealed class OrganizationIdRecord
    {
        [SugarColumn(ColumnName = "id")]
        public Guid Id { get; init; }

        [SugarColumn(ColumnName = "delete_at")]
        public DateTimeOffset? DeleteAt { get; init; }
    }

    private sealed class LockRequestSignalingUserRepository(IUserRepository inner) : IUserRepository
    {
        public TaskCompletionSource LockRequested { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task AcquireSecurityLockAsync(Guid id, CancellationToken cancellationToken)
        {
            LockRequested.TrySetResult();
            await inner.AcquireSecurityLockAsync(id, cancellationToken);
        }

        public Task<User?> GetAsync(Guid id, CancellationToken cancellationToken) =>
            inner.GetAsync(id, cancellationToken);
        public Task<User?> GetByAccountAsync(string account, CancellationToken cancellationToken) =>
            inner.GetByAccountAsync(account, cancellationToken);
        public Task<IReadOnlyList<User>> GetListAsync(CancellationToken cancellationToken) =>
            inner.GetListAsync(cancellationToken);
        public Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken) =>
            inner.UsernameExistsAsync(username, cancellationToken);
        public Task AddAsync(User user, CancellationToken cancellationToken) => inner.AddAsync(user, cancellationToken);
        public Task SaveAsync(User user, CancellationToken cancellationToken) => inner.SaveAsync(user, cancellationToken);
        public Task SavePasswordAsync(User user, CancellationToken cancellationToken) =>
            inner.SavePasswordAsync(user, cancellationToken);
        public Task DeleteAsync(Guid id, Guid version, CancellationToken cancellationToken) =>
            inner.DeleteAsync(id, version, cancellationToken);
    }

    private sealed class BlockingTokenCacheService : SuccessfulTokenCacheService
    {
        private readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Entered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async Task<bool> SetInvalidBeforeAsync(Guid userId, CancellationToken cancellationToken)
        {
            Entered.TrySetResult();
            await release.Task.WaitAsync(cancellationToken);
            return true;
        }

        public void Release() => release.TrySetResult();
    }

    private class SuccessfulTokenCacheService : IUserTokenCacheService
    {
        public virtual Task<bool> SetInvalidBeforeAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(true);
        public Task<bool> SaveAsync(Guid userId, Guid sessionId, TokenPair tokens, CancellationToken cancellationToken) =>
            Task.FromResult(true);
        public Task<bool> RefreshAsync(Guid userId, Guid sessionId, long issuedAtUnixTimeSeconds, string refreshToken, TokenPair tokens, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<bool> DeleteAsync(Guid userId, Guid sessionId, string refreshToken, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<bool> IsTokenIssuedAfterInvalidBeforeAsync(Guid userId, long issuedAtUnixTimeSeconds, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FixedTokenService : ITokenService
    {
        public TokenPair Generate(User user, Guid sessionId) => new("access", "refresh", DateTime.UtcNow.AddDays(1));
        public bool TryValidate(string token, out TokenPayload? payload)
        {
            payload = null;
            return false;
        }
    }

    private sealed class NoOpUserInfoCacheInvalidationPublisher : IUserInfoCacheInvalidationPublisher
    {
        public Task PublishAsync(Guid userId, CancellationToken cancellationToken) => Task.CompletedTask;
    }

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
        private readonly string? databasePath = database.CurrentConnectionConfig.DbType == DbType.Sqlite
            ? database.CurrentConnectionConfig.ConnectionString
                .Split(';', StringSplitOptions.RemoveEmptyEntries)[0]["Data Source=".Length..]
            : null;

        public SqlSugarClient Database { get; } = database;

        public ICapPublisher Publisher { get; } = publisher;

        public UnitOfWork UnitOfWork { get; } = new(database, publisher);

        public void Dispose()
        {
            Database.Ado.Close();
            Database.Dispose();
            serviceProvider.Dispose();
            if (databasePath is not null)
            {
                File.Delete(databasePath);
            }
        }
    }
}
