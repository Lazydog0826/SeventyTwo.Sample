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
    var organizationsPermissionId = Guid.CreateVersion7();
    var organizationsListPermissionId = Guid.CreateVersion7();
    var organizationsCreatePermissionId = Guid.CreateVersion7();
    var organizationsUpdatePermissionId = Guid.CreateVersion7();
    var organizationsDeletePermissionId = Guid.CreateVersion7();
    var dataDictionariesPermissionId = Guid.CreateVersion7();
    var dataDictionariesListPermissionId = Guid.CreateVersion7();
    var dataDictionariesCreatePermissionId = Guid.CreateVersion7();
    var dataDictionariesUpdatePermissionId = Guid.CreateVersion7();
    var dataDictionariesDeletePermissionId = Guid.CreateVersion7();
    var usersPermissionId = Guid.CreateVersion7();
    var usersListPermissionId = Guid.CreateVersion7();
    var usersCreatePermissionId = Guid.CreateVersion7();
    var usersUpdatePermissionId = Guid.CreateVersion7();
    var usersDeletePermissionId = Guid.CreateVersion7();
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
                Code = "home",
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
                    Code = "permissions",
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
                    Code = "permissionsList",
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
                    Code = "permissionsCreate",
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
                    Code = "permissionsUpdate",
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
                    Code = "permissionsDelete",
                    Title = "删除权限",
                    Type = PermissionType.Button,
                    SortOrder = 104,
                    ParentId = permissionsListPermissionId,
                    MetaData = default,
                    OrgId = organizationId,
                },
                new PermissionRecord
                {
                    Id = organizationsPermissionId,
                    Code = "organizations",
                    Title = "机构管理",
                    Type = PermissionType.Directory,
                    SortOrder = 200,
                    Icon = "Building2",
                    VueComponentPath = string.Empty,
                    RoutePath = string.Empty,
                    RouteName = string.Empty,
                    ParentId = null,
                    MetaData = new PermissionMetaData(true),
                    OrgId = organizationId,
                },
                new PermissionRecord
                {
                    Id = organizationsListPermissionId,
                    Code = "organizationsList",
                    Title = "列表",
                    Type = PermissionType.Page,
                    SortOrder = 201,
                    Icon = string.Empty,
                    VueComponentPath = "/src/views/organizations/list.vue",
                    RoutePath = "/organizations/list",
                    RouteName = "Organizations.List",
                    ParentId = organizationsPermissionId,
                    MetaData = new PermissionMetaData(true),
                    OrgId = organizationId,
                },
                new PermissionRecord
                {
                    Id = organizationsCreatePermissionId,
                    Code = "organizationsCreate",
                    Title = "新增机构",
                    Type = PermissionType.Button,
                    SortOrder = 202,
                    ParentId = organizationsListPermissionId,
                    MetaData = default,
                    OrgId = organizationId,
                },
                new PermissionRecord
                {
                    Id = organizationsUpdatePermissionId,
                    Code = "organizationsUpdate",
                    Title = "修改机构",
                    Type = PermissionType.Button,
                    SortOrder = 203,
                    ParentId = organizationsListPermissionId,
                    MetaData = default,
                    OrgId = organizationId,
                },
                new PermissionRecord
                {
                    Id = organizationsDeletePermissionId,
                    Code = "organizationsDelete",
                    Title = "删除机构",
                    Type = PermissionType.Button,
                    SortOrder = 204,
                    ParentId = organizationsListPermissionId,
                    MetaData = default,
                    OrgId = organizationId,
                },
                new PermissionRecord
                {
                    Id = dataDictionariesPermissionId,
                    Code = "dataDictionaries",
                    Title = "字典管理",
                    Type = PermissionType.Directory,
                    SortOrder = 300,
                    Icon = "BookOpen",
                    VueComponentPath = string.Empty,
                    RoutePath = string.Empty,
                    RouteName = string.Empty,
                    ParentId = null,
                    MetaData = new PermissionMetaData(true),
                    OrgId = organizationId,
                },
                new PermissionRecord
                {
                    Id = dataDictionariesListPermissionId,
                    Code = "dataDictionariesList",
                    Title = "列表",
                    Type = PermissionType.Page,
                    SortOrder = 301,
                    Icon = string.Empty,
                    VueComponentPath = "/src/views/dataDictionaries/list.vue",
                    RoutePath = "/dataDictionaries/list",
                    RouteName = "DataDictionaries.List",
                    ParentId = dataDictionariesPermissionId,
                    MetaData = new PermissionMetaData(true),
                    OrgId = organizationId,
                },
                new PermissionRecord
                {
                    Id = dataDictionariesCreatePermissionId,
                    Code = "dataDictionariesCreate",
                    Title = "新增字典",
                    Type = PermissionType.Button,
                    SortOrder = 302,
                    ParentId = dataDictionariesListPermissionId,
                    MetaData = default,
                    OrgId = organizationId,
                },
                new PermissionRecord
                {
                    Id = dataDictionariesUpdatePermissionId,
                    Code = "dataDictionariesUpdate",
                    Title = "修改字典",
                    Type = PermissionType.Button,
                    SortOrder = 303,
                    ParentId = dataDictionariesListPermissionId,
                    MetaData = default,
                    OrgId = organizationId,
                },
                new PermissionRecord
                {
                    Id = dataDictionariesDeletePermissionId,
                    Code = "dataDictionariesDelete",
                    Title = "删除字典",
                    Type = PermissionType.Button,
                    SortOrder = 304,
                    ParentId = dataDictionariesListPermissionId,
                    MetaData = default,
                    OrgId = organizationId,
                },
                new PermissionRecord
                {
                    Id = usersPermissionId,
                    Code = "users",
                    Title = "用户管理",
                    Type = PermissionType.Directory,
                    SortOrder = 400,
                    Icon = "Users",
                    VueComponentPath = string.Empty,
                    RoutePath = string.Empty,
                    RouteName = string.Empty,
                    ParentId = null,
                    MetaData = new PermissionMetaData(true),
                    OrgId = organizationId,
                },
                new PermissionRecord
                {
                    Id = usersListPermissionId,
                    Code = "usersList",
                    Title = "列表",
                    Type = PermissionType.Page,
                    SortOrder = 401,
                    Icon = string.Empty,
                    VueComponentPath = "/src/views/users/list.vue",
                    RoutePath = "/users/list",
                    RouteName = "Users.List",
                    ParentId = usersPermissionId,
                    MetaData = new PermissionMetaData(true),
                    OrgId = organizationId,
                },
                new PermissionRecord
                {
                    Id = usersCreatePermissionId,
                    Code = "usersCreate",
                    Title = "新增用户",
                    Type = PermissionType.Button,
                    SortOrder = 402,
                    ParentId = usersListPermissionId,
                    MetaData = default,
                    OrgId = organizationId,
                },
                new PermissionRecord
                {
                    Id = usersUpdatePermissionId,
                    Code = "usersUpdate",
                    Title = "修改用户",
                    Type = PermissionType.Button,
                    SortOrder = 403,
                    ParentId = usersListPermissionId,
                    MetaData = default,
                    OrgId = organizationId,
                },
                new PermissionRecord
                {
                    Id = usersDeletePermissionId,
                    Code = "usersDelete",
                    Title = "删除用户",
                    Type = PermissionType.Button,
                    SortOrder = 404,
                    ParentId = usersListPermissionId,
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
