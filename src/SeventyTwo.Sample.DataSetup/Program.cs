using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using SeventyTwo.Sample.Domain.Permissions;
using SeventyTwo.Sample.Domain.Users;
using SeventyTwo.Sample.Infrastructure;
using SeventyTwo.Sample.Infrastructure.DataDictionaries;
using SeventyTwo.Sample.Infrastructure.Permissions;
using SeventyTwo.Sample.Infrastructure.Users;
using SqlSugar;

const string displayName = "超级管理员";
const string initialPassword = "123456";
const string phone = "00000000000";
const string email = "superadmin@localhost";

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();
var connectionString = configuration.GetConnectionString("PostgreSQL");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("未配置 ConnectionStrings:PostgreSQL。");
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

    var userId = Guid.CreateVersion7();
    var dataPermissionTypeDictionaryId = Guid.CreateVersion7();
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
    var usersAuthorizePermissionId = Guid.CreateVersion7();
    var usersResetPasswordPermissionId = Guid.CreateVersion7();
    var passwordHash = new PasswordHasher<string>().HashPassword(SystemUsernames.SuperAdmin, initialPassword);

    db.Insertable(
            new UserAccountRecord
            {
                Id = userId,
                Username = SystemUsernames.SuperAdmin,
                PasswordHash = passwordHash,
                DisplayName = displayName,
                Phone = phone,
                Email = email,
                DataPermissionType = DataPermissionType.All,
                DefaultPageId = homePermissionId,
                OrgId = Guid.Empty,
            }
        )
        .ExecuteCommand();
    db.Insertable(
            new DataDictionaryRecord
            {
                Id = dataPermissionTypeDictionaryId,
                Code = "DATA_PERMISSION_TYPE",
                Name = "数据权限类型",
                Description = "用户可访问的数据范围类型",
                Enable = true,
                OrgId = Guid.Empty,
            }
        )
        .ExecuteCommand();
    db.Insertable(
            new[]
            {
                new DataDictionaryItemRecord
                {
                    Id = Guid.CreateVersion7(),
                    DictionaryId = dataPermissionTypeDictionaryId,
                    Value = ((short)DataPermissionType.All).ToString(),
                    Label = "全部数据",
                    SortOrder = 1,
                },
                new DataDictionaryItemRecord
                {
                    Id = Guid.CreateVersion7(),
                    DictionaryId = dataPermissionTypeDictionaryId,
                    Value = ((short)DataPermissionType.Organization).ToString(),
                    Label = "本机构数据",
                    SortOrder = 2,
                },
                new DataDictionaryItemRecord
                {
                    Id = Guid.CreateVersion7(),
                    DictionaryId = dataPermissionTypeDictionaryId,
                    Value = ((short)DataPermissionType.OrganizationAndDescendants).ToString(),
                    Label = "本机构与下级机构数据",
                    SortOrder = 3,
                },
                new DataDictionaryItemRecord
                {
                    Id = Guid.CreateVersion7(),
                    DictionaryId = dataPermissionTypeDictionaryId,
                    Value = ((short)DataPermissionType.Self).ToString(),
                    Label = "自己的数据",
                    SortOrder = 4,
                },
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
                Path = homePermissionId.ToString(),
                MetaData = new PermissionMetaData(true),
                OrgId = Guid.Empty,
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
                    Path = permissionsPermissionId.ToString(),
                    MetaData = new PermissionMetaData(true),
                    OrgId = Guid.Empty,
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
                    Path = $"{permissionsPermissionId}/{permissionsListPermissionId}",
                    MetaData = new PermissionMetaData(true),
                    OrgId = Guid.Empty,
                },
                new PermissionRecord
                {
                    Id = permissionsCreatePermissionId,
                    Code = "permissionsCreate",
                    Title = "新增权限",
                    Type = PermissionType.Button,
                    SortOrder = 102,
                    ParentId = permissionsListPermissionId,
                    Path = $"{permissionsPermissionId}/{permissionsListPermissionId}/{permissionsCreatePermissionId}",
                    MetaData = default,
                    OrgId = Guid.Empty,
                },
                new PermissionRecord
                {
                    Id = permissionsUpdatePermissionId,
                    Code = "permissionsUpdate",
                    Title = "修改权限",
                    Type = PermissionType.Button,
                    SortOrder = 103,
                    ParentId = permissionsListPermissionId,
                    Path = $"{permissionsPermissionId}/{permissionsListPermissionId}/{permissionsUpdatePermissionId}",
                    MetaData = default,
                    OrgId = Guid.Empty,
                },
                new PermissionRecord
                {
                    Id = permissionsDeletePermissionId,
                    Code = "permissionsDelete",
                    Title = "删除权限",
                    Type = PermissionType.Button,
                    SortOrder = 104,
                    ParentId = permissionsListPermissionId,
                    Path = $"{permissionsPermissionId}/{permissionsListPermissionId}/{permissionsDeletePermissionId}",
                    MetaData = default,
                    OrgId = Guid.Empty,
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
                    Path = organizationsPermissionId.ToString(),
                    MetaData = new PermissionMetaData(true),
                    OrgId = Guid.Empty,
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
                    Path = $"{organizationsPermissionId}/{organizationsListPermissionId}",
                    MetaData = new PermissionMetaData(true),
                    OrgId = Guid.Empty,
                },
                new PermissionRecord
                {
                    Id = organizationsCreatePermissionId,
                    Code = "organizationsCreate",
                    Title = "新增机构",
                    Type = PermissionType.Button,
                    SortOrder = 202,
                    ParentId = organizationsListPermissionId,
                    Path =
                        $"{organizationsPermissionId}/{organizationsListPermissionId}/{organizationsCreatePermissionId}",
                    MetaData = default,
                    OrgId = Guid.Empty,
                },
                new PermissionRecord
                {
                    Id = organizationsUpdatePermissionId,
                    Code = "organizationsUpdate",
                    Title = "修改机构",
                    Type = PermissionType.Button,
                    SortOrder = 203,
                    ParentId = organizationsListPermissionId,
                    Path =
                        $"{organizationsPermissionId}/{organizationsListPermissionId}/{organizationsUpdatePermissionId}",
                    MetaData = default,
                    OrgId = Guid.Empty,
                },
                new PermissionRecord
                {
                    Id = organizationsDeletePermissionId,
                    Code = "organizationsDelete",
                    Title = "删除机构",
                    Type = PermissionType.Button,
                    SortOrder = 204,
                    ParentId = organizationsListPermissionId,
                    Path =
                        $"{organizationsPermissionId}/{organizationsListPermissionId}/{organizationsDeletePermissionId}",
                    MetaData = default,
                    OrgId = Guid.Empty,
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
                    Path = dataDictionariesPermissionId.ToString(),
                    MetaData = new PermissionMetaData(true),
                    OrgId = Guid.Empty,
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
                    Path = $"{dataDictionariesPermissionId}/{dataDictionariesListPermissionId}",
                    MetaData = new PermissionMetaData(true),
                    OrgId = Guid.Empty,
                },
                new PermissionRecord
                {
                    Id = dataDictionariesCreatePermissionId,
                    Code = "dataDictionariesCreate",
                    Title = "新增字典",
                    Type = PermissionType.Button,
                    SortOrder = 302,
                    ParentId = dataDictionariesListPermissionId,
                    Path =
                        $"{dataDictionariesPermissionId}/{dataDictionariesListPermissionId}/{dataDictionariesCreatePermissionId}",
                    MetaData = default,
                    OrgId = Guid.Empty,
                },
                new PermissionRecord
                {
                    Id = dataDictionariesUpdatePermissionId,
                    Code = "dataDictionariesUpdate",
                    Title = "修改字典",
                    Type = PermissionType.Button,
                    SortOrder = 303,
                    ParentId = dataDictionariesListPermissionId,
                    Path =
                        $"{dataDictionariesPermissionId}/{dataDictionariesListPermissionId}/{dataDictionariesUpdatePermissionId}",
                    MetaData = default,
                    OrgId = Guid.Empty,
                },
                new PermissionRecord
                {
                    Id = dataDictionariesDeletePermissionId,
                    Code = "dataDictionariesDelete",
                    Title = "删除字典",
                    Type = PermissionType.Button,
                    SortOrder = 304,
                    ParentId = dataDictionariesListPermissionId,
                    Path =
                        $"{dataDictionariesPermissionId}/{dataDictionariesListPermissionId}/{dataDictionariesDeletePermissionId}",
                    MetaData = default,
                    OrgId = Guid.Empty,
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
                    Path = usersPermissionId.ToString(),
                    MetaData = new PermissionMetaData(true),
                    OrgId = Guid.Empty,
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
                    Path = $"{usersPermissionId}/{usersListPermissionId}",
                    MetaData = new PermissionMetaData(true),
                    OrgId = Guid.Empty,
                },
                new PermissionRecord
                {
                    Id = usersCreatePermissionId,
                    Code = "usersCreate",
                    Title = "新增用户",
                    Type = PermissionType.Button,
                    SortOrder = 402,
                    ParentId = usersListPermissionId,
                    Path = $"{usersPermissionId}/{usersListPermissionId}/{usersCreatePermissionId}",
                    MetaData = default,
                    OrgId = Guid.Empty,
                },
                new PermissionRecord
                {
                    Id = usersUpdatePermissionId,
                    Code = "usersUpdate",
                    Title = "修改用户",
                    Type = PermissionType.Button,
                    SortOrder = 403,
                    ParentId = usersListPermissionId,
                    Path = $"{usersPermissionId}/{usersListPermissionId}/{usersUpdatePermissionId}",
                    MetaData = default,
                    OrgId = Guid.Empty,
                },
                new PermissionRecord
                {
                    Id = usersDeletePermissionId,
                    Code = "usersDelete",
                    Title = "删除用户",
                    Type = PermissionType.Button,
                    SortOrder = 404,
                    ParentId = usersListPermissionId,
                    Path = $"{usersPermissionId}/{usersListPermissionId}/{usersDeletePermissionId}",
                    MetaData = default,
                    OrgId = Guid.Empty,
                },
                new PermissionRecord
                {
                    Id = usersAuthorizePermissionId,
                    Code = "usersAuthorize",
                    Title = "用户授权",
                    Type = PermissionType.Button,
                    SortOrder = 405,
                    ParentId = usersListPermissionId,
                    Path = $"{usersPermissionId}/{usersListPermissionId}/{usersAuthorizePermissionId}",
                    MetaData = default,
                    OrgId = Guid.Empty,
                },
                new PermissionRecord
                {
                    Id = usersResetPasswordPermissionId,
                    Code = "usersResetPassword",
                    Title = "重置用户密码",
                    Type = PermissionType.Button,
                    SortOrder = 406,
                    ParentId = usersListPermissionId,
                    Path = $"{usersPermissionId}/{usersListPermissionId}/{usersResetPasswordPermissionId}",
                    MetaData = default,
                    OrgId = Guid.Empty,
                },
            }
        )
        .ExecuteCommand();
    db.Ado.CommitTran();
    Console.WriteLine("超级管理员和权限初始化完成。");
}
catch
{
    db.Ado.RollbackTran();
    throw;
}
