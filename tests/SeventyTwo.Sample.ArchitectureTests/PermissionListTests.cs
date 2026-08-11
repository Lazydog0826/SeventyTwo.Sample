using System.Reflection;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SeventyTwo.Sample.Application.Permissions;
using SeventyTwo.Sample.Domain.Permissions;
using SeventyTwo.Sample.Infrastructure.Permissions;
using SeventyTwo.Sample.WebApi.Authentication;
using SeventyTwo.Sample.WebApi.Controllers;
using SqlSugar;

namespace SeventyTwo.Sample.ArchitectureTests;

public sealed class PermissionListTests
{
    [Fact]
    public void PersistenceMapping_ShouldPreserveDisabledState()
    {
        var configuration = CreateMapsterConfiguration();
        var record = CreateRecord("Disabled", false);

        var permission = record.Adapt<Permission>(configuration);

        Assert.False(permission.Enable);
        Assert.Equal(record.Id, permission.Id);
        Assert.Equal(record.CreatedAt, permission.CreatedAt);
    }

    [Fact]
    public async Task RepositoryList_ShouldIncludeDisabledAndExcludeSoftDeleted()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"permission-list-{Guid.NewGuid():N}.db");
        try
        {
            using var db = new SqlSugarClient(
                new ConnectionConfig
                {
                    DbType = DbType.Sqlite,
                    ConnectionString = $"Data Source={databasePath};Pooling=False",
                    IsAutoCloseConnection = true,
                }
            );
            db.CodeFirst.InitTables<PermissionRecord>();
            await db.Insertable(
                    new[]
                    {
                        CreateRecord("Enabled", true, 2),
                        CreateRecord("Disabled", false, 1),
                        CreateRecord("Deleted", false, 0, DateTimeOffset.UtcNow),
                    }
                )
                .ExecuteCommandAsync();
            var repository = new PermissionRepository(db);

            var list = await repository.GetListAsync(CancellationToken.None);
            var active = await repository.GetAllAsync(CancellationToken.None);

            Assert.Equal(["Disabled", "Enabled"], list.Select(x => x.Code));
            Assert.False(list[0].Enable);
            Assert.Equal("Enabled", Assert.Single(active).Code);
            db.Ado.Close();
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task ListEndpoint_ShouldRequirePermissionListPermission()
    {
        var action = typeof(PermissionsController).GetMethod(nameof(PermissionsController.GetListAsync));

        var httpGet = Assert.IsType<HttpGetAttribute>(action?.GetCustomAttribute<HttpGetAttribute>());
        var permission = Assert.IsType<PermissionAttribute>(action?.GetCustomAttribute<PermissionAttribute>());
        Assert.Equal("list", httpGet.Template);

        var policyProvider = new PermissionPolicyProvider(Options.Create(new AuthorizationOptions()));
        var policy = await policyProvider.GetPolicyAsync(Assert.IsType<string>(permission.Policy));
        var requirement = Assert.Single(
            Assert.IsType<AuthorizationPolicy>(policy).Requirements.OfType<PermissionRequirement>()
        );
        Assert.Equal(PermissionMatchMode.All, requirement.MatchMode);
        Assert.Equal(["permissionsList"], requirement.PermissionCodes);
    }

    private static TypeAdapterConfig CreateMapsterConfiguration()
    {
        var configuration = new TypeAdapterConfig();
        configuration.Scan(typeof(Infrastructure.AssemblyMarker).Assembly);
        configuration.Compile();
        return configuration;
    }

    private static PermissionRecord CreateRecord(
        string code,
        bool enable,
        int sortOrder = 0,
        DateTimeOffset? deleteAt = null
    )
    {
        return new PermissionRecord
        {
            Code = code,
            Title = code,
            Type = PermissionType.Page,
            Enable = enable,
            SortOrder = sortOrder,
            VueComponentPath = $"/src/views/{code}.vue",
            RoutePath = $"/{code}",
            RouteName = code,
            MetaData = new PermissionMetaData(true),
            DeleteAt = deleteAt,
        };
    }
}
