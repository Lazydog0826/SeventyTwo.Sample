using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using SeventyTwo.Sample.Application.Permissions;
using SeventyTwo.Sample.Domain.Permissions;
using SeventyTwo.Sample.Infrastructure.Permissions;
using SeventyTwo.Sample.WebApi.Authentication;
using SeventyTwo.Sample.WebApi.Controllers;
using SqlSugar;

namespace SeventyTwo.Sample.ArchitectureTests;

public sealed class PermissionCrudTests
{
    [Theory]
    [InlineData(nameof(PermissionsController.CreateAsync), "create", "Permissions.Create")]
    [InlineData(nameof(PermissionsController.UpdateAsync), "update", "Permissions.Update")]
    [InlineData(nameof(PermissionsController.DeleteAsync), "delete", "Permissions.Delete")]
    public void MutationEndpoints_ShouldRequireDedicatedPermission(string methodName, string route, string code)
    {
        var action = typeof(PermissionsController).GetMethod(methodName);

        var httpPost = Assert.IsType<HttpPostAttribute>(action?.GetCustomAttribute<HttpPostAttribute>());
        var permission = Assert.IsType<PermissionAttribute>(action?.GetCustomAttribute<PermissionAttribute>());
        Assert.Equal(route, httpPost.Template);
        Assert.Equal($"Permission:All:{code}", permission.Policy);
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
            null,
            null,
            null,
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
                null,
                null,
                null,
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
        var repository = new FakePermissionRepository([parent, child]);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var application = new PermissionApplication(repository, cache, new FakePermissionChecker());

        await Assert.ThrowsAsync<PermissionDomainException>(() =>
            application.CreateAsync(CreateInput("Parent"), CancellationToken.None)
        );
        await Assert.ThrowsAsync<PermissionDomainException>(() =>
            application.UpdateAsync(parent.Id, UpdateInput(parent, child.Id), CancellationToken.None)
        );
    }

    [Fact]
    public async Task ApplicationMutation_ShouldInvalidatePermissionCaches()
    {
        var permission = CreatePermission("Editable", PermissionType.Page);
        var userId = Guid.CreateVersion7();
        var repository = new FakePermissionRepository([permission], [userId]);
        var checker = new FakePermissionChecker();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        cache.Set("Permissions:All", new[] { permission });
        var application = new PermissionApplication(repository, cache, checker);

        await application.UpdateAsync(permission.Id, UpdateInput(permission, null), CancellationToken.None);

        Assert.False(cache.TryGetValue("Permissions:All", out _));
        Assert.Equal([userId], checker.InvalidatedUserIds);
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

            await Assert.ThrowsAsync<PermissionDomainException>(() =>
                repository.DeleteAsync(parentId, CancellationToken.None)
            );
            Assert.Equal(2, await db.Queryable<PermissionRecord>().CountAsync());
            db.Ado.Close();
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    private static Permission CreatePermission(string code, PermissionType type, Guid? parentId = null)
    {
        return new Permission(
            Guid.CreateVersion7(),
            code,
            code,
            type,
            0,
            type == PermissionType.Directory ? "Folder" : string.Empty,
            type == PermissionType.Page ? $"/src/views/{code}.vue" : null,
            type == PermissionType.Page ? $"/{code}" : null,
            type == PermissionType.Page ? code : null,
            parentId,
            new PermissionMetaData(true)
        );
    }

    private static CreatePermissionInput CreateInput(string code)
    {
        return new CreatePermissionInput(
            code,
            code,
            PermissionType.Page,
            true,
            0,
            null,
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

    private static PermissionRecord CreateRecord(Guid id, string code, Guid? parentId)
    {
        return new PermissionRecord
        {
            Id = id,
            Code = code,
            Title = code,
            Type = PermissionType.Directory,
            Icon = "Folder",
            ParentId = parentId,
            MetaData = new PermissionMetaData(true),
        };
    }

    private sealed class FakePermissionChecker : IUserPermissionChecker
    {
        public IReadOnlyList<Guid> InvalidatedUserIds { get; private set; } = [];

        public Task InvalidateAsync(IReadOnlyCollection<Guid> userIds)
        {
            InvalidatedUserIds = [.. userIds];
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<string>> GetCodesAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>([]);

        public Task<bool> HasAsync(
            Guid userId,
            IReadOnlyCollection<string> permissionCodes,
            PermissionMatchMode matchMode,
            CancellationToken cancellationToken
        ) => Task.FromResult(false);
    }

    private sealed class FakePermissionRepository(
        IReadOnlyCollection<Permission> permissions,
        IReadOnlyList<Guid>? userIds = null
    ) : IPermissionRepository
    {
        private readonly List<Permission> items = [.. permissions];

        public Task<Permission?> FindAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(items.SingleOrDefault(permission => permission.Id == id));

        public Task<bool> CodeExistsAsync(string code, Guid? excludedId, CancellationToken cancellationToken) =>
            Task.FromResult(items.Any(permission => permission.Code == code && permission.Id != excludedId));

        public Task<bool> HasChildrenAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(items.Any(permission => permission.ParentId == id));

        public Task<IReadOnlyList<Guid>> GetUserIdsAsync(Guid permissionId, CancellationToken cancellationToken) =>
            Task.FromResult(userIds ?? []);

        public Task AddAsync(Permission permission, CancellationToken cancellationToken)
        {
            items.Add(permission);
            return Task.CompletedTask;
        }

        public Task SaveAsync(Permission permission, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            items.RemoveAll(permission => permission.Id == id);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Permission>> GetListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Permission>>(items);

        public Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Permission>>(items.Where(permission => permission.Enable).ToList());

        public Task<IReadOnlyList<string>> GetCodesByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>([]);
    }
}
