namespace SeventyTwo.Sample.Common.MessageKeys;

public static partial class MessageKeys
{
    public static class Validation
    {
        /// <summary>
        /// 请求参数校验失败。
        /// </summary>
        public const string Failed = "validation.failed";

        /// <summary>
        /// 账号不能为空。
        /// </summary>
        public const string AccountRequired = "validation.accountRequired";

        /// <summary>
        /// 账号长度不符合要求。
        /// </summary>
        public const string AccountLengthInvalid = "validation.accountLengthInvalid";

        /// <summary>
        /// 密码不能为空。
        /// </summary>
        public const string PasswordRequired = "validation.passwordRequired";

        /// <summary>
        /// 密码长度不符合要求。
        /// </summary>
        public const string PasswordLengthInvalid = "validation.passwordLengthInvalid";
    }
}
