using Microsoft.AspNetCore.Identity;
using SeventyTwo.Sample.Domain.Permissions;
using SeventyTwo.Sample.Infrastructure;
using SeventyTwo.Sample.Infrastructure.Organizations;
using SeventyTwo.Sample.Infrastructure.Permissions;
using SeventyTwo.Sample.Infrastructure.Users;
using SqlSugar;

const string connectionString =
    "Host=xuniji.com;Port=5432;Database=SeventyTwo.Sample;Username=postgres;Password=123456";
const string organizationCode = "DefaultOrg";
const string organizationName = "测试机构";
const string userName = "superadmin";
const string displayName = "超级管理员";
const string initialPassword = "123456";

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("请先在 Program.cs 中填写 PostgreSQL 连接字符串。");
}

using var db = new SqlSugarClient(
    new ConnectionConfig
    {
        DbType = DbType.PostgreSQL,
        IsAutoCloseConnection = true,
        ConnectionString = connectionString,
    }
);

var entityTypes = typeof(AssemblyMarker)
    .Assembly.GetTypes()
    .Where(type => !type.IsAbstract && type.IsDefined(typeof(SugarTable), false))
    .ToArray();

db.Ado.BeginTran();
try
{
    db.CodeFirst.InitTables(entityTypes);
    Console.WriteLine($"已根据 {entityTypes.Length} 个数据库实体完成建表。");

    var organizationId = Guid.CreateVersion7();
    var userId = Guid.CreateVersion7();
    var homePermissionId = Guid.CreateVersion7();
    var permissionsPermissionId = Guid.CreateVersion7();
    var permissionsListPermissionId = Guid.CreateVersion7();
    var permissionsCreatePermissionId = Guid.CreateVersion7();
    var permissionsUpdatePermissionId = Guid.CreateVersion7();
    var permissionsDeletePermissionId = Guid.CreateVersion7();
    var passwordHash = new PasswordHasher<string>().HashPassword(userName, initialPassword);

    db.Insertable(
            new OrganizationRecord
            {
                Id = organizationId,
                Code = organizationCode,
                Name = organizationName,
                OrgId = organizationId,
            }
        )
        .ExecuteCommand();
    db.Insertable(
            new UserAccountRecord
            {
                Id = userId,
                Username = userName,
                PasswordHash = passwordHash,
                DisplayName = displayName,
                OrgId = organizationId,
            }
        )
        .ExecuteCommand();
    db.Insertable(
            new OrganizationMemberRecord
            {
                OrganizationId = organizationId,
                UserId = userId,
                IsPrimary = true,
                OrgId = organizationId,
            }
        )
        .ExecuteCommand();
    db.Insertable(
            new PermissionRecord
            {
                Id = homePermissionId,
                Code = "Home",
                Title = "首页",
                Type = PermissionType.Page,
                SortOrder = 0,
                Icon = "House",
                VueComponentPath = "/src/views/home.vue",
                RoutePath = "/home",
                RouteName = "home",
                ParentId = null,
                MetaData = new PermissionMetaData(true),
                OrgId = organizationId,
            }
        )
        .ExecuteCommand();
    db.Insertable(
            new[]
            {
                new PermissionRecord
                {
                    Id = permissionsPermissionId,
                    Code = "Permissions",
                    Title = "权限管理",
                    Type = PermissionType.Directory,
                    SortOrder = 100,
                    Icon = "UserShield",
                    VueComponentPath = string.Empty,
                    RoutePath = string.Empty,
                    RouteName = string.Empty,
                    ParentId = null,
                    MetaData = new PermissionMetaData(true),
                    OrgId = organizationId,
                },
                new PermissionRecord
                {
                    Id = permissionsListPermissionId,
                    Code = "Permissions.List",
                    Title = "列表",
                    Type = PermissionType.Page,
                    SortOrder = 101,
                    Icon = string.Empty,
                    VueComponentPath = "/src/views/permissions/list.vue",
                    RoutePath = "/permissions/list",
                    RouteName = "Permissions.List",
                    ParentId = permissionsPermissionId,
                    MetaData = new PermissionMetaData(true),
                    OrgId = organizationId,
                },
                new PermissionRecord
                {
                    Id = permissionsCreatePermissionId,
                    Code = "Permissions.Create",
                    Title = "新增权限",
                    Type = PermissionType.Button,
                    SortOrder = 102,
                    ParentId = permissionsListPermissionId,
                    MetaData = default,
                    OrgId = organizationId,
                },
                new PermissionRecord
                {
                    Id = permissionsUpdatePermissionId,
                    Code = "Permissions.Update",
                    Title = "修改权限",
                    Type = PermissionType.Button,
                    SortOrder = 103,
                    ParentId = permissionsListPermissionId,
                    MetaData = default,
                    OrgId = organizationId,
                },
                new PermissionRecord
                {
                    Id = permissionsDeletePermissionId,
                    Code = "Permissions.Delete",
                    Title = "删除权限",
                    Type = PermissionType.Button,
                    SortOrder = 104,
                    ParentId = permissionsListPermissionId,
                    MetaData = default,
                    OrgId = organizationId,
                },
            }
        )
        .ExecuteCommand();
    db.Ado.CommitTran();
    Console.WriteLine("测试机构、超级管理员和权限初始化完成。");
}
catch
{
    db.Ado.RollbackTran();
    throw;
}
