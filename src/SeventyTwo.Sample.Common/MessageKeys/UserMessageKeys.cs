namespace SeventyTwo.Sample.Common.MessageKeys;

public static partial class MessageKeys
{
    public static class Users
    {
        /// <summary>用户 ID 不能为空。</summary>
        public const string IdRequired = "user.idRequired";

        /// <summary>用户不存在。</summary>
        public const string NotFound = "user.notFound";

        /// <summary>用户名不能为空。</summary>
        public const string UsernameRequired = "user.usernameRequired";

        /// <summary>密码摘要不能为空。</summary>
        public const string PasswordHashRequired = "user.passwordHashRequired";

        /// <summary>用户显示名称不能为空。</summary>
        public const string DisplayNameRequired = "user.displayNameRequired";

        /// <summary>账号或密码错误。</summary>
        public const string CredentialsInvalid = "user.credentialsInvalid";

        /// <summary>用户已禁用。</summary>
        public const string Disabled = "user.disabled";

        /// <summary>手机号码不能为空。</summary>
        public const string PhoneRequired = "user.phoneRequired";

        /// <summary>电子邮箱不能为空。</summary>
        public const string EmailRequired = "user.emailRequired";

        /// <summary>用户所属机构不能为空。</summary>
        public const string OrgIdRequired = "user.orgIdRequired";

        /// <summary>用户所属机构不存在。</summary>
        public const string OrganizationNotFound = "user.organizationNotFound";

        /// <summary>用户所属机构已禁用。</summary>
        public const string OrganizationDisabled = "user.organizationDisabled";

        /// <summary>用户名已存在。</summary>
        public const string UsernameExists = "user.usernameExists";

        /// <summary>用户名为系统保留账号。</summary>
        public const string UsernameReserved = "user.usernameReserved";

        /// <summary>用户数据已变更，需要刷新后重试。</summary>
        public const string DataChanged = "user.dataChanged";

        /// <summary>用户修改时间不能为空。</summary>
        public const string ModifiedAtRequired = "user.modifiedAtRequired";

        /// <summary>用户已关联权限，无法删除。</summary>
        public const string HasPermissions = "user.hasPermissions";

        /// <summary>超级管理员不允许执行该操作。</summary>
        public const string SuperAdminProtected = "user.superAdminProtected";
    }
}
