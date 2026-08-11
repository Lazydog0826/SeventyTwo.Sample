namespace SeventyTwo.Sample.Domain.DataDictionaries;

/// <summary>
/// 数据字典仓储。
/// </summary>
public interface IDataDictionaryRepository
{
    /// <summary>
    /// 按 ID 查找数据字典及其字典项。
    /// </summary>
    /// <param name="id">数据字典 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>匹配的数据字典；不存在时返回 <see langword="null" />。</returns>
    Task<DataDictionary?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// 按编码查找已启用的数据字典及其字典项。
    /// </summary>
    /// <param name="code">数据字典编码。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>匹配且已启用的数据字典；不存在时返回 <see langword="null" />。</returns>
    Task<DataDictionary?> FindEnabledByCodeAsync(string code, CancellationToken cancellationToken);

    /// <summary>
    /// 获取数据字典及其字典项列表。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>数据字典列表。</returns>
    Task<IReadOnlyList<DataDictionary>> GetListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 检查数据字典编码是否已存在。
    /// </summary>
    /// <param name="code">数据字典编码。</param>
    /// <param name="excludedId">检查时排除的数据字典 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>编码已存在时返回 <see langword="true" />；否则返回 <see langword="false" />。</returns>
    Task<bool> CodeExistsAsync(string code, Guid? excludedId, CancellationToken cancellationToken);

    /// <summary>
    /// 新增数据字典。
    /// </summary>
    /// <param name="dictionary">待新增的数据字典。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task AddAsync(DataDictionary dictionary, CancellationToken cancellationToken);

    /// <summary>
    /// 保存数据字典的基本信息和启用状态。
    /// </summary>
    /// <param name="dictionary">待保存的数据字典。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task SaveAsync(DataDictionary dictionary, CancellationToken cancellationToken);

    /// <summary>
    /// 保存数据字典及其全部字典项。
    /// </summary>
    /// <param name="dictionary">待保存的数据字典。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task SaveItemsAsync(DataDictionary dictionary, CancellationToken cancellationToken);

    /// <summary>
    /// 删除指定的数据字典及其字典项。
    /// </summary>
    /// <param name="id">数据字典 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
