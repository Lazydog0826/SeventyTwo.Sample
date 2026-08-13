namespace SeventyTwo.Sample.Application.Organizations;

/// <summary>
/// 机构应用服务。
/// </summary>
public interface IOrganizationApplication
{
    /// <summary>
    /// 获取指定机构的编辑详情。
    /// </summary>
    Task<OrganizationListOutput> GetDetailAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// 创建机构。
    /// </summary>
    /// <param name="input">创建机构的输入。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>创建后的机构信息。</returns>
    Task<OrganizationListOutput> CreateAsync(CreateOrganizationInput input, CancellationToken cancellationToken);

    /// <summary>
    /// 更新指定机构。
    /// </summary>
    /// <param name="id">机构 ID。</param>
    /// <param name="input">更新机构的输入。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task UpdateAsync(Guid id, UpdateOrganizationInput input, CancellationToken cancellationToken);

    /// <summary>
    /// 删除指定机构。
    /// </summary>
    /// <param name="id">机构 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// 获取机构列表。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>机构只读列表。</returns>
    Task<IReadOnlyList<OrganizationListOutput>> GetListAsync(CancellationToken cancellationToken);
}
