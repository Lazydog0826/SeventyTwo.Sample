using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using SeventyTwo.InfraKit.Cache;
using SeventyTwo.Sample.Application;
using SeventyTwo.Sample.Application.Permissions;
using SeventyTwo.Sample.Application.Users;
using SeventyTwo.Sample.Common.MessageKeys;
using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.Domain.Permissions;
using SeventyTwo.Sample.Domain.Users;
using SeventyTwo.Sample.Infrastructure.Messaging;
using SeventyTwo.Sample.Infrastructure.Permissions;
using SeventyTwo.Sample.WebApi.Authentication;
using SeventyTwo.Sample.WebApi.Controllers;
using SqlSugar;

namespace SeventyTwo.Sample.ArchitectureTests;

public sealed class PermissionCrudTests
{
    [Theory]
    [InlineData(PermissionType.Page, "/Default")]
    [InlineData(PermissionType.Directory, "")]
    public async Task UserGet_ShouldReturnDefaultPagePathOnlyForPagePermission(
        PermissionType permissionType,
        string expectedPath
    )
    {
        var permission = CreatePermission("Default", permissionType);
        var user = new User(
            Guid.CreateVersion7(),
            "user",
            "hash",
            "测试用户",
            "13800000000",
            "user@example.com",
            DataPermissionType.Self,
            permission.Id
        );
        var (cacheService, _, _, redisCacheService) = CreateCacheService();
        var userRepository = new FakeUserRepository(user);
        var userInfoCacheService = new UserInfoCacheService(
            userRepository,
            redisCacheService,
            Options.Create(new CacheConfiguration { KeyNamespace = "tests-user-info" })
        );
        var application = CreateUserApplication(
            userRepository,
            userInfoCacheService,
            new FakePermissionRepository([permission])
        );

        var output = await application.GetAsync(user.Id, CancellationToken.None);

        Assert.Equal(expectedPath, output.DefaultPagePath);
    }

    [Fact]
    public async Task UserGet_ShouldReturnEmptyDefaultPagePathWhenPermissionDoesNotExist()
    {
        var user = new User(
            Guid.CreateVersion7(),
            "user",
            "hash",
            "测试用户",
            "13800000000",
            "user@example.com",
            DataPermissionType.Self,
            Guid.CreateVersion7()
        );
        var (_, _, _, redisCacheService) = CreateCacheService();
        var userRepository = new FakeUserRepository(user);
        var application = CreateUserApplication(
            userRepository,
            new UserInfoCacheService(
                userRepository,
                redisCacheService,
                Options.Create(new CacheConfiguration { KeyNamespace = "tests-user-info-missing" })
            ),
            new FakePermissionRepository([])
        );

        var output = await application.GetAsync(user.Id, CancellationToken.None);

        Assert.Equal("", output.DefaultPagePath);
    }

    [Fact]
    public async Task UserGet_ShouldReturnEmptyDefaultPagePathWhenPermissionIsDisabled()
    {
        var permission = CreatePermission("Default", PermissionType.Page, enable: false);
        var user = new User(
            Guid.CreateVersion7(),
            "user",
            "hash",
            "测试用户",
            "13800000000",
            "user@example.com",
            DataPermissionType.Self,
            permission.Id
        );
        var (_, _, _, redisCacheService) = CreateCacheService();
        var userRepository = new FakeUserRepository(user);
        var application = CreateUserApplication(
            userRepository,
            new UserInfoCacheService(
                userRepository,
                redisCacheService,
                Options.Create(new CacheConfiguration { KeyNamespace = "tests-user-info-disabled" })
            ),
            new FakePermissionRepository([permission])
        );

        var output = await application.GetAsync(user.Id, CancellationToken.None);

        Assert.Equal("", output.DefaultPagePath);
    }

    [Fact]
    public async Task UserInfoCache_ShouldReloadWhenCachedValueIsInvalidJson()
    {
        var userId = Guid.CreateVersion7();
        var defaultPage = CreatePermission("Default", PermissionType.Page);
        var user = new User(
            userId,
            "user",
            "hash",
            "测试用户",
            "13800000000",
            "user@example.com",
            DataPermissionType.Self,
            defaultPage.Id
        );
        var userRepository = new FakeUserRepository(user);
        var database = DispatchProxy.Create<StackExchange.Redis.IDatabase, InMemoryRedisDatabase>();
        var redisDatabase = (InMemoryRedisDatabase)(object)database;
        var redisCacheService = new FakeRedisCacheService(database);
        var cacheConfiguration = Options.Create(new CacheConfiguration { KeyNamespace = "tests" });
        var cacheKey = cacheConfiguration.Value.Data("users", $"info:{userId}");
        redisDatabase.SetString(cacheKey, "invalid json");
        var service = new UserInfoCacheService(userRepository, redisCacheService, cacheConfiguration);

        var output = await service.FindAsync(userId, CancellationToken.None);

        Assert.NotNull(output);
        Assert.Equal(userId, output.Id);
        Assert.Equal(defaultPage.Id, output.DefaultPageId);
        Assert.Equal(1, userRepository.GetCount);
        var cachedOutput = JsonSerializer.Deserialize<UserInfoCacheOutput>(
            redisDatabase.GetString(cacheKey).ToString()
        );
        Assert.NotNull(cachedOutput);
        Assert.Equal(defaultPage.Id, cachedOutput.DefaultPageId);
    }

    [Fact]
    public async Task UserInfoCacheInvalidationConsumer_ShouldDeleteOnlyRequestedUserCache()
    {
        var userId = Guid.CreateVersion7();
        var otherUserId = Guid.CreateVersion7();
        var database = DispatchProxy.Create<StackExchange.Redis.IDatabase, InMemoryRedisDatabase>();
        var inMemoryDatabase = (InMemoryRedisDatabase)(object)database;
        var cacheConfiguration = Options.Create(new CacheConfiguration { KeyNamespace = "tests" });
        var userCacheKey = cacheConfiguration.Value.Data("users", $"info:{userId}");
        var otherUserCacheKey = cacheConfiguration.Value.Data("users", $"info:{otherUserId}");
        inMemoryDatabase.SetString(userCacheKey, "cached-user");
        inMemoryDatabase.SetString(otherUserCacheKey, "cached-other-user");
        var userInfoCacheService = new UserInfoCacheService(
            new FakeUserRepository(
                new User(userId, "user", "hash", "测试用户", "13800000000", "user@example.com", DataPermissionType.Self)
            ),
            new FakeRedisCacheService(database),
            cacheConfiguration
        );
        var consumer = new UserInfoCacheInvalidationConsumer(userInfoCacheService);

        await consumer.ConsumeAsync(new UserInfoCacheInvalidationMessage(userId), CancellationToken.None);

        Assert.False(inMemoryDatabase.StringExists(userCacheKey));
        Assert.True(inMemoryDatabase.StringExists(otherUserCacheKey));
    }

