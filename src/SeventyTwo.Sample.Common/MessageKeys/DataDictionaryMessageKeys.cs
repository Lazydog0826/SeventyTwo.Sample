namespace SeventyTwo.Sample.Common.MessageKeys;

public static partial class MessageKeys
{
    public static class DataDictionaries
    {
        /// <summary>数据字典 ID 不能为空。</summary>
        public const string IdRequired = "dataDictionary.idRequired";

        /// <summary>数据字典编码不能为空。</summary>
        public const string CodeRequired = "dataDictionary.codeRequired";

        /// <summary>数据字典名称不能为空。</summary>
        public const string NameRequired = "dataDictionary.nameRequired";

        /// <summary>数据字典项 ID 不能为空。</summary>
        public const string ItemIdRequired = "dataDictionary.itemIdRequired";

        /// <summary>数据字典项排序号不能小于零。</summary>
        public const string ItemSortMustNotBeNegative = "dataDictionary.itemSortMustNotBeNegative";

        /// <summary>数据字典项值不能为空。</summary>
        public const string ItemValueRequired = "dataDictionary.itemValueRequired";

        /// <summary>数据字典项显示文本不能为空。</summary>
        public const string ItemLabelRequired = "dataDictionary.itemLabelRequired";

        /// <summary>数据字典编码已存在。</summary>
        public const string CodeExists = "dataDictionary.codeExists";

        /// <summary>数据字典不存在。</summary>
        public const string NotFound = "dataDictionary.notFound";

        /// <summary>数据字典项不存在。</summary>
        public const string ItemNotFound = "dataDictionary.itemNotFound";

        /// <summary>数据字典项值已存在。</summary>
        public const string ItemValueExists = "dataDictionary.itemValueExists";

        /// <summary>数据字典数据已变更。</summary>
        public const string DataChanged = "dataDictionary.dataChanged";

        /// <summary>数据字典修改时间不能为空。</summary>
        public const string ModifiedAtRequired = "dataDictionary.modifiedAtRequired";
    }
}
