namespace SeventyTwo.Sample.Common.MessageKeys;

public static partial class MessageKeys
{
    public static class Paging
    {
        /// <summary>
        /// 页码必须大于零。
        /// </summary>
        public const string PageNumberMustBePositive = "paging.pageNumberMustBePositive";

        /// <summary>
        /// 每页数量必须在一到一百之间。
        /// </summary>
        public const string PageSizeOutOfRange100 = "paging.pageSizeOutOfRange100";

        /// <summary>
        /// 每页数量必须在一到一千之间。
        /// </summary>
        public const string PageSizeOutOfRange1000 = "paging.pageSizeOutOfRange1000";

        /// <summary>
        /// 分页偏移超出支持范围。
        /// </summary>
        public const string PageOffsetOutOfRange = "paging.pageOffsetOutOfRange";

        /// <summary>
        /// 游标时间和游标 ID 必须同时提供。
        /// </summary>
        public const string CursorFieldsMustBeProvidedTogether = "paging.cursorFieldsMustBeProvidedTogether";

        /// <summary>
        /// 游标翻页方向无效。
        /// </summary>
        public const string CursorDirectionInvalid = "paging.cursorDirectionInvalid";

        /// <summary>
        /// 查询上一页时必须提供游标。
        /// </summary>
        public const string PreviousPageCursorRequired = "paging.previousPageCursorRequired";
    }
}
