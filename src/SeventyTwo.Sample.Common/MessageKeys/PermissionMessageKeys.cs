namespace SeventyTwo.Sample.Common.MessageKeys;

public static partial class MessageKeys
{
    public static class Permissions
    {
        /// <summary>权限 ID 不能为空。</summary>
        public const string IdRequired = "permission.idRequired";

        /// <summary>用户权限关联 ID 不能为空。</summary>
        public const string UserPermissionIdRequired = "permission.userPermissionIdRequired";

        /// <summary>权限编码不能为空。</summary>
        public const string CodeRequired = "permission.codeRequired";

        /// <summary>权限编码已存在。</summary>
        public const string CodeExists = "permission.codeExists";

        /// <summary>权限不存在。</summary>
        public const string NotFound = "permission.notFound";

        /// <summary>上级权限不存在。</summary>
        public const string ParentNotFound = "permission.parentNotFound";

        /// <summary>权限层级存在循环引用。</summary>
        public const string HierarchyCycle = "permission.hierarchyCycle";

        /// <summary>权限不能将自身或下级权限设为上级权限。</summary>
        public const string DescendantCannotBeParent = "permission.descendantCannotBeParent";

        /// <summary>权限存在下级权限，无法删除。</summary>
        public const string HasChildren = "permission.hasChildren";

        /// <summary>权限不能以自身作为上级权限。</summary>
        public const string SelfCannotBeParent = "permission.selfCannotBeParent";

        /// <summary>上级权限 ID 不能为空。</summary>
        public const string ParentIdRequired = "permission.parentIdRequired";

        /// <summary>按钮的上级权限不能为空。</summary>
        public const string ButtonParentRequired = "permission.buttonParentRequired";

        /// <summary>权限类型无效。</summary>
        public const string TypeInvalid = "permission.typeInvalid";

        /// <summary>权限排序号不能小于零。</summary>
        public const string SortMustNotBeNegative = "permission.sortMustNotBeNegative";

        /// <summary>权限数据已变更，需要刷新后重试。</summary>
        public const string DataChanged = "permission.dataChanged";

        /// <summary>权限修改时间不能为空。</summary>
        public const string ModifiedAtRequired = "permission.modifiedAtRequired";

        /// <summary>权限标题不能为空。</summary>
        public const string TitleRequired = "permission.titleRequired";

        /// <summary>目录图标不能为空。</summary>
        public const string DirectoryIconRequired = "permission.directoryIconRequired";

        /// <summary>页面 Vue 组件路径不能为空。</summary>
        public const string VueComponentPathRequired = "permission.vueComponentPathRequired";

        /// <summary>页面路由路径不能为空。</summary>
        public const string RoutePathRequired = "permission.routePathRequired";

        /// <summary>页面路由名称不能为空。</summary>
        public const string RouteNameRequired = "permission.routeNameRequired";

        /// <summary>路由元数据不能为空。</summary>
        public const string RouteMetadataRequired = "permission.routeMetadataRequired";

        /// <summary>用户授权包含重复或不存在的权限。</summary>
        public const string AuthorizationInvalid = "permission.authorizationInvalid";

        /// <summary>用户授权未包含所选权限的完整祖先链。</summary>
        public const string AuthorizationHierarchyInvalid = "permission.authorizationHierarchyInvalid";

        /// <summary>禁止修改超级管理员授权。</summary>
        public const string SuperAdminAuthorizationForbidden = "permission.superAdminAuthorizationForbidden";
    }
}
