namespace SeventyTwo.Sample.Application.DataDictionaries;

/// <summary>
/// 数据字典应用服务。
/// </summary>
public interface IDataDictionaryApplication
{
    /// <summary>
    /// 创建数据字典。
    /// </summary>
    /// <param name="input">数据字典创建参数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>新建的数据字典。</returns>
    Task<DataDictionaryListOutput> CreateAsync(CreateDataDictionaryInput input, CancellationToken cancellationToken);

    /// <summary>
    /// 更新指定的数据字典。
    /// </summary>
    /// <param name="id">数据字典 ID。</param>
    /// <param name="input">数据字典更新参数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task UpdateAsync(Guid id, UpdateDataDictionaryInput input, CancellationToken cancellationToken);

    /// <summary>
    /// 删除指定的数据字典。
    /// </summary>
    /// <param name="id">数据字典 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// 获取数据字典列表。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>数据字典列表。</returns>
    Task<IReadOnlyList<DataDictionaryListOutput>> GetListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 获取指定数据字典的字典项。
    /// </summary>
    /// <param name="id">数据字典 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>数据字典项及字典版本信息。</returns>
    Task<DataDictionaryItemsOutput> GetItemsAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// 创建数据字典项。
    /// </summary>
    /// <param name="input">数据字典项创建参数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>创建后的字典项及最新字典版本信息。</returns>
    Task<DataDictionaryItemMutationOutput> CreateItemAsync(
        CreateDataDictionaryItemInput input,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// 更新数据字典项。
    /// </summary>
    /// <param name="input">数据字典项更新参数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>更新后的字典项及最新字典版本信息。</returns>
    Task<DataDictionaryItemMutationOutput> UpdateItemAsync(
        UpdateDataDictionaryItemInput input,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// 删除数据字典项。
    /// </summary>
    /// <param name="input">数据字典项删除参数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>删除后的最新字典版本信息。</returns>
    Task<DataDictionaryItemMutationOutput> DeleteItemAsync(
        DeleteDataDictionaryItemInput input,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// 按数据字典编码获取已启用字典的选项。
    /// </summary>
    /// <param name="code">数据字典编码。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>按排序序号排列的数据字典选项。</returns>
    Task<IReadOnlyList<DataDictionaryOptionOutput>> GetOptionsByCodeAsync(
        string code,
        CancellationToken cancellationToken
    );
}