    [Fact]
    public async Task SuperAdmin_ShouldReceiveAllEnabledPermissionsWithoutAssignments()
    {
        var userId = Guid.CreateVersion7();
        var repository = new FakePermissionRepository([
            CreatePermission("Enabled", PermissionType.Page),
            CreatePermission("Disabled", PermissionType.Page, enable: false),
        ]);
        var database = DispatchProxy.Create<StackExchange.Redis.IDatabase, InMemoryRedisDatabase>();
        var redisCacheService = new FakeRedisCacheService(database);
        var cacheConfiguration = Options.Create(new CacheConfiguration { KeyNamespace = "tests" });
        var userRepository = new FakeUserRepository(
            User.Restore(
                userId,
                SystemUsernames.SuperAdmin,
                "hash",
                "超级管理员",
                "13800000000",
                "admin@example.com",
                DataPermissionType.All
            )
        );
        var userPermissionCacheService = new UserPermissionCacheService(
            repository,
            new UserInfoCacheService(userRepository, redisCacheService, cacheConfiguration),
            redisCacheService,
            cacheConfiguration
        );
        var permissionCacheKey = cacheConfiguration.Value.Data("permissions", "user-codes:super-admin");
        var inMemoryDatabase = (InMemoryRedisDatabase)(object)database;
        inMemoryDatabase.SetString(permissionCacheKey, "invalid-json");

        var codes = await userPermissionCacheService.GetCodesAsync(userId, CancellationToken.None);
        var hasEnabled = await userPermissionCacheService.HasAsync(
            userId,
            ["Enabled"],
            PermissionMatchMode.All,
            CancellationToken.None
        );
        var hasDisabled = await userPermissionCacheService.HasAsync(
            userId,
            ["Disabled"],
            PermissionMatchMode.All,
            CancellationToken.None
        );

        Assert.Equal(["Enabled"], codes);
        Assert.True(hasEnabled);
        Assert.False(hasDisabled);
        Assert.Equal(1, userRepository.GetCount);
        Assert.Equal(1, repository.GetAllCount);

        var cachedCodes = JsonSerializer.Deserialize<string[]>(
            inMemoryDatabase.GetString(permissionCacheKey).ToString()
        );
        Assert.NotNull(cachedCodes);
        Assert.Equal(["Enabled"], cachedCodes);

        await userPermissionCacheService.DeleteAsync(userId, CancellationToken.None);
        Assert.True(inMemoryDatabase.StringExists(permissionCacheKey));

        await userPermissionCacheService.DeleteSuperAdminAsync(CancellationToken.None);
        Assert.False(inMemoryDatabase.StringExists(permissionCacheKey));
    }

    /// <summary>
    /// 验证普通用户权限缓存键包含全局版本，且超级管理员失效流程会切换该版本。
    /// </summary>
    [Fact]
    public async Task UserPermissionCache_ShouldUseVersionAndRotateItWithSuperAdminInvalidation()
    {
        var user = User.Restore(
            Guid.CreateVersion7(),
            "user",
            "hash",
            "用户",
            "13800000000",
            "user@example.com",
            DataPermissionType.Self
        );
        var repository = new FakePermissionRepository([]);
        var database = DispatchProxy.Create<StackExchange.Redis.IDatabase, InMemoryRedisDatabase>();
        var inMemoryDatabase = (InMemoryRedisDatabase)(object)database;
        var redisCacheService = new FakeRedisCacheService(database);
        var cacheConfiguration = Options.Create(new CacheConfiguration { KeyNamespace = "tests" });
        var service = new UserPermissionCacheService(
            repository,
            new UserInfoCacheService(new FakeUserRepository(user), redisCacheService, cacheConfiguration),
            redisCacheService,
            cacheConfiguration
        );
        var versionKey = cacheConfiguration.Value.Data("permissions", "user-codes:version");

        await service.GetCodesAsync(user.Id, CancellationToken.None);
        var oldVersion = inMemoryDatabase.GetString(versionKey).ToString();
        var oldCacheKey = cacheConfiguration.Value.Data("permissions", $"user-codes:{oldVersion}:{user.Id}");

        await service.DeleteSuperAdminAsync(CancellationToken.None);
        var newVersion = inMemoryDatabase.GetString(versionKey).ToString();

        Assert.NotEmpty(oldVersion);
        Assert.True(inMemoryDatabase.StringExists(oldCacheKey));
        Assert.NotEqual(oldVersion, newVersion);

        await service.GetCodesAsync(user.Id, CancellationToken.None);
        var newCacheKey = cacheConfiguration.Value.Data("permissions", $"user-codes:{newVersion}:{user.Id}");
        Assert.True(inMemoryDatabase.StringExists(newCacheKey));
        Assert.Equal(2, repository.GetCodesByUserIdCount);
    }

    [Theory]
    [InlineData(nameof(PermissionsController.GetDetailAsync), "detail", "permissionsUpdate")]
    [InlineData(nameof(PermissionsController.CreateAsync), "create", "permissionsCreate")]
    [InlineData(nameof(PermissionsController.UpdateAsync), "update", "permissionsUpdate")]
    [InlineData(nameof(PermissionsController.DeleteAsync), "delete", "permissionsDelete")]
    public async Task ManagementEndpoints_ShouldRequireDedicatedPermission(string methodName, string route, string code)
    {
        var action = typeof(PermissionsController).GetMethod(methodName);

        var httpMethod = Assert.Single(action!.GetCustomAttributes().OfType<HttpMethodAttribute>());
        var permission = Assert.IsType<PermissionAttribute>(action?.GetCustomAttribute<PermissionAttribute>());
        Assert.Equal(route, httpMethod.Template);

        var policyProvider = new PermissionPolicyProvider(Options.Create(new AuthorizationOptions()));
        var policy = await policyProvider.GetPolicyAsync(Assert.IsType<string>(permission.Policy));
        var requirement = Assert.Single(
            Assert.IsType<AuthorizationPolicy>(policy).Requirements.OfType<PermissionRequirement>()
        );
        Assert.Equal(PermissionMatchMode.All, requirement.MatchMode);
        Assert.Equal([code], requirement.PermissionCodes);
    }

