using System.Reflection;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Routing;
using SeventyTwo.Sample.Application;
using SeventyTwo.Sample.Application.Authentication;
using SeventyTwo.Sample.Application.Users;
using SeventyTwo.Sample.Common.MessageKeys;
using SeventyTwo.Sample.Domain.Users;
using SeventyTwo.Sample.Domain.Organizations;
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
        var organization = CreateOrganization();
        var organizationRepository = new FakeOrganizationRepository(organization);
        var application = new UserApplication(
            repository,
            organizationRepository,
            null!,
            new FakeUnitOfWork(),
            null!,
            null!
        );

        await application.CreateAsync(
            new("user", password, "测试用户", "13800000000", "user@example.com", true, organization.Id),
            CancellationToken.None
        );

        var user = Assert.IsType<User>(repository.AddedUser);
        Assert.Equal(1, organizationRepository.MutationLockAcquireCount);
        Assert.Equal(organization.Id, user.OrgId);
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

    [Fact]
    public async Task Update_ShouldAcquireOrganizationMutationLockBeforeValidation()
    {
        var organization = CreateOrganization();
        var user = new User(
            Guid.CreateVersion7(),
            "user",
            "hash",
            "测试用户",
            "13800000000",
            "user@example.com"
        )
        {
            Version = Guid.CreateVersion7(),
            OrgId = organization.Id,
        };
        var userRepository = new CapturingUserRepository(user);
        var organizationRepository = new FakeOrganizationRepository(organization);
        var application = new UserApplication(
            userRepository,
            organizationRepository,
            null!,
            new FakeUnitOfWork(),
            null!,
            null!
        );

        await application.UpdateAsync(
            user.Id,
            new("新姓名", "13900000000", "new@example.com", organization.Id, user.Version),
            CancellationToken.None
        );

        Assert.Equal(1, organizationRepository.MutationLockAcquireCount);
        Assert.Same(user, userRepository.SavedUser);
    }

    [Fact]
    public async Task Create_WithDisabledOrganization_ShouldFail()
    {
        var organization = CreateOrganization(false);
        var application = new UserApplication(
            new CapturingUserRepository(),
            new FakeOrganizationRepository(organization),
            null!,
            new FakeUnitOfWork(),
            null!,
            null!
        );

        var exception = await Assert.ThrowsAsync<UserDomainException>(() =>
            application.CreateAsync(
                new("user", "password", "测试用户", "13800000000", "user@example.com", true, organization.Id),
                CancellationToken.None
            )
        );

        Assert.Equal(MessageKeys.Users.OrganizationDisabled, exception.Message);
    }

    [Fact]
    public async Task Create_WithInvalidPassword_ShouldFailBeforeOpeningUnitOfWork()
    {
        var organization = CreateOrganization();
        var unitOfWork = new CapturingUnitOfWork();
        var application = new UserApplication(
            new CapturingUserRepository(),
            new FakeOrganizationRepository(organization),
            null!,
            unitOfWork,
            null!,
            null!
        );

        var exception = await Assert.ThrowsAsync<UserDomainException>(() =>
            application.CreateAsync(
                new("user", " ", "测试用户", "13800000000", "user@example.com", true, organization.Id),
                CancellationToken.None
            )
        );

        Assert.Equal(MessageKeys.Validation.PasswordRequired, exception.Message);
        Assert.Equal(0, unitOfWork.ExecuteCount);
    }

    [Fact]
    public async Task Create_WhenCanceled_ShouldFailBeforeOpeningUnitOfWork()
    {
        var organization = CreateOrganization();
        var unitOfWork = new CapturingUnitOfWork();
        var application = new UserApplication(
            new CapturingUserRepository(),
            new FakeOrganizationRepository(organization),
            null!,
            unitOfWork,
            null!,
            null!
        );
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            application.CreateAsync(
                new("user", "password", "测试用户", "13800000000", "user@example.com", true, organization.Id),
                cancellationTokenSource.Token
            )
        );

        Assert.Equal(0, unitOfWork.ExecuteCount);
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldRejectTokenIssuedBeforeInvalidBefore()
    {
        const long issuedAt = 1_800_000_000;
        var userId = Guid.CreateVersion7();
        var tokenService = new FixedTokenService(
            new(userId, "user", "用户", "refresh", Guid.CreateVersion7(), issuedAt)
        );
        var tokenCacheService = new RejectingUserTokenCacheService();
        var application = new UserApplication(
            new ThrowingUserRepository(),
            null!,
            null!,
            new FakeUnitOfWork(),
            tokenService,
            tokenCacheService
        );

        var exception = await Assert.ThrowsAsync<TokenAuthenticationException>(() =>
            application.RefreshTokenAsync("refresh-token", CancellationToken.None)
        );

        Assert.Equal(MessageKeys.Authentication.RefreshTokenInvalid, exception.Message);
        Assert.Equal((userId, issuedAt), tokenCacheService.VerifiedToken);
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
            var organizationId = Guid.CreateVersion7();
            user.OrgId = organizationId;
            user.UpdateProfile("新姓名", "13900000000", "new@example.com", version, Guid.Empty, DateTimeOffset.UtcNow);

            await repository.SaveAsync(user, CancellationToken.None);

            var saved = await db.Queryable<UserAccountRecord>().SingleAsync(x => x.Id == record.Id);
            Assert.Equal(record.Username, saved.Username);
            Assert.Equal(record.PasswordHash, saved.PasswordHash);
            Assert.Equal("新姓名", saved.DisplayName);
            Assert.Equal(organizationId, saved.OrgId);
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

    private static Organization CreateOrganization(bool enable = true)
    {
        var organization = new Organization(Guid.CreateVersion7(), "ORG", "测试机构") { Enable = enable };
        organization.OrgId = organization.Id;
        return organization;
    }

    private sealed class CapturingUserRepository(User? existingUser = null) : IUserRepository
    {
        public User? AddedUser { get; private set; }
        public User? SavedUser { get; private set; }

        public Task<User?> GetAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(existingUser?.Id == id ? existingUser : null);

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

        public Task SaveAsync(User user, CancellationToken cancellationToken)
        {
            SavedUser = user;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, Guid version, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken) => action();
    }

    private sealed class CapturingUnitOfWork : IUnitOfWork
    {
        public int ExecuteCount { get; private set; }

        public async Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken)
        {
            ExecuteCount++;
            await action();
        }
    }

    private sealed class FakeOrganizationRepository(Organization organization) : IOrganizationRepository
    {
        public int MutationLockAcquireCount { get; private set; }

        public Task AcquireMutationLockAsync(CancellationToken cancellationToken)
        {
            MutationLockAcquireCount++;
            return Task.CompletedTask;
        }

        public Task<Organization?> FindAsync(Guid id, CancellationToken cancellationToken) =>
            MutationLockAcquireCount == 0
                ? throw new InvalidOperationException("验证机构前必须先获取机构变更锁")
                : Task.FromResult(id == organization.Id ? organization : null);
        public Task<IReadOnlyList<Organization>> GetListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Organization>>([organization]);
        public Task<bool> CodeExistsAsync(Guid orgId, string code, Guid? excludedId, CancellationToken cancellationToken) =>
            Task.FromResult(false);
        public Task AddAsync(Organization value, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SaveAsync(Organization value, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FixedTokenService(TokenPayload payload) : ITokenService
    {
        public TokenPair Generate(User user, Guid sessionId) => throw new NotSupportedException();

        public bool TryValidate(string token, out TokenPayload? result)
        {
            result = payload;
            return true;
        }
    }

    private sealed class RejectingUserTokenCacheService : IUserTokenCacheService
    {
        public (Guid UserId, long IssuedAt)? VerifiedToken { get; private set; }

        public Task<bool> SaveAsync(
            Guid userId,
            Guid sessionId,
            TokenPair tokens,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task<bool> RefreshAsync(
            Guid userId,
            Guid sessionId,
            long issuedAtUnixTimeSeconds,
            string refreshToken,
            TokenPair tokens,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task<bool> DeleteAsync(
            Guid userId,
            Guid sessionId,
            string refreshToken,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task<bool> SetInvalidBeforeAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> IsTokenIssuedAfterInvalidBeforeAsync(
            Guid userId,
            long issuedAtUnixTimeSeconds,
            CancellationToken cancellationToken
        )
        {
            VerifiedToken = (userId, issuedAtUnixTimeSeconds);
            return Task.FromResult(false);
        }
    }

    private sealed class ThrowingUserRepository : IUserRepository
    {
        public Task<User?> GetAsync(Guid id, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("失效的刷新令牌不应查询用户");

        public Task<User?> GetByAccountAsync(string account, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<User>> GetListAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task AddAsync(User user, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task SaveAsync(User user, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task DeleteAsync(Guid id, Guid version, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
