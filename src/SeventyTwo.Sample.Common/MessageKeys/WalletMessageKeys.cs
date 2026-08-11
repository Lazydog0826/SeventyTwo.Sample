namespace SeventyTwo.Sample.Common.MessageKeys;

public static partial class MessageKeys
{
    public static class Wallets
    {
        /// <summary>钱包 ID 不能为空。</summary>
        public const string IdRequired = "wallet.idRequired";

        /// <summary>客户 ID 不能为空。</summary>
        public const string CustomerIdRequired = "wallet.customerIdRequired";

        /// <summary>钱包类型无效。</summary>
        public const string TypeInvalid = "wallet.typeInvalid";

        /// <summary>钱包变更类型无效。</summary>
        public const string ChangeTypeInvalid = "wallet.changeTypeInvalid";

        /// <summary>钱包余额变更明细不能为空。</summary>
        public const string ChangeItemsRequired = "wallet.changeItemsRequired";

        /// <summary>钱包业务请求号不能为空。</summary>
        public const string RequestNoRequired = "wallet.requestNoRequired";

        /// <summary>钱包不属于当前客户。</summary>
        public const string NotOwnedByCustomer = "wallet.notOwnedByCustomer";

        /// <summary>客户存在重复的钱包类型。</summary>
        public const string DuplicateTypeForCustomer = "wallet.duplicateTypeForCustomer";

        /// <summary>余额变更金额必须大于零。</summary>
        public const string ChangeAmountMustBePositive = "wallet.changeAmountMustBePositive";

        /// <summary>金额不能小于零。</summary>
        public const string AmountMustNotBeNegative = "wallet.amountMustNotBeNegative";

        /// <summary>金额超出允许范围。</summary>
        public const string AmountOutOfRange = "wallet.amountOutOfRange";

        /// <summary>金额的小数位数不符合要求。</summary>
        public const string AmountScaleInvalid = "wallet.amountScaleInvalid";

        /// <summary>钱包余额不足。</summary>
        public const string InsufficientBalance = "wallet.insufficientBalance";
    }
}