    [Fact]
    public void DomainUpdate_ShouldApplyAllFieldsAndRejectStaleVersion()
    {
        var permission = CreatePermission("Before", PermissionType.Page);
        var version = permission.Version;

        permission.Update(
            "After",
            "修改后",
            PermissionType.Directory,
            false,
            9,
            "Folder",
            string.Empty,
            string.Empty,
            string.Empty,
            null,
            new PermissionMetaData(false),
            version,
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow
        );

        Assert.Equal("After", permission.Code);
        Assert.Equal(PermissionType.Directory, permission.Type);
        Assert.False(permission.Enable);
        Assert.Equal(9, permission.SortOrder);
        Assert.Throws<PermissionDomainException>(() =>
            permission.Update(
                "Again",
                "再次修改",
                PermissionType.Directory,
                true,
                0,
                "Folder",
                string.Empty,
                string.Empty,
                string.Empty,
                null,
                new PermissionMetaData(true),
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                DateTimeOffset.UtcNow
            )
        );
    }

    [Fact]
    public async Task Application_ShouldRejectDuplicateCodeAndCyclicParent()
    {
        var parent = CreatePermission("Parent", PermissionType.Directory);
        var child = CreatePermission("Child", PermissionType.Directory, parent.Id);
        var repository = new FakePermissionRepository([parent, child]) { RequireCatalogMutationLock = true };
        var (cacheService, _, _, _) = CreateCacheService();
        var unitOfWork = new FakeUnitOfWork();
        var application = new PermissionApplication(
            repository,
            cacheService,
            new FakeUserPermissionCacheService(),
            new FakePermissionCacheInvalidationPublisher(),
            new FakeUserPermissionCacheInvalidationPublisher(),
            unitOfWork,
            null!
        );

        await Assert.ThrowsAsync<PermissionDomainException>(() =>
            application.CreateAsync(CreateInput("Parent"), CancellationToken.None)
        );
        await Assert.ThrowsAsync<PermissionDomainException>(() =>
            application.UpdateAsync(parent.Id, UpdateInput(parent, child.Id), CancellationToken.None)
        );
        Assert.Equal(2, unitOfWork.ExecuteCount);
        Assert.Equal(2, repository.CatalogMutationLockAcquireCount);
    }

    [Theory]
    [InlineData("/Created", "existing", MessageKeys.Permissions.RouteNameExists)]
    [InlineData("/existing", "Created", MessageKeys.Permissions.RoutePathExists)]
    [InlineData("/Created", "LAYOUT", MessageKeys.Permissions.RouteNameExists)]
    [InlineData("/LOGIN", "Created", MessageKeys.Permissions.RoutePathExists)]
    public async Task CreatePage_WithConflictingRoute_ShouldFail(
        string routePath,
        string routeName,
        string expectedMessage
    )
    {
        var existing = CreatePermission("Existing", PermissionType.Page);
        var repository = new FakePermissionRepository([existing]) { RequireCatalogMutationLock = true };
        var (cacheService, _, _, _) = CreateCacheService();
        var application = new PermissionApplication(
            repository,
            cacheService,
            new FakeUserPermissionCacheService(),
            new FakePermissionCacheInvalidationPublisher(),
            new FakeUserPermissionCacheInvalidationPublisher(),
            new FakeUnitOfWork(),
            null!
        );
        var input = CreateInput("Created") with { RoutePath = routePath, RouteName = routeName };

        var exception = await Assert.ThrowsAsync<PermissionDomainException>(() =>
            application.CreateAsync(input, CancellationToken.None)
        );

        Assert.Equal(expectedMessage, exception.Message);
        Assert.Equal(DomainErrorType.Conflict, exception.ErrorType);
    }

    [Fact]
    public async Task UpdateDirectoryToPage_WithExistingRoute_ShouldFail()
    {
        var existing = CreatePermission("Existing", PermissionType.Page);
        var directory = CreatePermission("Directory", PermissionType.Directory);
        var repository = new FakePermissionRepository([existing, directory]) { RequireCatalogMutationLock = true };
        var (cacheService, _, _, _) = CreateCacheService();
        var application = new PermissionApplication(
            repository,
            cacheService,
            new FakeUserPermissionCacheService(),
            new FakePermissionCacheInvalidationPublisher(),
            new FakeUserPermissionCacheInvalidationPublisher(),
            new FakeUnitOfWork(),
            null!
        );
        var input = UpdateInput(directory, null) with
        {
            Type = PermissionType.Page,
            VueComponentPath = "/src/views/Directory.vue",
            RoutePath = "/Existing",
            RouteName = "Existing",
        };

        var exception = await Assert.ThrowsAsync<PermissionDomainException>(() =>
            application.UpdateAsync(directory.Id, input, CancellationToken.None)
        );

        Assert.Equal(MessageKeys.Permissions.RouteNameExists, exception.Message);
        Assert.Equal(DomainErrorType.Conflict, exception.ErrorType);
        Assert.Equal(PermissionType.Directory, directory.Type);
    }

    [Fact]
    public async Task GetDefaultPageOptions_ShouldBuildHierarchicalTitleFromPath()
    {
        var root = CreatePermission("Root", PermissionType.Directory);
        var child = CreatePermission("Child", PermissionType.Directory, root.Id);
        var pageId = Guid.CreateVersion7();
        var page = new Permission(
            pageId,
            "Page",
            "Page",
            PermissionType.Page,
            0,
            string.Empty,
            "/src/views/Page.vue",
            "/Page",
            "Page",
            child.Id,
            new PermissionMetaData(true),
            $"{root.Id}/{child.Id}/{pageId}"
        );
        var repository = new FakePermissionRepository([root, child, page]);
        var (cacheService, _, _, _) = CreateCacheService();
        var application = new PermissionApplication(
            repository,
            cacheService,
            new FakeUserPermissionCacheService(),
            new FakePermissionCacheInvalidationPublisher(),
            new FakeUserPermissionCacheInvalidationPublisher(),
            new FakeUnitOfWork(),
            null!
        );

        var options = await application.GetDefaultPageOptionsAsync(CancellationToken.None);

        var option = Assert.Single(options);
        Assert.Equal(page.Id, option.Id);
        Assert.Equal("Root / Child / Page", option.Title);
    }

    [Fact]
    public async Task Create_ShouldReportParentNotFound_WhenParentDoesNotExist()
    {
        var repository = new FakePermissionRepository([]);
        var (cacheService, _, _, _) = CreateCacheService();
        var application = new PermissionApplication(
            repository,
            cacheService,
            new FakeUserPermissionCacheService(),
            new FakePermissionCacheInvalidationPublisher(),
            new FakeUserPermissionCacheInvalidationPublisher(),
            new FakeUnitOfWork(),
            null!
        );
        var input = CreateInput("Child") with { ParentId = Guid.CreateVersion7() };

        var exception = await Assert.ThrowsAsync<PermissionDomainException>(() =>
            application.CreateAsync(input, CancellationToken.None)
        );

        Assert.Equal(MessageKeys.Permissions.ParentNotFound, exception.Message);
        Assert.Equal(DomainErrorType.NotFound, exception.ErrorType);
    }

