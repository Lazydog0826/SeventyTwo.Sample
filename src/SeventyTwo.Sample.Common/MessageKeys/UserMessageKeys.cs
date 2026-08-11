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
    }
}
