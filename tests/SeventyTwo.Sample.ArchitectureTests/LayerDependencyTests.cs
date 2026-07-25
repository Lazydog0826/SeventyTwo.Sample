using System.Reflection;
using SeventyTwo.Sample.Application.Abstractions;
using SeventyTwo.Sample.Domain.Orders;
using SeventyTwo.Sample.Infrastructure.Ids;

namespace SeventyTwo.Sample.ArchitectureTests;

public sealed class LayerDependencyTests
{
    [Fact]
    public void Domain_ShouldNotReferenceOuterLayers()
    {
        var references = typeof(Order).Assembly.GetReferencedAssemblies().Select(item => item.Name).ToHashSet();

        Assert.DoesNotContain("SeventyTwo.Sample.Application", references);
        Assert.DoesNotContain("SeventyTwo.Sample.Infrastructure", references);
        Assert.DoesNotContain("SeventyTwo.Sample.WebApi", references);
        Assert.DoesNotContain("SeventyTwo.InfraKit", references);
        Assert.DoesNotContain("SqlSugar", references);
    }

    [Fact]
    public void Application_ShouldNotReferenceInfrastructureOrWebApi()
    {
        var references = typeof(IIdGenerator).Assembly.GetReferencedAssemblies().Select(item => item.Name).ToHashSet();

        Assert.DoesNotContain("SeventyTwo.Sample.Infrastructure", references);
        Assert.DoesNotContain("SeventyTwo.Sample.WebApi", references);
        Assert.DoesNotContain("SeventyTwo.InfraKit", references);
        Assert.DoesNotContain("MediatR", references);
    }

    [Fact]
    public void Infrastructure_ShouldNotReferenceWebApi()
    {
        var references = typeof(SnowflakeIdGenerator).Assembly.GetReferencedAssemblies().Select(item => item.Name).ToHashSet();

        Assert.DoesNotContain("SeventyTwo.Sample.WebApi", references);
    }

    [Fact]
    public void WebApi_ShouldReferenceApplicationAndInfrastructure()
    {
        var references = Assembly
            .Load("SeventyTwo.Sample.WebApi")
            .GetReferencedAssemblies()
            .Select(item => item.Name)
            .ToHashSet();

        Assert.Contains("SeventyTwo.Sample.Application", references);
        Assert.Contains("SeventyTwo.Sample.Infrastructure", references);
    }
}