    [Fact]
    public async Task Create_ShouldReportParentIdRequired_WhenParentIdIsEmpty()
    {
        var repository = new FakePermissionRepository([]);
        var (cacheService, _, _, _) = CreateCacheService();
        var application = new PermissionApplication(
            repository,
            cacheService,
            new FakeUserPermissionCacheService(),
            new FakePermissionCacheInvalidationPublisher(),
            new FakeUserPermissionCacheInvalidationPublisher(),
            new FakeUnitOfWork(),
            null!
        );
        var input = CreateInput("Child") with { ParentId = Guid.Empty };

        var exception = await Assert.ThrowsAsync<PermissionDomainException>(() =>
            application.CreateAsync(input, CancellationToken.None)
        );

        Assert.Equal(MessageKeys.Permissions.ParentIdRequired, exception.Message);
        Assert.Equal(DomainErrorType.Validation, exception.ErrorType);
    }

    [Fact]
    public async Task ApplicationMutation_ShouldPublishCacheInvalidationMessage()
    {
        var permission = CreatePermission("Editable", PermissionType.Page);
        var repository = new FakePermissionRepository([permission]) { RequireCatalogMutationLock = true };
        var (cacheService, database, configuration, _) = CreateCacheService();
        var versionKey = PermissionCacheKeys.GetAllPermissionsVersionKey(configuration);
        database.SetString(versionKey, "old-version");
        var publisher = new FakePermissionCacheInvalidationPublisher();
        var userPermissionCacheInvalidationPublisher = new FakeUserPermissionCacheInvalidationPublisher();
        var userPermissionCacheService = new FakeUserPermissionCacheService();
        var unitOfWork = new FakeUnitOfWork();
        var application = new PermissionApplication(
            repository,
            cacheService,
            userPermissionCacheService,
            publisher,
            userPermissionCacheInvalidationPublisher,
            unitOfWork,
            null!
        );

        await application.UpdateAsync(permission.Id, UpdateInput(permission, null), CancellationToken.None);
        await application.CreateAsync(CreateInput("Created"), CancellationToken.None);
        await application.DeleteAsync(permission.Id, CancellationToken.None);

        Assert.Equal(3, publisher.PublishCount);
        Assert.Equal(3, unitOfWork.ExecuteCount);
        Assert.Equal(3, repository.CatalogMutationLockAcquireCount);
        Assert.Equal(
            [
                new UserPermissionCacheInvalidationMessage(Guid.Empty, true),
                new UserPermissionCacheInvalidationMessage(Guid.Empty, true),
                new UserPermissionCacheInvalidationMessage(Guid.Empty, true),
            ],
            userPermissionCacheInvalidationPublisher.Messages
        );
        Assert.Equal(0, userPermissionCacheService.DeleteSuperAdminCount);
        Assert.True(database.StringExists(versionKey));
    }

    /// <summary>
    /// 验证授权编辑回显包含禁用权限，并在保存后发布指定用户的缓存失效消息。
    /// </summary>
    [Fact]
    public async Task Authorization_ShouldReturnValidSelectionsAndPublishUserInvalidation()
    {
        var root = CreatePermission("Root", PermissionType.Directory);
        var page = CreatePermission("Page", PermissionType.Page, root.Id);
        var disabled = CreatePermission("Disabled", PermissionType.Button, page.Id, false);
        var user = User.Restore(
            Guid.CreateVersion7(),
            "user",
            "hash",
            "用户",
            "13800000000",
            "user@example.com",
            DataPermissionType.Self
        );
        var repository = new FakePermissionRepository([root, page, disabled])
        {
            AssociatedPermissionIds = [root.Id, page.Id, disabled.Id],
        };
        var publisher = new FakeUserPermissionCacheInvalidationPublisher();
        var userRepository = new FakeUserRepository(
            user,
            () => Assert.Equal(1, repository.CatalogSharedLockAcquireCount)
        );
        var (cacheService, _, _, _) = CreateCacheService();
        var application = new PermissionApplication(
            repository,
            cacheService,
            new FakeUserPermissionCacheService(),
            new FakePermissionCacheInvalidationPublisher(),
            publisher,
            new FakeUnitOfWork(),
            userRepository
        );

        var output = await application.GetAuthorizationAsync(user.Id, CancellationToken.None);
        await application.AuthorizeAsync(user.Id, [root.Id, page.Id], CancellationToken.None);

        Assert.Equal([root.Id, page.Id, disabled.Id], output.PermissionIds);
        Assert.Equal([root.Id, page.Id], repository.AssociatedPermissionIds);
        Assert.Equal(1, repository.CatalogSharedLockAcquireCount);
        Assert.Equal(0, repository.CatalogMutationLockAcquireCount);
        Assert.Equal([user.Id], userRepository.LockedUserIds);
        Assert.Equal([new UserPermissionCacheInvalidationMessage(user.Id, false)], publisher.Messages);
    }

    /// <summary>
    /// 验证授权保存拒绝重复权限和不完整祖先链，但允许保存禁用权限。
    /// </summary>
    [Fact]
    public async Task Authorization_ShouldRejectInvalidSelections()
    {
        var root = CreatePermission("Root", PermissionType.Directory);
        var page = CreatePermission("Page", PermissionType.Page, root.Id);
        var disabled = CreatePermission("Disabled", PermissionType.Button, page.Id, false);
        var user = User.Restore(
            Guid.CreateVersion7(),
            "user",
            "hash",
            "用户",
            "13800000000",
            "user@example.com",
            DataPermissionType.Self
        );
        var repository = new FakePermissionRepository([root, page, disabled]);
        var (cacheService, _, _, _) = CreateCacheService();
        var application = new PermissionApplication(
            repository,
            cacheService,
            new FakeUserPermissionCacheService(),
            new FakePermissionCacheInvalidationPublisher(),
            new FakeUserPermissionCacheInvalidationPublisher(),
            new FakeUnitOfWork(),
            new FakeUserRepository(user)
        );

        await Assert.ThrowsAsync<PermissionDomainException>(() =>
            application.AuthorizeAsync(user.Id, [root.Id, root.Id], CancellationToken.None)
        );
        await application.AuthorizeAsync(user.Id, [root.Id, page.Id, disabled.Id], CancellationToken.None);
        Assert.Equal([root.Id, page.Id, disabled.Id], repository.AssociatedPermissionIds);
        await Assert.ThrowsAsync<PermissionDomainException>(() =>
            application.AuthorizeAsync(user.Id, [page.Id], CancellationToken.None)
        );
    }

