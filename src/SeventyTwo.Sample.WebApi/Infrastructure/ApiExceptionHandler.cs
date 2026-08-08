using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SeventyTwo.InfraKit.Core;
using SeventyTwo.Sample.Application.Authentication;
using SeventyTwo.Sample.Domain;

namespace SeventyTwo.Sample.WebApi.Infrastructure;

/// <summary>
/// 全局 API 异常处理器：将认证、领域及参数校验异常转换为对应的统一错误响应，
/// 并记录未处理异常的跟踪标识、异常数据和调用堆栈。
/// </summary>
/// <param name="logger">用于记录未处理异常的日志记录器。</param>
/// <param name="jsonOptions">用于序列化统一错误响应的 JSON 配置。</param>
public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger, IOptions<JsonOptions> jsonOptions)
    : IExceptionHandler
{
    /// <summary>
    /// 处理请求管道中捕获的异常，并将统一错误响应写入当前 HTTP 响应。
    /// </summary>
    /// <param name="httpContext">当前请求的 HTTP 上下文。</param>
    /// <param name="exception">请求管道中捕获的异常。</param>
    /// <param name="cancellationToken">用于取消异步响应写入操作的令牌。</param>
    /// <returns>始终返回 <see langword="true"/>，表示异常已处理。</returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        var request = httpContext.Request;

        var routeValues = request.RouteValues.ToDictionary(item => item.Key, item => item.Value?.ToString());

        request.Headers.TryGetValue("requestNo", out var requestNo);

        if (exception is not DomainException and not ApiValidationException and not TokenAuthenticationException)
        {
            logger.LogError(
                exception,
                """
                处理接口请求时发生未处理异常。
                RequestNo: {RequestNo}
                TraceId: {TraceId}
                Method: {Method}
                Path: {Path}
                RouteValues: {@RouteValues}
                ExceptionData: {@ExceptionData}
                """,
                requestNo,
                httpContext.TraceIdentifier,
                request.Method,
                request.Path.Value,
                routeValues,
                exception.Data
            );
        }

        var response = exception switch
        {
            TokenAuthenticationException => WebApiResponse.Error(exception.Message, HttpStatusCode.Unauthorized),
            DomainException or ApiValidationException => WebApiResponse.Error(
                exception.Message,
                HttpStatusCode.BadRequest
            ),
            _ => WebApiResponse.Error("服务异常: " + requestNo),
        };

        httpContext.Response.StatusCode = (int)response.Code;
        await httpContext.Response.WriteAsJsonAsync(
            response,
            jsonOptions.Value.JsonSerializerOptions,
            cancellationToken
        );
        return true;
    }
}
