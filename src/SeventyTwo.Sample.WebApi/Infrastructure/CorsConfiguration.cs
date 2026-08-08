namespace SeventyTwo.Sample.WebApi.Infrastructure;

internal sealed class CorsConfiguration
{
    public string Origins { get; init; } = string.Empty;

    public string Headers { get; init; } = string.Empty;

    public string Methods { get; init; } = string.Empty;
}
