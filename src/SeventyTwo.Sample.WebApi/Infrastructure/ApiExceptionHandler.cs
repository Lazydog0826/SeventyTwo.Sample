using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SeventyTwo.InfraKit.Core;
using SeventyTwo.Sample.Domain;

namespace SeventyTwo.Sample.WebApi.Infrastructure;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger, IOptions<JsonOptions> jsonOptions)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        if (exception is not DomainException)
        {
            logger.LogError(exception, "处理接口请求时发生未处理异常");
        }

        var response = exception switch
        {
            DomainException => WebApiResponse.Error(exception.Message, HttpStatusCode.BadRequest),
            _ => WebApiResponse.Error("服务异常"),
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
