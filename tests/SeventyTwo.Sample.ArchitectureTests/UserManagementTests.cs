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
        var cacheInvalidationPublisher = new FakeUserInfoCacheInvalidationPublisher();
        var application = new UserApplication(
            userRepository,
            organizationRepository,
            null!,
            new FakeUnitOfWork(),
            null!,
            null!,
            cacheInvalidationPublisher
        );

        await application.UpdateAsync(
            user.Id,
            new("新姓名", "13900000000", "new@example.com", organization.Id, user.Version),
            CancellationToken.None
        );

        Assert.Equal(1, organizationRepository.MutationLockAcquireCount);
        Assert.Same(user, userRepository.SavedUser);
        Assert.Equal([user.Id], cacheInvalidationPublisher.UserIds);
    }

    [Theory]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    public async Task SetEnable_ShouldInvalidateUserInfoCacheAndOnlyInvalidateTokensWhenDisabled(
        bool enable,
        int expectedTokenInvalidations
    )
    {
        var user = CreateUser(enable: !enable);
        var userRepository = new CapturingUserRepository(user);
        var tokenCacheService = new CapturingUserTokenCacheService();
        var cacheInvalidationPublisher = new FakeUserInfoCacheInvalidationPublisher();
        var application = new UserApplication(
            userRepository,
            null!,
            null!,
            new FakeUnitOfWork(),
            null!,
            tokenCacheService,
            cacheInvalidationPublisher
        );

        await application.SetEnableAsync(
            user.Id,
            new(enable, user.Version),
            CancellationToken.None
        );

        Assert.Equal([user.Id], userRepository.LockedUserIds);
        Assert.Equal(expectedTokenInvalidations, tokenCacheService.InvalidatedUserIds.Count);
        Assert.Equal([user.Id], cacheInvalidationPublisher.UserIds);
    }

    [Fact]
    public async Task SetEnable_WhenTokenInvalidationReturnsFalse_ShouldFailWithoutPublishingCacheMessage()
    {
        var user = CreateUser(enable: true);
        var tokenCacheService = new CapturingUserTokenCacheService { SetInvalidBeforeResult = false };
        var cacheInvalidationPublisher = new FakeUserInfoCacheInvalidationPublisher();
        var userRepository = new CapturingUserRepository(user);
        var application = new UserApplication(
            userRepository,
            null!,
            null!,
            new FakeUnitOfWork(),
            null!,
            tokenCacheService,
            cacheInvalidationPublisher
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            application.SetEnableAsync(user.Id, new(false, user.Version), CancellationToken.None)
        );

        Assert.Empty(cacheInvalidationPublisher.UserIds);
    }

    [Fact]
    public async Task SetEnable_WhenTokenInvalidationThrows_ShouldPropagateWithoutPublishingCacheMessage()
    {
        var user = CreateUser(enable: true);
        var tokenCacheService = new CapturingUserTokenCacheService
        {
            SetInvalidBeforeException = new InvalidOperationException("Redis unavailable"),
        };
        var cacheInvalidationPublisher = new FakeUserInfoCacheInvalidationPublisher();
        var application = new UserApplication(
            new CapturingUserRepository(user),
            null!,
            null!,
            new FakeUnitOfWork(),
            null!,
            tokenCacheService,
            cacheInvalidationPublisher
        );

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            application.SetEnableAsync(user.Id, new(false, user.Version), CancellationToken.None)
        );

        Assert.Equal("Redis unavailable", exception.Message);
        Assert.Empty(cacheInvalidationPublisher.UserIds);
    }

    [Fact]
    public async Task Delete_ShouldLockUserInvalidateTokensAndUserInfoCache()
    {
        var user = CreateUser(enable: true);
        var calls = new List<string>();
        var userRepository = new CapturingUserRepository(user, calls);
        var tokenCacheService = new CapturingUserTokenCacheService(calls);
        var cacheInvalidationPublisher = new FakeUserInfoCacheInvalidationPublisher(calls);
        var application = new UserApplication(
            userRepository,
            null!,
            null!,
            new FakeUnitOfWork(),
            null!,
            tokenCacheService,
            cacheInvalidationPublisher
        );

        await application.DeleteAsync(user.Id, user.Version, CancellationToken.None);

        Assert.Equal([user.Id], userRepository.LockedUserIds);
        Assert.Equal(1, userRepository.GetAfterSecurityLockCount);
        Assert.Equal([user.Id], userRepository.DeletedUserIds);
        Assert.Equal([user.Id], tokenCacheService.InvalidatedUserIds);
        Assert.Equal([user.Id], cacheInvalidationPublisher.UserIds);
        Assert.Equal(["lock", "get", "delete", "invalidate-tokens", "publish-cache-invalidation"], calls);
    }

    [Fact]
    public async Task Delete_WhenTokenInvalidationFails_ShouldNotPublishCacheMessage()
    {
        var user = CreateUser(enable: true);
        var tokenCacheService = new CapturingUserTokenCacheService { SetInvalidBeforeResult = false };
        var cacheInvalidationPublisher = new FakeUserInfoCacheInvalidationPublisher();
        var application = new UserApplication(
            new CapturingUserRepository(user),
            null!,
            null!,
            new FakeUnitOfWork(),
            null!,
            tokenCacheService,
            cacheInvalidationPublisher
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            application.DeleteAsync(user.Id, user.Version, CancellationToken.None)
        );

        Assert.Empty(cacheInvalidationPublisher.UserIds);
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
    public async Task Login_WithDisabledUser_ShouldReportDisabled()
    {
        const string username = "disabled-user";
        const string password = "password";
        var passwordHash = new PasswordHasher<string>().HashPassword(username, password);
        var user = new User(
            Guid.CreateVersion7(),
            username,
            passwordHash,
            "禁用用户",
            "13800000000",
            "disabled@example.com"
        )
        {
            Enable = false,
        };
        var userRepository = new CapturingUserRepository(user);
        var application = new UserApplication(
            userRepository,
            null!,
            null!,
            new FakeUnitOfWork(),
            null!,
            null!,
            null!
        );

        var exception = await Assert.ThrowsAsync<UserDomainException>(() =>
            application.LoginAsync(new(username, password), CancellationToken.None)
        );

        Assert.Equal(MessageKeys.Users.Disabled, exception.Message);
        Assert.Equal([user.Id], userRepository.LockedUserIds);
        Assert.Equal(1, userRepository.GetAfterSecurityLockCount);
    }

    [Fact]
    public async Task Login_WithDisabledUserAndInvalidPassword_ShouldRejectCredentials()
    {
        const string username = "disabled-user";
        var passwordHash = new PasswordHasher<string>().HashPassword(username, "password");
        var user = new User(
            Guid.CreateVersion7(),
            username,
            passwordHash,
            "禁用用户",
            "13800000000",
            "disabled@example.com"
        )
        {
            Enable = false,
        };
        var userRepository = new CapturingUserRepository(user);
        var unitOfWork = new CapturingUnitOfWork();
        var application = new UserApplication(
            userRepository,
            null!,
            null!,
            unitOfWork,
            null!,
            null!,
            null!
        );

        var exception = await Assert.ThrowsAsync<UserDomainException>(() =>
            application.LoginAsync(new(username, "invalid-password"), CancellationToken.None)
        );

        Assert.Equal(MessageKeys.Users.CredentialsInvalid, exception.Message);
        Assert.Equal(0, unitOfWork.ExecuteCount);
        Assert.Empty(userRepository.LockedUserIds);
    }

    [Fact]
    public async Task Login_WhenPasswordHashChangesWhileWaitingForSecurityLock_ShouldRejectCredentials()
    {
        const string username = "password-changed-user";
        const string password = "password";
        var userId = Guid.CreateVersion7();
        var candidate = new User(
            userId,
            username,
            new PasswordHasher<string>().HashPassword(username, password),
            "密码变更用户",
            "13800000000",
            "password-changed@example.com"
        )
        {
            Enable = true,
        };
        var lockedUser = new User(
            userId,
            username,
            new PasswordHasher<string>().HashPassword(username, "new-password"),
            "密码变更用户",
            "13800000000",
            "password-changed@example.com"
        )
        {
            Enable = true,
        };
        var repository = new WaitingSecurityLockUserRepository(candidate, lockedUser);
        var application = new UserApplication(
            repository,
            null!,
            null!,
            new FakeUnitOfWork(),
            null!,
            null!,
            null!
        );

        var loginTask = application.LoginAsync(new(username, password), CancellationToken.None);
        await repository.LockWaitStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        repository.ReleaseLockWait();

        var exception = await Assert.ThrowsAsync<UserDomainException>(() => loginTask);
        Assert.Equal(MessageKeys.Users.CredentialsInvalid, exception.Message);
        Assert.Equal(1, repository.LockedGetCount);
    }

    [Fact]
    public async Task Login_WhenUserIsDisabledWhileWaitingForSecurityLock_ShouldRejectDisabledUser()
    {
        const string username = "concurrent-user";
        const string password = "password";
        var passwordHash = new PasswordHasher<string>().HashPassword(username, password);
        var userId = Guid.CreateVersion7();
        var enabledUser = new User(
            userId,
            username,
            passwordHash,
            "并发用户",
            "13800000000",
            "concurrent@example.com"
        )
        {
            Enable = true,
        };
        var disabledUser = new User(
            userId,
            username,
            passwordHash,
            "并发用户",
            "13800000000",
            "concurrent@example.com"
        )
        {
            Enable = false,
        };
        var repository = new WaitingSecurityLockUserRepository(enabledUser, disabledUser);
        var application = new UserApplication(
            repository,
            null!,
            null!,
            new FakeUnitOfWork(),
            null!,
            null!,
            null!
        );

        var loginTask = application.LoginAsync(new(username, password), CancellationToken.None);
        await repository.LockWaitStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        repository.ReleaseLockWait();

        var exception = await Assert.ThrowsAsync<UserDomainException>(() => loginTask);
        Assert.Equal(MessageKeys.Users.Disabled, exception.Message);
        Assert.Equal(1, repository.LockedGetCount);
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
            tokenCacheService,
            null!
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
    [InlineData(nameof(UsersController.GetAuthorizationAsync), "authorization", "usersAuthorize")]
    [InlineData(nameof(UsersController.AuthorizeAsync), "authorize", "usersAuthorize")]
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

    private static User CreateUser(bool enable)
    {
        return new User(
            Guid.CreateVersion7(),
            $"user-{Guid.NewGuid():N}",
            "hash",
            "测试用户",
            "13800000000",
            "user@example.com"
        )
        {
            Enable = enable,
            Version = Guid.CreateVersion7(),
        };
    }

    private sealed class CapturingUserRepository(User? existingUser = null, List<string>? calls = null) : IUserRepository
    {
        public User? AddedUser { get; private set; }
        public User? SavedUser { get; private set; }
        public IReadOnlyList<Guid> DeletedUserIds { get; private set; } = [];
        public IReadOnlyList<Guid> LockedUserIds { get; private set; } = [];
        public int GetAfterSecurityLockCount { get; private set; }

        public Task AcquireSecurityLockAsync(Guid id, CancellationToken cancellationToken)
        {
            calls?.Add("lock");
            LockedUserIds = [.. LockedUserIds, id];
            return Task.CompletedTask;
        }

        public Task<User?> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            calls?.Add("get");
            if (LockedUserIds.Contains(id))
            {
                GetAfterSecurityLockCount++;
            }
            return Task.FromResult(existingUser?.Id == id ? existingUser : null);
        }

        public Task<User?> GetByAccountAsync(string account, CancellationToken cancellationToken) =>
            Task.FromResult(existingUser?.Username == account ? existingUser : null);

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

        public Task DeleteAsync(Guid id, Guid version, CancellationToken cancellationToken)
        {
            calls?.Add("delete");
            DeletedUserIds = [.. DeletedUserIds, id];
            return Task.CompletedTask;
        }
    }

    private sealed class WaitingSecurityLockUserRepository(User candidate, User lockedUser) : IUserRepository
    {
        private readonly TaskCompletionSource releaseLockWait = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource LockWaitStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int LockedGetCount { get; private set; }

        public async Task AcquireSecurityLockAsync(Guid id, CancellationToken cancellationToken)
        {
            Assert.Equal(candidate.Id, id);
            LockWaitStarted.TrySetResult();
            await releaseLockWait.Task.WaitAsync(cancellationToken);
        }

        public void ReleaseLockWait() => releaseLockWait.TrySetResult();

        public Task<User?> GetByAccountAsync(string account, CancellationToken cancellationToken) =>
            Task.FromResult<User?>(account == candidate.Username ? candidate : null);

        public Task<User?> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            LockedGetCount++;
            return Task.FromResult<User?>(id == lockedUser.Id ? lockedUser : null);
        }

        public Task<IReadOnlyList<User>> GetListAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task AddAsync(User user, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task SaveAsync(User user, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteAsync(Guid id, Guid version, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
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

    private sealed class CapturingUserTokenCacheService(List<string>? calls = null) : IUserTokenCacheService
    {
        public IReadOnlyList<Guid> InvalidatedUserIds { get; private set; } = [];
        public bool SetInvalidBeforeResult { get; init; } = true;
        public Exception? SetInvalidBeforeException { get; init; }

        public Task<bool> SetInvalidBeforeAsync(Guid userId, CancellationToken cancellationToken)
        {
            calls?.Add("invalidate-tokens");
            if (SetInvalidBeforeException is not null)
            {
                throw SetInvalidBeforeException;
            }
            InvalidatedUserIds = [.. InvalidatedUserIds, userId];
            return Task.FromResult(SetInvalidBeforeResult);
        }

        public Task<bool> SaveAsync(Guid userId, Guid sessionId, TokenPair tokens, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<bool> RefreshAsync(Guid userId, Guid sessionId, long issuedAtUnixTimeSeconds, string refreshToken, TokenPair tokens, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<bool> DeleteAsync(Guid userId, Guid sessionId, string refreshToken, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<bool> IsTokenIssuedAfterInvalidBeforeAsync(Guid userId, long issuedAtUnixTimeSeconds, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeUserInfoCacheInvalidationPublisher(List<string>? calls = null)
        : IUserInfoCacheInvalidationPublisher
    {
        public IReadOnlyList<Guid> UserIds { get; private set; } = [];

        public Task PublishAsync(Guid userId, CancellationToken cancellationToken)
        {
            calls?.Add("publish-cache-invalidation");
            UserIds = [.. UserIds, userId];
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingUserRepository : IUserRepository
    {
        public Task AcquireSecurityLockAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

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
