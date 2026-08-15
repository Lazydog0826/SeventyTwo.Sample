using System.Security.Claims;
using SeventyTwo.Sample.Domain.Users;

namespace SeventyTwo.Sample.Application.Authentication;

/// <summary>
/// 当前执行的业务用户上下文。
/// HTTP 请求由 WebApi 在请求开始（认证之后）通过 <see cref="FromPrincipal"/> 注入业务 JWT 身份；
/// CAP 消费者、后台任务、测试等场景通过 <see cref="Set"/> 手动注入。
/// 身份未注入时读取属性是编程错误，会抛出异常。
/// </summary>
public interface IBusinessUserContext
{
    /// <summary>
    /// 用户 ID。
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// 用户所属机构 ID。
    /// </summary>
    Guid OrgId { get; }

    /// <summary>
    /// 数据权限类型。
    /// </summary>
    DataPermissionType DataPermissionType { get; }

    /// <summary>
    /// 从 HTTP 请求的业务 JWT 身份加载用户信息。
    /// 身份声明由认证处理器写入，能进入业务接口即视为完整有效。
    /// </summary>
    /// <param name="user">当前请求的认证身份。</param>
    void FromPrincipal(ClaimsPrincipal user);

    /// <summary>
    /// 手动注入用户信息；同一作用域内优先于 HTTP 解析结果。
    /// </summary>
    /// <param name="userId">用户 ID。</param>
    /// <param name="orgId">用户所属机构 ID。</param>
    /// <param name="dataPermissionType">数据权限类型。</param>
    void Set(Guid userId, Guid orgId, DataPermissionType dataPermissionType);
}
