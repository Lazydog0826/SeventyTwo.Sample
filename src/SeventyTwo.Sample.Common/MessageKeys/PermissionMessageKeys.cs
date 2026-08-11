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
    }
}
