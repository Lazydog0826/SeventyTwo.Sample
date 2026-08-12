using System.Reflection;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using SeventyTwo.Sample.Application;
using SeventyTwo.Sample.Application.Organizations;
using SeventyTwo.Sample.Common.MessageKeys;
using SeventyTwo.Sample.Domain.Organizations;
using SeventyTwo.Sample.Infrastructure.Organizations;
using SeventyTwo.Sample.Infrastructure.Users;
using SeventyTwo.Sample.WebApi.Authentication;
using SeventyTwo.Sample.WebApi.Controllers;
using SqlSugar;

namespace SeventyTwo.Sample.ArchitectureTests;

public sealed class OrganizationCrudTests
{
    static OrganizationCrudTests()
    {
        new OrganizationMappingProfile().Register(TypeAdapterConfig.GlobalSettings);
    }

    [Fact]
    public async Task Create_ShouldSupportMultipleRootsAndInheritParentRoot()
    {
        var repository = new FakeOrganizationRepository([]);
        var application = new OrganizationApplication(repository, new FakeUnitOfWork());

        var root1 = await application.CreateAsync(new("ROOT", "根一", true, null), CancellationToken.None);
        var root2 = await application.CreateAsync(new("ROOT", "根二", true, null), CancellationToken.None);
        var child = await application.CreateAsync(new("CHILD", "子机构", true, root1.Id), CancellationToken.None);

        Assert.NotEqual(root1.Id, root2.Id);
        Assert.Equal(root1.Id, repository.Items.Single(item => item.Id == root1.Id).OrgId);
        Assert.Equal(root2.Id, repository.Items.Single(item => item.Id == root2.Id).OrgId);
        Assert.Equal(root1.Id, repository.Items.Single(item => item.Id == child.Id).OrgId);
        Assert.Equal($"{root1.Id}/{child.Id}", repository.Items.Single(item => item.Id == child.Id).Path);
    }

    [Fact]
    public async Task Create_WithDuplicateCodeInSameRoot_ShouldFail()
    {
        var root = CreateOrganization("ROOT", null);
        var existing = CreateOrganization("DUP", root.Id, root.Id);
        var application = new OrganizationApplication(new FakeOrganizationRepository([root, existing]), new FakeUnitOfWork());

        await Assert.ThrowsAsync<OrganizationDomainException>(() =>
            application.CreateAsync(new("DUP", "重复", true, root.Id), CancellationToken.None)
        );
    }

