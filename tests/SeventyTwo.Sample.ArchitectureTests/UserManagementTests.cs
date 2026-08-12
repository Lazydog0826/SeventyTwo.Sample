using System.Reflection;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Routing;
using SeventyTwo.Sample.Application;
using SeventyTwo.Sample.Application.Users;
using SeventyTwo.Sample.Common.MessageKeys;
using SeventyTwo.Sample.Domain.Users;
using SeventyTwo.Sample.Infrastructure.Permissions;
using SeventyTwo.Sample.Infrastructure.Users;
using SeventyTwo.Sample.WebApi.Authentication;
using SeventyTwo.Sample.WebApi.Controllers;
using SqlSugar;

namespace SeventyTwo.Sample.ArchitectureTests;

public sealed class UserManagementTests
{
    static UserManagementTests() => new UserMappingProfile().Register(TypeAdapterConfig.GlobalSettings);

    [Fact]
    public async Task Create_ShouldPreservePasswordWhitespace()
    {
        const string password = " abcdef ";
        var repository = new CapturingUserRepository();
        var application = new UserApplication(
            repository,
            null!,
            new FakeUnitOfWork(),
            null!,
            null!
        );

        await application.CreateAsync(
            new("user", password, "测试用户", "13800000000", "user@example.com", true),
            CancellationToken.None
        );

        var user = Assert.IsType<User>(repository.AddedUser);
        var hasher = new PasswordHasher<string>();
        Assert.NotEqual(
            PasswordVerificationResult.Failed,
            hasher.VerifyHashedPassword(user.Username, user.PasswordHash, password)
        );
        Assert.Equal(
            PasswordVerificationResult.Failed,
            hasher.VerifyHashedPassword(user.Username, user.PasswordHash, password.Trim())
        );
    }

    [Theory]
    [InlineData(nameof(UsersController.GetListAsync), "list", "usersList")]
    [InlineData(nameof(UsersController.CreateAsync), "create", "usersCreate")]
    [InlineData(nameof(UsersController.UpdateAsync), "update", "usersUpdate")]
    [InlineData(nameof(UsersController.SetEnableAsync), "set-enable", "usersUpdate")]
    [InlineData(nameof(UsersController.DeleteAsync), "delete", "usersDelete")]
    public async Task Endpoints_ShouldUseDedicatedPermission(string methodName, string route, string code)
    {
        var method = typeof(UsersController).GetMethod(methodName)!;
        Assert.Equal(route, method.GetCustomAttributes().OfType<HttpMethodAttribute>().Single().Template);
        var permission = method.GetCustomAttribute<PermissionAttribute>()!;
        var policy = await new PermissionPolicyProvider(
            Microsoft.Extensions.Options.Options.Create(new Microsoft.AspNetCore.Authorization.AuthorizationOptions())
        ).GetPolicyAsync(permission.Policy!);
        Assert.Equal([code], Assert.Single(policy!.Requirements.OfType<PermissionRequirement>()).PermissionCodes);
    }

    [Fact]
    public async Task RepositorySave_ShouldAdvanceVersionWithoutChangingCredentials()
    {
        var path = Path.Combine(Path.GetTempPath(), $"user-update-{Guid.NewGuid():N}.db");
        try
        {
            using var db = CreateDatabase(path);
            db.CodeFirst.InitTables<UserAccountRecord, UserPermissionRecord>();
            var record = CreateRecord();
            await db.Insertable(record).ExecuteCommandAsync();
            var repository = new UserRepository(db);
            var user = Assert.IsType<User>(await repository.GetAsync(record.Id, CancellationToken.None));
            var version = user.Version;
            user.UpdateProfile("新姓名", "13900000000", "new@example.com", version, Guid.Empty, DateTimeOffset.UtcNow);

            await repository.SaveAsync(user, CancellationToken.None);

            var saved = await db.Queryable<UserAccountRecord>().SingleAsync(x => x.Id == record.Id);
            Assert.Equal(record.Username, saved.Username);
            Assert.Equal(record.PasswordHash, saved.PasswordHash);
            Assert.Equal("新姓名", saved.DisplayName);
            Assert.NotEqual(version, saved.Version);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task RepositoryGetList_ShouldExcludeSuperAdmin()
    {
        var path = Path.Combine(Path.GetTempPath(), $"user-list-{Guid.NewGuid():N}.db");
        try
        {
            using var db = CreateDatabase(path);
            db.CodeFirst.InitTables<UserAccountRecord>();
            var superAdmin = CreateRecord(SystemUsernames.SuperAdmin);
            var regularUser = CreateRecord();
            await db.Insertable(new[] { superAdmin, regularUser }).ExecuteCommandAsync();

            var users = await new UserRepository(db).GetListAsync(CancellationToken.None);

            var user = Assert.Single(users);
            Assert.Equal(regularUser.Id, user.Id);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task RepositoryDelete_WithPermissionAssociation_ShouldFail()
    {
        var path = Path.Combine(Path.GetTempPath(), $"user-delete-related-{Guid.NewGuid():N}.db");
        try
        {
            using var db = CreateDatabase(path);
            db.CodeFirst.InitTables<UserAccountRecord, UserPermissionRecord>();
            var record = CreateRecord();
            await db.Insertable(record).ExecuteCommandAsync();
            await db.Insertable(new UserPermissionRecord { UserId = record.Id, PermissionId = Guid.CreateVersion7() }).ExecuteCommandAsync();

            var exception = await Assert.ThrowsAsync<UserDomainException>(() =>
                new UserRepository(db).DeleteAsync(record.Id, record.Version, CancellationToken.None)
            );
            Assert.Equal(MessageKeys.Users.HasPermissions, exception.Message);
            Assert.Equal(1, await db.Queryable<UserAccountRecord>().CountAsync());
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task RepositoryDelete_WithoutAssociation_ShouldPhysicallyDelete()
    {
        var path = Path.Combine(Path.GetTempPath(), $"user-delete-{Guid.NewGuid():N}.db");
        try
        {
            using var db = CreateDatabase(path);
            db.CodeFirst.InitTables<UserAccountRecord, UserPermissionRecord>();
            var record = CreateRecord();
            await db.Insertable(record).ExecuteCommandAsync();

            await new UserRepository(db).DeleteAsync(record.Id, record.Version, CancellationToken.None);

            Assert.Equal(0, await db.Queryable<UserAccountRecord>().CountAsync());
        }
        finally { File.Delete(path); }
    }

    private static SqlSugarClient CreateDatabase(string path) =>
        new(new ConnectionConfig { DbType = DbType.Sqlite, ConnectionString = $"Data Source={path};Pooling=False", IsAutoCloseConnection = true });

    private static UserAccountRecord CreateRecord(string? username = null) => new()
    {
        Id = Guid.CreateVersion7(), Username = username ?? $"user-{Guid.NewGuid():N}", PasswordHash = "hash",
        DisplayName = "测试用户", Phone = "13800000000", Email = "user@example.com", Version = Guid.CreateVersion7(),
    };

    private sealed class CapturingUserRepository : IUserRepository
    {
        public User? AddedUser { get; private set; }

        public Task<User?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<User?>(null);

        public Task<User?> GetByAccountAsync(string account, CancellationToken cancellationToken) =>
            Task.FromResult<User?>(null);

        public Task<IReadOnlyList<User>> GetListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<User>>([]);

        public Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task AddAsync(User user, CancellationToken cancellationToken)
        {
            AddedUser = user;
            return Task.CompletedTask;
        }

        public Task SaveAsync(User user, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task DeleteAsync(Guid id, Guid version, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken) => action();
    }
}