    [Fact]
    public async Task CacheInvalidationConsumer_ShouldInvalidateAllPermissionsCache()
    {
        var (cacheService, database, configuration, _) = CreateCacheService();
        var versionKey = PermissionCacheKeys.GetAllPermissionsVersionKey(configuration);
        database.SetString(versionKey, "old-version");
        var consumer = new PermissionCacheInvalidationConsumer(cacheService);

        await consumer.ConsumeAsync(
            new PermissionCacheInvalidationMessage(Guid.CreateVersion7(), DateTimeOffset.UtcNow),
            CancellationToken.None
        );

        Assert.False(database.StringExists(versionKey));
    }

    [Fact]
    public async Task UserPermissionCacheInvalidationConsumer_ShouldDeleteRequestedCache()
    {
        var userId = Guid.CreateVersion7();
        var cacheService = new FakeUserPermissionCacheService();
        var consumer = new UserPermissionCacheInvalidationConsumer(cacheService);

        await consumer.ConsumeAsync(new UserPermissionCacheInvalidationMessage(userId, false), CancellationToken.None);
        await consumer.ConsumeAsync(
            new UserPermissionCacheInvalidationMessage(Guid.Empty, true),
            CancellationToken.None
        );

        Assert.Equal([userId], cacheService.DeletedUserIds);
        Assert.Equal(1, cacheService.DeleteSuperAdminCount);
    }

    [Fact]
    public async Task RedisCacheInvalidation_ShouldWaitForLoadingAndRemoveLoadedVersion()
    {
        var permission = CreatePermission("Concurrent", PermissionType.Page);
        var (cacheService, database, configuration, redisCacheService) = CreateCacheService();
        var loadStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseLoad = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var loadTask = cacheService.GetOrLoadAsync(
            async _ =>
            {
                loadStarted.SetResult();
                await releaseLoad.Task;
                return [permission];
            },
            CancellationToken.None
        );
        await loadStarted.Task;

        var invalidationTask = cacheService.InvalidateAsync();
        Assert.False(invalidationTask.IsCompleted);
        Assert.Equal(TimeSpan.FromMinutes(2.5), redisCacheService.LastLockAcquireTimeout);

        releaseLoad.SetResult();
        await loadTask;
        await invalidationTask;

        Assert.False(database.StringExists(PermissionCacheKeys.GetAllPermissionsVersionKey(configuration)));
    }

    [Fact]
    public async Task RedisCache_ShouldReturnCompleteCachedPermissionWithoutReloading()
    {
        var parentId = Guid.CreateVersion7();
        var permission = CreatePermission("Cached", PermissionType.Page, parentId);
        permission.CreatedAt = DateTimeOffset.Parse("2026-08-09T01:02:03+00:00");
        permission.UpdatedBy = Guid.CreateVersion7();
        permission.UpdatedAt = DateTimeOffset.Parse("2026-08-09T02:03:04+00:00");
        permission.OrgId = Guid.CreateVersion7();
        var (cacheService, _, _, _) = CreateCacheService();
        var loadCount = 0;

        var first = await cacheService.GetOrLoadAsync(
            _ =>
            {
                loadCount++;
                return Task.FromResult<IReadOnlyList<Permission>>([permission]);
            },
            CancellationToken.None
        );
        var second = await cacheService.GetOrLoadAsync(
            _ =>
            {
                loadCount++;
                return Task.FromResult<IReadOnlyList<Permission>>([]);
            },
            CancellationToken.None
        );

        var cachedPermission = Assert.Single(second);
        Assert.Same(permission, Assert.Single(first));
        Assert.Equal(1, loadCount);
        Assert.Equal(permission.Id, cachedPermission.Id);
        Assert.Equal(permission.Code, cachedPermission.Code);
        Assert.Equal(permission.Path, cachedPermission.Path);
        Assert.Equal(permission.MetaData, cachedPermission.MetaData);
        Assert.Equal(permission.CreatedAt, cachedPermission.CreatedAt);
        Assert.Equal(permission.UpdatedBy, cachedPermission.UpdatedBy);
        Assert.Equal(permission.UpdatedAt, cachedPermission.UpdatedAt);
        Assert.Equal(permission.OrgId, cachedPermission.OrgId);
        Assert.Equal(permission.Version, cachedPermission.Version);
    }

    [Fact]
    public async Task RedisCache_ShouldReloadWhenLegacyChildPermissionHasNoPath()
    {
        var parentId = Guid.CreateVersion7();
        var permission = CreatePermission("LegacyCached", PermissionType.Page, parentId);
        var (cacheService, database, configuration, _) = CreateCacheService();
        var loadCount = 0;

        Task<IReadOnlyList<Permission>> LoadAsync(CancellationToken _)
        {
            loadCount++;
            return Task.FromResult<IReadOnlyList<Permission>>([permission]);
        }

        await cacheService.GetOrLoadAsync(LoadAsync, CancellationToken.None);
        var version = database.GetString(PermissionCacheKeys.GetAllPermissionsVersionKey(configuration));
        var metaValue = database.GetString(
            PermissionCacheKeys.GetAllPermissionsMetaKey(configuration, version.ToString())
        );
        var bucketKey = Assert.Single(
            JsonSerializer.Deserialize<PermissionCacheMeta>(metaValue.ToString())!.BucketKeys
        );
        var bucketValue = database.GetString(bucketKey).ToString();
        var legacyBucketValue = bucketValue.Replace($",\"Path\":\"{permission.Path}\"", string.Empty);
        Assert.NotEqual(bucketValue, legacyBucketValue);
        database.SetString(bucketKey, legacyBucketValue);

        var cachedPermissions = await cacheService.GetOrLoadAsync(LoadAsync, CancellationToken.None);

        Assert.Equal(2, loadCount);
        Assert.Equal(permission.Path, Assert.Single(cachedPermissions).Path);
    }