    [Fact]
    public async Task Create_WithEmptyParentId_ShouldReturnParentValidation()
    {
        var application = new OrganizationApplication(new FakeOrganizationRepository([]), new FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<OrganizationDomainException>(() =>
            application.CreateAsync(new("CODE", "机构", true, Guid.Empty), CancellationToken.None)
        );

        Assert.Equal(MessageKeys.Organizations.ParentIdRequired, exception.Message);
    }

    [Fact]
    public async Task Update_ShouldAllowSameRootReparenting()
    {
        var root = CreateOrganization("ROOT", null);
        var left = CreateOrganization("LEFT", root.Id, root.Id);
        var right = CreateOrganization("RIGHT", root.Id, root.Id);
        var application = new OrganizationApplication(new FakeOrganizationRepository([root, left, right]), new FakeUnitOfWork());

        await application.UpdateAsync(
            left.Id,
            new(left.Code, left.Name, left.Enable, right.Id, left.Version),
            CancellationToken.None
        );

        Assert.Equal(right.Id, left.ParentId);
        Assert.Equal($"{root.Id}/{right.Id}/{left.Id}", left.Path);
    }

    [Fact]
    public async Task Update_WithSelfParent_ShouldReturnSelfParentValidation()
    {
        var organization = CreateOrganization("ROOT", null);
        var application = new OrganizationApplication(
            new FakeOrganizationRepository([organization]),
            new FakeUnitOfWork()
        );

        var exception = await Assert.ThrowsAsync<OrganizationDomainException>(() =>
            application.UpdateAsync(
                organization.Id,
                new(organization.Code, organization.Name, organization.Enable, organization.Id, organization.Version),
                CancellationToken.None
            )
        );

        Assert.Equal(MessageKeys.Organizations.SelfCannotBeParent, exception.Message);
    }

    [Theory]
    [InlineData("root-to-child", MessageKeys.Organizations.RootCannotBeChild)]
    [InlineData("child-to-root", MessageKeys.Organizations.ChildCannotBeRoot)]
    [InlineData("cross-root", MessageKeys.Organizations.CrossRootMoveNotAllowed)]
    [InlineData("descendant", MessageKeys.Organizations.DescendantCannotBeParent)]
    public async Task Update_WithInvalidHierarchyChange_ShouldFail(string scenario, string expectedMessage)
    {
        var root1 = CreateOrganization("ROOT1", null);
        var root2 = CreateOrganization("ROOT2", null);
        var child = CreateOrganization("CHILD", root1.Id, root1.Id);
        var descendant = CreateOrganization("DESC", child.Id, root1.Id);
        var application = new OrganizationApplication(
            new FakeOrganizationRepository([root1, root2, child, descendant]),
            new FakeUnitOfWork()
        );
        var target = scenario == "root-to-child" ? root1 : child;
        Guid? parentId = scenario switch
        {
            "root-to-child" => root2.Id,
            "child-to-root" => null,
            "cross-root" => root2.Id,
            _ => descendant.Id,
        };

        var exception = await Assert.ThrowsAsync<OrganizationDomainException>(() =>
            application.UpdateAsync(
                target.Id,
                new(target.Code, target.Name, target.Enable, parentId, target.Version),
                CancellationToken.None
            )
        );

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Theory]
    [InlineData(nameof(OrganizationsController.GetListAsync), "list", "organizationsList")]
    [InlineData(nameof(OrganizationsController.CreateAsync), "create", "organizationsCreate")]
    [InlineData(nameof(OrganizationsController.UpdateAsync), "update", "organizationsUpdate")]
    [InlineData(nameof(OrganizationsController.DeleteAsync), "delete", "organizationsDelete")]
    public async Task Endpoints_ShouldUseDedicatedPermission(string methodName, string route, string code)
    {
        var method = typeof(OrganizationsController).GetMethod(methodName)!;
        var routeAttribute = method.GetCustomAttributes().OfType<HttpMethodAttribute>().Single();
        var permission = method.GetCustomAttribute<PermissionAttribute>()!;
        var policy = await new PermissionPolicyProvider(
            Microsoft.Extensions.Options.Options.Create(new Microsoft.AspNetCore.Authorization.AuthorizationOptions())
        ).GetPolicyAsync(permission.Policy!);
        var requirement = Assert.Single(policy!.Requirements.OfType<PermissionRequirement>());

        Assert.Equal(route, routeAttribute.Template);
        Assert.Equal([code], requirement.PermissionCodes);
    }

    [Fact]
    public async Task CreateEndpoint_ShouldPassSortOrderToApplication()
    {
        var application = new CapturingOrganizationApplication();
        var controller = new OrganizationsController(application);

        await controller.CreateAsync(new("CODE", "机构", true, null, 3), CancellationToken.None);

        Assert.Equal(3, Assert.IsType<CreateOrganizationInput>(application.CreateInput).SortOrder);
    }

    [Fact]
    public async Task UpdateEndpoint_ShouldPassSortOrderToApplication()
    {
        var application = new CapturingOrganizationApplication();
        var controller = new OrganizationsController(application);

        await controller.UpdateAsync(
            new(Guid.CreateVersion7(), "CODE", "机构", true, null, Guid.CreateVersion7(), 4),
            CancellationToken.None
        );

        Assert.Equal(4, Assert.IsType<UpdateOrganizationInput>(application.UpdateInput).SortOrder);
    }

    [Fact]
    public async Task Repository_ShouldPersistUpdateAndAdvanceVersion()
    {
        var path = Path.Combine(Path.GetTempPath(), $"organization-update-{Guid.NewGuid():N}.db");
        try
        {
            using var db = CreateDatabase(path);
            db.CodeFirst.InitTables<OrganizationRecord>();
            var id = Guid.CreateVersion7();
            await db.Insertable(CreateRecord(id, "OLD", null, id)).ExecuteCommandAsync();
            var repository = new OrganizationRepository(db);
            var organization = Assert.IsType<Organization>(await repository.FindAsync(id, CancellationToken.None));
            var version = organization.Version;
            organization.Update("NEW", "新名称", false, null, version, Guid.Empty, DateTimeOffset.UtcNow);

            await repository.SaveAsync(organization, CancellationToken.None);

            var record = await db.Queryable<OrganizationRecord>().SingleAsync(item => item.Id == id);
            Assert.Equal("NEW", record.Code);
            Assert.False(record.Enable);
            Assert.NotEqual(version, record.Version);
            db.Ado.Close();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task RepositoryReparent_ShouldUpdateDescendantPaths()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"organization-reparent-{Guid.NewGuid():N}.db");
        try
        {
            using var db = CreateDatabase(databasePath);
            db.CodeFirst.InitTables<OrganizationRecord>();
            var rootId = Guid.CreateVersion7();
            var leftId = Guid.CreateVersion7();
            var rightId = Guid.CreateVersion7();
            var childId = Guid.CreateVersion7();
            await db.Insertable(
                    new[]
                    {
                        CreateRecord(rootId, "ROOT", null, rootId),
                        CreateRecord(leftId, "LEFT", rootId, rootId, $"{rootId}/{leftId}"),
                        CreateRecord(rightId, "RIGHT", rootId, rootId, $"{rootId}/{rightId}"),
                        CreateRecord(childId, "CHILD", leftId, rootId, $"{rootId}/{leftId}/{childId}"),
                    }
                )
                .ExecuteCommandAsync();
            var repository = new OrganizationRepository(db);
            var left = Assert.IsType<Organization>(await repository.FindAsync(leftId, CancellationToken.None));
            left.Update("LEFT", "LEFT", true, rightId, left.Version, Guid.Empty, DateTimeOffset.UtcNow);
            left.ChangePath($"{rootId}/{rightId}");

            await repository.SaveAsync(left, CancellationToken.None);

            var child = await db.Queryable<OrganizationRecord>().SingleAsync(item => item.Id == childId);
            Assert.Equal($"{rootId}/{rightId}/{leftId}/{childId}", child.Path);
            db.Ado.Close();
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task RepositoryDelete_WithChildren_ShouldFail()
    {
        var path = Path.Combine(Path.GetTempPath(), $"organization-delete-blocked-{Guid.NewGuid():N}.db");
        try
        {
            using var db = CreateDatabase(path);
            db.CodeFirst.InitTables<OrganizationRecord, UserAccountRecord>();
            var id = Guid.CreateVersion7();
            await db.Insertable(CreateRecord(id, "ROOT", null, id)).ExecuteCommandAsync();
            await db.Insertable(CreateRecord(Guid.CreateVersion7(), "CHILD", id, id)).ExecuteCommandAsync();

            await Assert.ThrowsAsync<OrganizationDomainException>(() =>
                new OrganizationRepository(db).DeleteAsync(id, CancellationToken.None)
            );
            db.Ado.Close();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task RepositoryDelete_WithoutRelations_ShouldRemoveAndAllowCodeReuse()
    {
        var path = Path.Combine(Path.GetTempPath(), $"organization-delete-{Guid.NewGuid():N}.db");
        try
        {
            using var db = CreateDatabase(path);
            db.CodeFirst.InitTables<OrganizationRecord, UserAccountRecord>();
            var id = Guid.CreateVersion7();
            await db.Insertable(CreateRecord(id, "REUSE", null, id)).ExecuteCommandAsync();

            await new OrganizationRepository(db).DeleteAsync(id, CancellationToken.None);

            Assert.Equal(0, await db.Queryable<OrganizationRecord>().CountAsync());
            await db.Insertable(CreateRecord(Guid.CreateVersion7(), "REUSE", null, Guid.CreateVersion7()))
                .ExecuteCommandAsync();
            db.Ado.Close();
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static Organization CreateOrganization(string code, Guid? parentId, Guid? orgId = null)
    {
        var id = Guid.CreateVersion7();
        var path = parentId is null ? null : $"{(orgId ?? parentId).Value}/{id}";
        var organization = new Organization(id, code, code, parentId, path)
        {
            Version = Guid.CreateVersion7(),
        };
        organization.OrgId = orgId ?? organization.Id;
        return organization;
    }

    private static SqlSugarClient CreateDatabase(string path) =>
        new(new ConnectionConfig { DbType = DbType.Sqlite, ConnectionString = $"Data Source={path};Pooling=False", IsAutoCloseConnection = true });

    private static OrganizationRecord CreateRecord(
        Guid id,
        string code,
        Guid? parentId,
        Guid orgId,
        string? path = null
    ) =>
        new()
        {
            Id = id,
            Code = code,
            Name = code,
            ParentId = parentId,
            Path = path ?? (parentId is null ? id.ToString() : $"{orgId}/{id}"),
            OrgId = orgId,
            Version = Guid.CreateVersion7(),
        };

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken) => action();
    }

    private sealed class CapturingOrganizationApplication : IOrganizationApplication
    {
        public CreateOrganizationInput? CreateInput { get; private set; }

        public UpdateOrganizationInput? UpdateInput { get; private set; }

        public Task<OrganizationListOutput> CreateAsync(
            CreateOrganizationInput input,
            CancellationToken cancellationToken
        )
        {
            CreateInput = input;
            return Task.FromResult(new OrganizationListOutput());
        }

        public Task UpdateAsync(Guid id, UpdateOrganizationInput input, CancellationToken cancellationToken)
        {
            UpdateInput = input;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<OrganizationListOutput>> GetListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<OrganizationListOutput>>([]);
    }

    private sealed class FakeOrganizationRepository(IEnumerable<Organization> organizations) : IOrganizationRepository
    {
        public List<Organization> Items { get; } = [.. organizations];

        public Task AcquireMutationLockAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<Organization?> FindAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.SingleOrDefault(item => item.Id == id));

        public Task<IReadOnlyList<Organization>> GetListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Organization>>(Items);

        public Task<bool> CodeExistsAsync(Guid orgId, string code, Guid? excludedId, CancellationToken cancellationToken) =>
            Task.FromResult(Items.Any(item => item.OrgId == orgId && item.Code == code && item.Id != excludedId));

        public Task AddAsync(Organization organization, CancellationToken cancellationToken)
        {
            Items.Add(organization);
            return Task.CompletedTask;
        }

        public Task SaveAsync(Organization organization, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item => item.Id == id);
            return Task.CompletedTask;
        }
    }
}
