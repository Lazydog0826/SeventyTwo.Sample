using SeventyTwo.Sample.WebApi.Authentication;

namespace SeventyTwo.Sample.ArchitectureTests;

public sealed class CapDashboardAuthenticationTests
{
    [Theory]
    [InlineData("/cap")]
    [InlineData("/cap/")]
    [InlineData("/cap/index.html")]
    [InlineData("/cap/assets/index.js")]
    public void SelectScheme_ShouldUseBasicForDashboardPath(string path)
    {
        var scheme = CapDashboardAuthenticationDefaults.SelectScheme(path);

        Assert.Equal(CapDashboardAuthenticationDefaults.BasicScheme, scheme);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/api/users")]
    [InlineData("/capability")]
    public void SelectScheme_ShouldUseBusinessJwtOutsideDashboardPath(string path)
    {
        var scheme = CapDashboardAuthenticationDefaults.SelectScheme(path);

        Assert.Equal(BusinessJwtAuthenticationDefaults.Scheme, scheme);
    }
}