    [Fact]
    public async Task RedisCache_ShouldHonorPreCanceledTokenWhenCacheExists()
    {
        var permission = CreatePermission("Cached", PermissionType.Page);
        var (cacheService, _, _, _) = CreateCacheService();
        await cacheService.GetOrLoadAsync(
            _ => Task.FromResult<IReadOnlyList<Permission>>([permission]),
            CancellationToken.None
        );
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            cacheService.GetOrLoadAsync(
                _ => Task.FromResult<IReadOnlyList<Permission>>([]),
                cancellationTokenSource.Token
            )
        );
    }

    [Fact]
    public async Task RedisCache_ShouldHonorCancellationAfterLockedCacheRead()
    {
        var permission = CreatePermission("Cached", PermissionType.Page);
        var (cacheService, database, configuration, _) = CreateCacheService();
        await cacheService.GetOrLoadAsync(
            _ => Task.FromResult<IReadOnlyList<Permission>>([permission]),
            CancellationToken.None
        );
        var versionKey = PermissionCacheKeys.GetAllPermissionsVersionKey(configuration);
        var version = database.GetString(versionKey);
        Assert.True(database.Delete(versionKey));
        using var cancellationTokenSource = new CancellationTokenSource();
        var versionReadCount = 0;
        database.StringGetCompleted = key =>
        {
            if (key != versionKey)
            {
                return;
            }

            versionReadCount++;
            if (versionReadCount == 1)
            {
                database.SetString(versionKey, version);
            }
            else if (versionReadCount == 2)
            {
                cancellationTokenSource.Cancel();
            }
        };
        var loadCount = 0;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            cacheService.GetOrLoadAsync(
                _ =>
                {
                    loadCount++;
                    return Task.FromResult<IReadOnlyList<Permission>>([]);
                },
                cancellationTokenSource.Token
            )
        );

        Assert.Equal(2, versionReadCount);
        Assert.Equal(0, loadCount);
    }

    [Fact]
    public async Task RedisCache_ShouldNotPublishVersionWhenCanceledDuringCacheWrite()
    {
        var permission = CreatePermission("Canceled", PermissionType.Page);
        var (cacheService, database, configuration, _) = CreateCacheService();
        using var cancellationTokenSource = new CancellationTokenSource();
        database.StringSetCompleted = key =>
        {
            if (key.Contains(":meta:", StringComparison.Ordinal))
            {
                cancellationTokenSource.Cancel();
            }
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            cacheService.GetOrLoadAsync(
                _ => Task.FromResult<IReadOnlyList<Permission>>([permission]),
                cancellationTokenSource.Token
            )
        );

        Assert.False(database.StringExists(PermissionCacheKeys.GetAllPermissionsVersionKey(configuration)));
    }

    [Fact]
    public async Task RedisCache_ShouldCacheEmptyPermissionList()
    {
        var (cacheService, _, _, _) = CreateCacheService();
        var loadCount = 0;

        async Task<IReadOnlyList<Permission>> LoadAsync(CancellationToken _)
        {
            loadCount++;
            await Task.CompletedTask;
            return [];
        }

        Assert.Empty(await cacheService.GetOrLoadAsync(LoadAsync, CancellationToken.None));
        Assert.Empty(await cacheService.GetOrLoadAsync(LoadAsync, CancellationToken.None));
        Assert.Equal(1, loadCount);
    }

    [Fact]
    public async Task RedisCache_ShouldReloadAllPermissionsWhenBucketIsMissing()
    {
        var permissions = Enumerable
            .Range(0, 11)
            .Select(index => CreatePermission($"Permission{index}", PermissionType.Page))
            .ToArray();
        var (cacheService, database, configuration, _) = CreateCacheService();
        var loadCount = 0;

        Task<IReadOnlyList<Permission>> LoadAsync(CancellationToken _)
        {
            loadCount++;
            return Task.FromResult<IReadOnlyList<Permission>>(permissions);
        }

        Assert.Equal(permissions, await cacheService.GetOrLoadAsync(LoadAsync, CancellationToken.None));

        var version = database.GetString(PermissionCacheKeys.GetAllPermissionsVersionKey(configuration));
        var metaValue = database.GetString(
            PermissionCacheKeys.GetAllPermissionsMetaKey(configuration, version.ToString())
        );
        var bucketKeys = JsonSerializer.Deserialize<PermissionCacheMeta>(metaValue.ToString())?.BucketKeys;
        Assert.NotNull(bucketKeys);
        Assert.True(bucketKeys.Length > 1);
        Assert.True(database.Delete(bucketKeys[0]));

        Assert.Equal(permissions, await cacheService.GetOrLoadAsync(LoadAsync, CancellationToken.None));
        Assert.Equal(2, loadCount);
    }

    [Fact]
    public async Task RepositorySave_ShouldPersistNullableGuidParentAndAdvanceVersion()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"permission-update-{Guid.NewGuid():N}.db");
        try
        {
            using var db = CreateDatabase(databasePath);
            db.CodeFirst.InitTables<PermissionRecord>();
            var parentId = Guid.CreateVersion7();
            var permissionId = Guid.CreateVersion7();
            await db.Insertable(
                    new[] { CreateRecord(parentId, "Parent", null), CreateRecord(permissionId, "Editable", parentId) }
                )
                .ExecuteCommandAsync();
            var repository = new PermissionRepository(db);
            var permission = Assert.IsType<Permission>(
                await repository.FindAsync(permissionId, CancellationToken.None)
            );
            var originalVersion = permission.Version;

            permission.Update(
                "Editable",
                "修改后",
                PermissionType.Button,
                true,
                9,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                parentId,
                default,
                originalVersion,
                Guid.CreateVersion7(),
                DateTimeOffset.UtcNow
            );
            await repository.SaveAsync(permission, CancellationToken.None);

            var record = await db.Queryable<PermissionRecord>().Where(item => item.Id == permissionId).SingleAsync();
            Assert.Equal(parentId, record.ParentId);
            Assert.Equal("修改后", record.Title);
            Assert.NotEqual(originalVersion, record.Version);
            Assert.Equal(record.Version, permission.Version);
            db.Ado.Close();
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task RepositoryDelete_ShouldRemoveAssignmentsAndAllowCodeReuse()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"permission-delete-{Guid.NewGuid():N}.db");
        try
        {
            using var db = CreateDatabase(databasePath);
            db.CodeFirst.InitTables<PermissionRecord, UserPermissionRecord>();
            var permissionId = Guid.CreateVersion7();
            var userId = Guid.CreateVersion7();
            await db.Insertable(CreateRecord(permissionId, "Reusable", null)).ExecuteCommandAsync();
            await db.Insertable(new UserPermissionRecord { UserId = userId, PermissionId = permissionId })
                .ExecuteCommandAsync();
            var repository = new PermissionRepository(db);

            await repository.DeleteAsync(permissionId, CancellationToken.None);

            Assert.Equal(0, await db.Queryable<PermissionRecord>().CountAsync());
            Assert.Equal(0, await db.Queryable<UserPermissionRecord>().CountAsync());
            await db.Insertable(CreateRecord(Guid.CreateVersion7(), "Reusable", null)).ExecuteCommandAsync();
            Assert.Equal(1, await db.Queryable<PermissionRecord>().CountAsync());
            db.Ado.Close();
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task RepositoryDelete_ShouldRejectPermissionWithChildren()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"permission-child-{Guid.NewGuid():N}.db");
        try
        {
            using var db = CreateDatabase(databasePath);
            db.CodeFirst.InitTables<PermissionRecord, UserPermissionRecord>();
            var parentId = Guid.CreateVersion7();
            await db.Insertable(
                    new[]
                    {
                        CreateRecord(parentId, "Parent", null),
                        CreateRecord(Guid.CreateVersion7(), "Child", parentId),
                    }
                )
                .ExecuteCommandAsync();
            var repository = new PermissionRepository(db);

            var exception = await Assert.ThrowsAsync<PermissionDomainException>(() =>
                repository.DeleteAsync(parentId, CancellationToken.None)
            );
            Assert.Equal(MessageKeys.Permissions.HasChildren, exception.Message);
            Assert.Equal(DomainErrorType.Conflict, exception.ErrorType);
            Assert.Equal(2, await db.Queryable<PermissionRecord>().CountAsync());
            db.Ado.Close();
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    /// <summary>
    /// 验证授权关联保留禁用权限，而有效权限查询会过滤禁用祖先的完整后代链。
    /// </summary>
    [Fact]
    public async Task RepositoryPermissionQueries_ShouldFilterDisabledHierarchyOnlyForEffectivePermissions()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"permission-enabled-hierarchy-{Guid.NewGuid():N}.db");
        try
        {
            using var db = CreateDatabase(databasePath);
            db.CodeFirst.InitTables<PermissionRecord, UserPermissionRecord>();
            var rootId = Guid.CreateVersion7();
            var pageId = Guid.CreateVersion7();
            var buttonId = Guid.CreateVersion7();
            var userId = Guid.CreateVersion7();
            await db.Insertable(
                    new[]
                    {
                        CreateRecord(rootId, "Root", null, false),
                        CreateRecord(pageId, "Page", rootId),
                        CreateRecord(buttonId, "Button", pageId),
                    }
                )
                .ExecuteCommandAsync();
            await db.Insertable(
                    new[]
                    {
                        new UserPermissionRecord { UserId = userId, PermissionId = rootId },
                        new UserPermissionRecord { UserId = userId, PermissionId = pageId },
                        new UserPermissionRecord { UserId = userId, PermissionId = buttonId },
                    }
                )
                .ExecuteCommandAsync();
            var repository = new PermissionRepository(db);

            var associatedIds = await repository.GetIdsByUserIdAsync(userId, CancellationToken.None);
            var effectivePermissions = await repository.GetAllAsync(CancellationToken.None);
            var effectiveCodes = await repository.GetCodesByUserIdAsync(userId, CancellationToken.None);

            Assert.Equal([rootId, pageId, buttonId], associatedIds);
            Assert.Empty(effectivePermissions);
            Assert.Empty(effectiveCodes);
            db.Ado.Close();
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    /// <summary>
    /// 构造 UserApplication 测试实例：默认页路径用例仅触达用户仓储、用户信息缓存与权限仓储，
    /// 其余依赖按用例路径不会被触达，统一以 null 传入；新增依赖时只需调整此工厂。
    /// </summary>
    private static UserApplication CreateUserApplication(
        IUserRepository userRepository,
        UserInfoCacheService userInfoCacheService,
        IPermissionRepository permissionRepository
    ) =>
        new(
            userRepository,
            null!, // organizationRepository（本测试类不触达）
            userInfoCacheService,
            new FakeUnitOfWork(),
            null!, // tokenService（本测试类不触达）
            null!, // userTokenCacheService（本测试类不触达）
            null!, // userInfoCacheInvalidationPublisher（本测试类不触达）
            permissionRepository
        );

    private static Permission CreatePermission(
        string code,
        PermissionType type,
        Guid? parentId = null,
        bool enable = true
    )
    {
        var id = Guid.CreateVersion7();
        return new Permission(
            id,
            code,
            code,
            type,
            0,
            type == PermissionType.Directory ? "Folder" : string.Empty,
            type == PermissionType.Page ? $"/src/views/{code}.vue" : string.Empty,
            type == PermissionType.Page ? $"/{code}" : string.Empty,
            type == PermissionType.Page ? code : string.Empty,
            parentId,
            new PermissionMetaData(true),
            parentId is null ? string.Empty : $"{parentId}/{id}"
        )
        {
            Enable = enable,
        };
    }

    private static (
        PermissionCacheService Service,
        InMemoryRedisDatabase Database,
        CacheConfiguration Configuration,
        FakeRedisCacheService RedisCacheService
    ) CreateCacheService()
    {
        var database = DispatchProxy.Create<StackExchange.Redis.IDatabase, InMemoryRedisDatabase>();
        var configuration = new CacheConfiguration { KeyNamespace = "tests" };
        var redisCacheService = new FakeRedisCacheService(database);
        return (
            new PermissionCacheService(redisCacheService, Options.Create(configuration)),
            (InMemoryRedisDatabase)(object)database,
            configuration,
            redisCacheService
        );
    }

    private sealed record PermissionCacheMeta(string[] BucketKeys);

    private static CreatePermissionInput CreateInput(string code)
    {
        return new CreatePermissionInput(
            code,
            code,
            PermissionType.Page,
            true,
            0,
            string.Empty,
            $"/src/views/{code}.vue",
            $"/{code}",
            code,
            null,
            new PermissionMetaData(true)
        );
    }

    private static UpdatePermissionInput UpdateInput(Permission permission, Guid? parentId)
    {
        return new UpdatePermissionInput(
            permission.Code,
            permission.Title,
            permission.Type,
            permission.Enable,
            permission.SortOrder,
            permission.Icon,
            permission.VueComponentPath,
            permission.RoutePath,
            permission.RouteName,
            parentId,
            permission.MetaData,
            permission.Version
        );
    }

    private static SqlSugarClient CreateDatabase(string path)
    {
        return new SqlSugarClient(
            new ConnectionConfig
            {
                DbType = DbType.Sqlite,
                ConnectionString = $"Data Source={path};Pooling=False",
                IsAutoCloseConnection = true,
            }
        );
    }

    private static PermissionRecord CreateRecord(Guid id, string code, Guid? parentId, bool enable = true)
    {
        return new PermissionRecord
        {
            Id = id,
            Code = code,
            Title = code,
            Type = PermissionType.Directory,
            Icon = "Folder",
            ParentId = parentId,
            Path = parentId is null ? id.ToString() : $"{parentId}/{id}",
            MetaData = new PermissionMetaData(true),
            Enable = enable,
        };
    }

    private sealed class FakeUserPermissionCacheService : IUserPermissionCacheService
    {
        public IReadOnlyList<Guid> InvalidatedUserIds { get; private set; } = [];

        public IReadOnlyList<Guid> DeletedUserIds { get; private set; } = [];

        public int DeleteSuperAdminCount { get; private set; }

        public Task InvalidateAsync(IReadOnlyCollection<Guid> userIds)
        {
            InvalidatedUserIds = [.. userIds];
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<string>> GetCodesAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>([]);

        public Task DeleteAsync(Guid userId, CancellationToken cancellationToken)
        {
            DeletedUserIds = [.. DeletedUserIds, userId];
            return Task.CompletedTask;
        }

        public Task DeleteSuperAdminAsync(CancellationToken cancellationToken)
        {
            DeleteSuperAdminCount++;
            return Task.CompletedTask;
        }

        public Task<bool> HasAsync(
            Guid userId,
            IReadOnlyCollection<string> permissionCodes,
            PermissionMatchMode matchMode,
            CancellationToken cancellationToken
        ) => Task.FromResult(false);
    }

    private sealed class FakeUserRepository(User user, Action? onAcquireSecurityLock = null) : IUserRepository
    {
        public int GetCount { get; private set; }

        /// <summary>
        /// 测试期间被安全锁锁定的用户 ID。
        /// </summary>
        public IReadOnlyList<Guid> LockedUserIds { get; private set; } = [];

        public Task AcquireSecurityLockAsync(Guid id, CancellationToken cancellationToken)
        {
            onAcquireSecurityLock?.Invoke();
            LockedUserIds = [.. LockedUserIds, id];
            return Task.CompletedTask;
        }

        public Task<User?> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GetCount++;
            return Task.FromResult(id == user.Id ? user : null);
        }

        public Task<User?> GetByAccountAsync(string account, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(string.Equals(account, user.Username, StringComparison.Ordinal) ? user : null);
        }

        public Task<UserPage> GetPageAsync(UserPageRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new UserPage([user], 1));

        public Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken) =>
            Task.FromResult(username == user.Username);

        public Task AddAsync(User value, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SaveAsync(User value, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SavePasswordAsync(User value, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task DeleteAsync(Guid id, Guid version, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakePermissionCacheInvalidationPublisher : IPermissionCacheInvalidationPublisher
    {
        public int PublishCount { get; private set; }

        public Task PublishAsync(CancellationToken cancellationToken)
        {
            PublishCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUserPermissionCacheInvalidationPublisher : IUserPermissionCacheInvalidationPublisher
    {
        public IReadOnlyList<UserPermissionCacheInvalidationMessage> Messages { get; private set; } = [];

        public Task PublishAsync(Guid userId, bool isSuperAdmin, CancellationToken cancellationToken)
        {
            Messages = [.. Messages, new UserPermissionCacheInvalidationMessage(userId, isSuperAdmin)];
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int ExecuteCount { get; private set; }

        public Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ExecuteCount++;
            return action();
        }
    }

    private sealed class FakePermissionRepository(
        IReadOnlyCollection<Permission> permissions,
        IReadOnlyList<Guid>? userIds = null
    ) : IPermissionRepository
    {
        private readonly List<Permission> items = [.. permissions];

        /// <summary>
        /// 测试仓储保存的用户权限关联 ID。
        /// </summary>
        public IReadOnlyList<Guid> AssociatedPermissionIds { get; set; } = [];

        /// <summary>
        /// 有效权限列表查询次数。
        /// </summary>
        public int GetAllCount { get; private set; }

        /// <summary>
        /// 用户有效权限编码查询次数。
        /// </summary>
        public int GetCodesByUserIdCount { get; private set; }

        /// <summary>
        /// 权限目录共享锁获取次数。
        /// </summary>
        public int CatalogSharedLockAcquireCount { get; private set; }

        /// <summary>
        /// 权限目录变更锁获取次数。
        /// </summary>
        public int CatalogMutationLockAcquireCount { get; private set; }

        /// <summary>
        /// 测试期间要求权限目录查询和写入必须已持有变更锁。
        /// </summary>
        public bool RequireCatalogMutationLock { get; set; }

        public Task AcquireCatalogSharedLockAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CatalogSharedLockAcquireCount++;
            return Task.CompletedTask;
        }

        public Task AcquireCatalogMutationLockAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CatalogMutationLockAcquireCount++;
            return Task.CompletedTask;
        }

        public Task<Permission?> FindAsync(Guid id, CancellationToken cancellationToken)
        {
            EnsureCatalogMutationLock();
            return Task.FromResult(items.SingleOrDefault(permission => permission.Id == id));
        }

        public Task<bool> CodeExistsAsync(string code, Guid? excludedId, CancellationToken cancellationToken)
        {
            EnsureCatalogMutationLock();
            return Task.FromResult(items.Any(permission => permission.Code == code && permission.Id != excludedId));
        }

        public Task<bool> HasChildrenAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(items.Any(permission => permission.ParentId == id));

        public Task<IReadOnlyList<Guid>> GetUserIdsAsync(Guid permissionId, CancellationToken cancellationToken) =>
            Task.FromResult(userIds ?? []);

        public Task AddAsync(Permission permission, CancellationToken cancellationToken)
        {
            EnsureCatalogMutationLock();
            items.Add(permission);
            return Task.CompletedTask;
        }

        public Task SaveAsync(Permission permission, CancellationToken cancellationToken)
        {
            EnsureCatalogMutationLock();
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            EnsureCatalogMutationLock();
            items.RemoveAll(permission => permission.Id == id);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Permission>> GetListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Permission>>(items);

        public Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken cancellationToken)
        {
            GetAllCount++;
            return Task.FromResult<IReadOnlyList<Permission>>(items.Where(permission => permission.Enable).ToList());
        }

        public Task<IReadOnlyList<string>> GetCodesByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            GetCodesByUserIdCount++;
            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        public Task<IReadOnlyList<Guid>> GetIdsByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(AssociatedPermissionIds);

        public Task ReplaceUserPermissionsAsync(
            Guid userId,
            IReadOnlyCollection<Guid> permissionIds,
            CancellationToken cancellationToken
        )
        {
            AssociatedPermissionIds = [.. permissionIds];
            return Task.CompletedTask;
        }

        private void EnsureCatalogMutationLock()
        {
            if (RequireCatalogMutationLock && CatalogMutationLockAcquireCount == 0)
            {
                throw new InvalidOperationException("权限目录查询或写入前必须先获取变更锁");
            }
        }
    }
}
