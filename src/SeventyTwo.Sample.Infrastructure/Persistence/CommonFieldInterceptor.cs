using SeventyTwo.InfraKit.Extension;
using SeventyTwo.Sample.Application.Authentication;
using SeventyTwo.Sample.Domain;
using SqlSugar;

namespace SeventyTwo.Sample.Infrastructure.Persistence;

/// <summary>
/// 公共字段自动填充拦截器。
/// 持久化实体按需实现 <see cref="IAudited"/>、<see cref="IOrgScoped"/>，
/// 即在写入时自动填充对应字段：插入时生成创建时间、补全创建人与机构归属，
/// 实体更新时覆盖修改人与修改时间，取值来自当前业务用户上下文；
/// 未实现接口的实体不受任何影响，全局数据等无需自动归属的实体不实现接口即可。
/// </summary>
/// <remarks>
/// <para>
/// 仅对基于实体对象的插入、更新生效；<c>SetColumns</c> 表达式更新的 SET 列由表达式自身决定，
/// 拦截器事件虽会触发但无法改写其列值，需要自动填充的更新必须使用实体加 <c>UpdateColumns</c> 风格。
/// </para>
/// <para>
/// 业务身份未注入（后台任务、未标记 BusinessUserContext 的接口）时，
/// <see cref="IBusinessUserContext"/> 按其设计抛出异常，写入随之失败，
/// 由调用方通过 Set 显式注入身份后再触发写入。
/// </para>
/// </remarks>
public static class CommonFieldInterceptor
{
    /// <summary>
    /// 为 SqlSugar 客户端挂接公共字段自动填充。
    /// </summary>
    /// <param name="client">待挂接的 SqlSugar 客户端。</param>
    /// <param name="userContext">当前依赖注入作用域的业务用户上下文，由宿主注册。</param>
    public static void Attach(SqlSugarClient client, IBusinessUserContext userContext)
    {
        client.Aop.DataExecuting = (oldValue, info) => Fill(oldValue, info, userContext);
    }

    /// <summary>
    /// 按操作类型分发填充逻辑；事件对实体的每个列各触发一次。
    /// </summary>
    /// <param name="oldValue">被填充前列的当前值。</param>
    /// <param name="info">SqlSugar 数据过滤事件参数。</param>
    /// <param name="userContext">当前作用域的业务用户上下文。</param>
    private static void Fill(object? oldValue, DataFilterModel info, IBusinessUserContext userContext)
    {
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (info.OperationType)
        {
            case DataFilterType.InsertByObject:
                FillInsert(oldValue, info, userContext);
                break;
            case DataFilterType.UpdateByObject:
                FillUpdate(info, userContext);
                break;
        }
    }

    /// <summary>
    /// 插入补全：创建时间统一由拦截器生成（创建时刻即入库时刻，覆盖预置值）；
    /// 创建人与机构归属仅当仍为默认值时补全，保留调用方的显式赋值。
    /// </summary>
    private static void FillInsert(object? oldValue, DataFilterModel info, IBusinessUserContext userContext)
    {
        if (info.EntityValue is not IOrgScoped && info.EntityValue is not IAudited)
        {
            return;
        }

        switch (info.PropertyName)
        {
            case nameof(IOrgScoped.OrgId)
                when info.EntityValue is IOrgScoped && oldValue is Guid orgId && orgId == Guid.Empty:
                info.SetValue(userContext.OrgId);
                break;
            case nameof(IAudited.CreatedBy)
                when info.EntityValue is IAudited && oldValue is Guid createdBy && createdBy == SystemIds.System:
                info.SetValue(userContext.UserId);
                break;
            case nameof(IAudited.CreatedAt) when info.EntityValue is IAudited:
                info.SetValue(DateTimeExtension.Now());
                break;
        }
    }

    /// <summary>
    /// 更新覆盖：修改人、修改时间始终反映本次实际执行者。
    /// </summary>
    private static void FillUpdate(DataFilterModel info, IBusinessUserContext userContext)
    {
        if (info.EntityValue is not IAudited)
        {
            return;
        }

        switch (info.PropertyName)
        {
            case nameof(IAudited.UpdatedBy):
                info.SetValue(userContext.UserId);
                break;
            case nameof(IAudited.UpdatedAt):
                info.SetValue(DateTimeExtension.Now());
                break;
        }
    }
}
