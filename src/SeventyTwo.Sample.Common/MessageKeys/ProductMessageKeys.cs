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

    /// <summary>
    /// 商品类目消息键。
    /// </summary>
    public static class ProductCategories
    {
        /// <summary>
        /// 商品类目 ID 不能为空。
        /// </summary>
        public const string IdRequired = "productCategory.idRequired";

        /// <summary>
        /// 商品类目不存在。
        /// </summary>
        public const string NotFound = "productCategory.notFound";

        /// <summary>
        /// 上级类目不存在。
        /// </summary>
        public const string ParentNotFound = "productCategory.parentNotFound";

        /// <summary>
        /// 商品类目的后代不能作为其上级类目。
        /// </summary>
        public const string DescendantCannotBeParent = "productCategory.descendantCannotBeParent";

        /// <summary>
        /// 商品类目存在下级类目，不能删除。
        /// </summary>
        public const string HasChildren = "productCategory.hasChildren";

        /// <summary>
        /// 商品类目数据已变更，需要刷新后重试。
        /// </summary>
        public const string DataChanged = "productCategory.dataChanged";

        /// <summary>
        /// 商品类目修改时间不能为空。
        /// </summary>
        public const string ModifiedAtRequired = "productCategory.modifiedAtRequired";

        /// <summary>
        /// 商品类目删除时间不能为空。
        /// </summary>
        public const string DeletedAtRequired = "productCategory.deletedAtRequired";

        /// <summary>
        /// 商品类目名称不能为空。
        /// </summary>
        public const string NameRequired = "productCategory.nameRequired";

        /// <summary>
        /// 商品类目名称长度超出限制。
        /// </summary>
        public const string NameTooLong = "productCategory.nameTooLong";

        /// <summary>
        /// 商品类目上级 ID 不能为空。
        /// </summary>
        public const string ParentIdRequired = "productCategory.parentIdRequired";

        /// <summary>
        /// 商品类目不能作为自身的上级类目。
        /// </summary>
        public const string SelfCannotBeParent = "productCategory.selfCannotBeParent";

        /// <summary>
        /// 商品类目排序号不能小于零。
        /// </summary>
        public const string SortMustNotBeNegative = "productCategory.sortMustNotBeNegative";
    }
}
