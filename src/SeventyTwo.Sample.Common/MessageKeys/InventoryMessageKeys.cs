namespace SeventyTwo.Sample.Common.MessageKeys;

public static partial class MessageKeys
{
    public static class Inventories
    {
        /// <summary>库存 ID 不能为空。</summary>
        public const string IdRequired = "inventory.idRequired";

        /// <summary>库存业务请求号不能为空。</summary>
        public const string BusinessRequestNoRequired = "inventory.businessRequestNoRequired";

        /// <summary>仓库 ID 不能为空。</summary>
        public const string WarehouseIdRequired = "inventory.warehouseIdRequired";

        /// <summary>货位 ID 不能为空。</summary>
        public const string LocationIdRequired = "inventory.locationIdRequired";

        /// <summary>库存变更数量必须大于零。</summary>
        public const string ChangeQuantityMustBePositive = "inventory.changeQuantityMustBePositive";

        /// <summary>入库批次号不能为空。</summary>
        public const string InboundBatchNoRequired = "inventory.inboundBatchNoRequired";

        /// <summary>入库批次号长度超出限制。</summary>
        public const string InboundBatchNoTooLong = "inventory.inboundBatchNoTooLong";

        /// <summary>入库时间不能为空。</summary>
        public const string InboundAtRequired = "inventory.inboundAtRequired";

        /// <summary>库存变更时间不能为空。</summary>
        public const string ChangedAtRequired = "inventory.changedAtRequired";

        /// <summary>初始库存数量不能小于零。</summary>
        public const string InitialQuantityMustNotBeNegative = "inventory.initialQuantityMustNotBeNegative";

        /// <summary>库存数量不能小于零。</summary>
        public const string QuantityMustNotBeNegative = "inventory.quantityMustNotBeNegative";

        /// <summary>库存数量超出允许范围。</summary>
        public const string QuantityOutOfRange = "inventory.quantityOutOfRange";

        /// <summary>库存不足。</summary>
        public const string Insufficient = "inventory.insufficient";
    }
}
