using SeventyTwo.Sample.Domain.Permissions;
using SeventyTwo.Sample.Infrastructure.Permissions;
using SqlSugar;

namespace SeventyTwo.Sample.DataSetup.Seeders;

// 权限种子：首页与各管理模块的目录、页面、按钮权限树。
internal static class PermissionSeeder
{
    // 返回首页权限 Id，供用户种子的默认页面与用户权限关联使用。
    public static Guid Seed(SqlSugarClient db)
    {
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
        var productsPermissionId = Guid.CreateVersion7();
        var productCategoriesPermissionId = Guid.CreateVersion7();
        var productCategoriesCreatePermissionId = Guid.CreateVersion7();
        var productCategoriesUpdatePermissionId = Guid.CreateVersion7();
        var productCategoriesDeletePermissionId = Guid.CreateVersion7();
        var productsListPermissionId = Guid.CreateVersion7();
        var productsDeletePermissionId = Guid.CreateVersion7();
        var productsEditPermissionId = Guid.CreateVersion7();
        var productsCreatePermissionId = Guid.CreateVersion7();
        var productsUpdatePermissionId = Guid.CreateVersion7();

        db.Insertable(
                new[]
                {
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
                    },
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
                        Title = "权限列表",
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
                        Path =
                            $"{permissionsPermissionId}/{permissionsListPermissionId}/{permissionsCreatePermissionId}",
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
                        Path =
                            $"{permissionsPermissionId}/{permissionsListPermissionId}/{permissionsUpdatePermissionId}",
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
                        Path =
                            $"{permissionsPermissionId}/{permissionsListPermissionId}/{permissionsDeletePermissionId}",
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
                        Title = "机构列表",
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
                        Title = "字典列表",
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
                        Title = "用户列表",
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
                    new PermissionRecord
                    {
                        Id = productsPermissionId,
                        Code = "products",
                        Title = "商品管理",
                        Type = PermissionType.Directory,
                        SortOrder = 500,
                        Icon = "ShoppingBag",
                        VueComponentPath = string.Empty,
                        RoutePath = string.Empty,
                        RouteName = string.Empty,
                        ParentId = null,
                        Path = productsPermissionId.ToString(),
                        MetaData = new PermissionMetaData(true),
                        OrgId = Guid.Empty,
                    },
                    new PermissionRecord
                    {
                        Id = productCategoriesPermissionId,
                        Code = "productCategories",
                        Title = "商品类目",
                        Type = PermissionType.Page,
                        SortOrder = 501,
                        Icon = string.Empty,
                        VueComponentPath = "/src/views/productCategories/list.vue",
                        RoutePath = "/productCategories/list",
                        RouteName = "ProductCategories.List",
                        ParentId = productsPermissionId,
                        Path = $"{productsPermissionId}/{productCategoriesPermissionId}",
                        MetaData = new PermissionMetaData(true),
                        OrgId = Guid.Empty,
                    },
                    new PermissionRecord
                    {
                        Id = productCategoriesCreatePermissionId,
                        Code = "productCategoriesCreate",
                        Title = "新增商品类目",
                        Type = PermissionType.Button,
                        SortOrder = 502,
                        ParentId = productCategoriesPermissionId,
                        Path =
                            $"{productsPermissionId}/{productCategoriesPermissionId}/{productCategoriesCreatePermissionId}",
                        MetaData = default,
                        OrgId = Guid.Empty,
                    },
                    new PermissionRecord
                    {
                        Id = productCategoriesUpdatePermissionId,
                        Code = "productCategoriesUpdate",
                        Title = "修改商品类目",
                        Type = PermissionType.Button,
                        SortOrder = 503,
                        ParentId = productCategoriesPermissionId,
                        Path =
                            $"{productsPermissionId}/{productCategoriesPermissionId}/{productCategoriesUpdatePermissionId}",
                        MetaData = default,
                        OrgId = Guid.Empty,
                    },
                    new PermissionRecord
                    {
                        Id = productCategoriesDeletePermissionId,
                        Code = "productCategoriesDelete",
                        Title = "删除商品类目",
                        Type = PermissionType.Button,
                        SortOrder = 504,
                        ParentId = productCategoriesPermissionId,
                        Path =
                            $"{productsPermissionId}/{productCategoriesPermissionId}/{productCategoriesDeletePermissionId}",
                        MetaData = default,
                        OrgId = Guid.Empty,
                    },
                    new PermissionRecord
                    {
                        Id = productsListPermissionId,
                        Code = "productsList",
                        Title = "商品列表",
                        Type = PermissionType.Page,
                        SortOrder = 505,
                        Icon = string.Empty,
                        VueComponentPath = "/src/views/products/list.vue",
                        RoutePath = "/products/list",
                        RouteName = "Products.List",
                        ParentId = productsPermissionId,
                        Path = $"{productsPermissionId}/{productsListPermissionId}",
                        MetaData = new PermissionMetaData(true),
                        OrgId = Guid.Empty,
                    },
                    new PermissionRecord
                    {
                        Id = productsDeletePermissionId,
                        Code = "productsDelete",
                        Title = "删除商品",
                        Type = PermissionType.Button,
                        SortOrder = 506,
                        ParentId = productsListPermissionId,
                        Path = $"{productsPermissionId}/{productsListPermissionId}/{productsDeletePermissionId}",
                        MetaData = default,
                        OrgId = Guid.Empty,
                    },
                    // 商品编辑页：独立的新增/编辑页面，不在侧边菜单展示（IsShow=false），仅注册前端路由。
                    new PermissionRecord
                    {
                        Id = productsEditPermissionId,
                        Code = "productsEdit",
                        Title = "商品编辑",
                        Type = PermissionType.Page,
                        SortOrder = 507,
                        Icon = string.Empty,
                        VueComponentPath = "/src/views/products/edit.vue",
                        RoutePath = "/products/edit",
                        RouteName = "Products.Edit",
                        ParentId = productsPermissionId,
                        Path = $"{productsPermissionId}/{productsEditPermissionId}",
                        MetaData = new PermissionMetaData(false),
                        OrgId = Guid.Empty,
                    },
                    // 新增/修改按钮挂在编辑页下：授权按钮时祖先连带页面权限，避免出现有按钮权限却无页面路由。
                    new PermissionRecord
                    {
                        Id = productsCreatePermissionId,
                        Code = "productsCreate",
                        Title = "新增商品",
                        Type = PermissionType.Button,
                        SortOrder = 508,
                        ParentId = productsEditPermissionId,
                        Path = $"{productsPermissionId}/{productsEditPermissionId}/{productsCreatePermissionId}",
                        MetaData = default,
                        OrgId = Guid.Empty,
                    },
                    new PermissionRecord
                    {
                        Id = productsUpdatePermissionId,
                        Code = "productsUpdate",
                        Title = "修改商品",
                        Type = PermissionType.Button,
                        SortOrder = 509,
                        ParentId = productsEditPermissionId,
                        Path = $"{productsPermissionId}/{productsEditPermissionId}/{productsUpdatePermissionId}",
                        MetaData = default,
                        OrgId = Guid.Empty,
                    },
                }
            )
            .ExecuteCommand();

        return homePermissionId;
    }
}
