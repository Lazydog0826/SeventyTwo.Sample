namespace SeventyTwo.Sample.Common.MessageKeys;

public static partial class MessageKeys
{
    public static class Products
    {
        /// <summary>
        /// 商品 ID 不能为空。
        /// </summary>
        public const string IdRequired = "product.idRequired";

        /// <summary>
        /// 商品不存在。
        /// </summary>
        public const string NotFound = "product.notFound";

        /// <summary>
        /// 商品数据已变更，需要刷新后重试。
        /// </summary>
        public const string DataChanged = "product.dataChanged";

        /// <summary>
        /// 商品修改时间不能为空。
        /// </summary>
        public const string ModifiedAtRequired = "product.modifiedAtRequired";

        /// <summary>
        /// 商品删除时间不能为空。
        /// </summary>
        public const string DeletedAtRequired = "product.deletedAtRequired";

        /// <summary>
        /// 商品名称不能为空。
        /// </summary>
        public const string NameRequired = "product.nameRequired";

        /// <summary>
        /// 商品名称长度超出限制。
        /// </summary>
        public const string NameTooLong = "product.nameTooLong";

        /// <summary>
        /// 商品价格必须大于零。
        /// </summary>
        public const string PriceMustBePositive = "product.priceMustBePositive";

        /// <summary>
        /// 商品价格超出允许范围。
        /// </summary>
        public const string PriceOutOfRange = "product.priceOutOfRange";

        /// <summary>
        /// 商品价格的小数位数不符合要求。
        /// </summary>
        public const string PriceScaleInvalid = "product.priceScaleInvalid";
    }
}
