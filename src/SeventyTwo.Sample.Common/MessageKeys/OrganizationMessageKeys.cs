namespace SeventyTwo.Sample.Common.MessageKeys;

public static partial class MessageKeys
{
    public static class Organizations
    {
        /// <summary>机构 ID 不能为空。</summary>
        public const string IdRequired = "organization.idRequired";

        /// <summary>上级机构 ID 不能为空。</summary>
        public const string ParentIdRequired = "organization.parentIdRequired";

        /// <summary>机构不能以自身作为上级机构。</summary>
        public const string SelfCannotBeParent = "organization.selfCannotBeParent";

        /// <summary>机构编码不能为空。</summary>
        public const string CodeRequired = "organization.codeRequired";

        /// <summary>机构名称不能为空。</summary>
        public const string NameRequired = "organization.nameRequired";

        /// <summary>机构编码已存在。</summary>
        public const string CodeExists = "organization.codeExists";

        /// <summary>机构不存在。</summary>
        public const string NotFound = "organization.notFound";

        /// <summary>上级机构不存在。</summary>
        public const string ParentNotFound = "organization.parentNotFound";

        /// <summary>机构层级存在循环引用。</summary>
        public const string HierarchyCycle = "organization.hierarchyCycle";

        /// <summary>机构不能将自身或下级机构设为上级机构。</summary>
        public const string DescendantCannotBeParent = "organization.descendantCannotBeParent";

        /// <summary>根机构不能变更为子机构。</summary>
        public const string RootCannotBeChild = "organization.rootCannotBeChild";

        /// <summary>子机构不能变更为根机构。</summary>
        public const string ChildCannotBeRoot = "organization.childCannotBeRoot";

        /// <summary>机构不能跨根机构移动。</summary>
        public const string CrossRootMoveNotAllowed = "organization.crossRootMoveNotAllowed";

        /// <summary>机构存在下级机构，无法删除。</summary>
        public const string HasChildren = "organization.hasChildren";

        /// <summary>机构数据已变更，需要刷新后重试。</summary>
        public const string DataChanged = "organization.dataChanged";

        /// <summary>机构修改时间不能为空。</summary>
        public const string ModifiedAtRequired = "organization.modifiedAtRequired";
    }
}
