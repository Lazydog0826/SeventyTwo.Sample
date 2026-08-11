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

        /// <summary>机构成员 ID 不能为空。</summary>
        public const string MemberIdRequired = "organization.memberIdRequired";
    }
}
