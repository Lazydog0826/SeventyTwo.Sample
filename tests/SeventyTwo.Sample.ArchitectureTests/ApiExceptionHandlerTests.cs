using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SeventyTwo.Sample.Application.Authentication;
using SeventyTwo.Sample.Common.MessageKeys;
using SeventyTwo.Sample.Domain;
using SeventyTwo.Sample.WebApi.Infrastructure;

namespace SeventyTwo.Sample.ArchitectureTests;

public sealed class ApiExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_ShouldReturnMessageAndErrorCodeKeys()
    {
        await AssertResponseAsync(
            new TokenAuthenticationException(MessageKeys.Authentication.RefreshTokenInvalid),
            MessageKeys.Authentication.RefreshTokenInvalid,
            ErrorCodes.Authentication,
            HttpStatusCode.Unauthorized
        );
        await AssertResponseAsync(
            new ApiValidationException(MessageKeys.Validation.AccountRequired),
            MessageKeys.Validation.AccountRequired,
            ErrorCodes.Validation,
            HttpStatusCode.BadRequest
        );
        await AssertDomainResponseAsync(DomainErrorType.Validation, HttpStatusCode.UnprocessableEntity);
        await AssertDomainResponseAsync(DomainErrorType.BusinessRule, HttpStatusCode.UnprocessableEntity);
        await AssertDomainResponseAsync(DomainErrorType.NotFound, HttpStatusCode.NotFound);
        await AssertDomainResponseAsync(DomainErrorType.Conflict, HttpStatusCode.Conflict);
        await AssertResponseAsync(
            new InvalidOperationException("internal detail"),
            MessageKeys.Common.InternalError,
            ErrorCodes.Internal,
            HttpStatusCode.InternalServerError
        );
    }

    private static Task AssertDomainResponseAsync(
        DomainErrorType errorType,
        HttpStatusCode expectedStatusCode
    ) =>
        AssertResponseAsync(
            new DomainException(MessageKeys.Products.NameRequired, errorType),
            MessageKeys.Products.NameRequired,
            ErrorCodes.Domain,
            expectedStatusCode
        );

    private static async Task AssertResponseAsync(
        Exception exception,
        string expectedMessageKey,
        string expectedErrorCode,
        HttpStatusCode expectedStatusCode
    )
    {
        var handler = new ApiExceptionHandler(
            NullLogger<ApiExceptionHandler>.Instance,
            Options.Create(new JsonOptions())
        );
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal((int)expectedStatusCode, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(expectedMessageKey, document.RootElement.GetProperty("message").GetString());
        Assert.Equal(expectedErrorCode, document.RootElement.GetProperty("errorCode").GetString());
    }
}
