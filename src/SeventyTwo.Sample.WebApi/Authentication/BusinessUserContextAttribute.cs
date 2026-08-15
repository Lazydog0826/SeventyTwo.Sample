using Microsoft.AspNetCore.Mvc.Filters;
using SeventyTwo.Sample.Application.Authentication;

namespace SeventyTwo.Sample.WebApi.Authentication;

/// <summary>
/// 标记需要初始化业务用户上下文的接口。
/// 排在所有过滤器最后执行（授权、模型校验等完成后、进入操作前），
/// 将当前认证身份注入 <see cref="IBusinessUserContext"/>，供应用层读取；
/// 未标记的接口不初始化，上下文保持为空。可标记在控制器或操作上。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class BusinessUserContextAttribute : Attribute, IAsyncActionFilter, IOrderedFilter
{
    /// <summary>
    /// 排到最后执行，保证注入身份时其余过滤器均已执行完毕。
    /// </summary>
    public int Order => int.MaxValue;

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            context
                .HttpContext.RequestServices.GetRequiredService<IBusinessUserContext>()
                .FromPrincipal(context.HttpContext.User);
        }

        await next();
    }
}
