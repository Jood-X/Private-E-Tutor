using System.Reflection;
using NetArchTest.Rules;

namespace ETutor.UnitTests.Architecture;

/// <summary>
/// Enforces the dependency rule from docs/architecture.md §2.1. A failing test here means a layer
/// has started to depend on something it must not know about.
/// </summary>
public sealed class LayerDependencyTests
{
    private const string Domain = "ETutor.Domain";
    private const string Application = "ETutor.Application";
    private const string Infrastructure = "ETutor.Infrastructure";
    private const string Api = "ETutor.Api";

    private static readonly Assembly DomainAssembly = typeof(ETutor.Domain.Common.LocalizedText).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(ETutor.Application.DependencyInjection).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(ETutor.Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly ApiAssembly = typeof(Program).Assembly;

    [Fact]
    public void Domain_DependsOnNothingElse()
    {
        AssertNoDependency(DomainAssembly, Application, Infrastructure, Api,
            "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "System.Net.Http");
    }

    [Fact]
    public void Application_DoesNotDependOnInfrastructureOrWeb()
    {
        AssertNoDependency(ApplicationAssembly, Infrastructure, Api,
            "Microsoft.EntityFrameworkCore.SqlServer", "Microsoft.AspNetCore");
    }

    [Fact]
    public void Infrastructure_DoesNotDependOnApi()
    {
        AssertNoDependency(InfrastructureAssembly, Api, "Microsoft.AspNetCore.Mvc");
    }

    [Fact]
    public void Controllers_DoNotUsePersistenceDirectly()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That().ResideInNamespace($"{Api}.Controllers")
            .ShouldNot().HaveDependencyOnAny("ETutor.Infrastructure", "Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Controllers_InheritTheEnvelopeBase()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That().ResideInNamespace($"{Api}.Controllers").And().AreClasses()
            .Should().Inherit(typeof(ETutor.Api.Common.ApiControllerBase))
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    private static void AssertNoDependency(Assembly assembly, params string[] forbidden)
    {
        var result = Types.InAssembly(assembly).ShouldNot().HaveDependencyOnAny(forbidden).GetResult();
        Assert.True(result.IsSuccessful, Describe(result));
    }

    private static string Describe(TestResult result) =>
        "Offending types: " + string.Join(", ", result.FailingTypeNames ?? []);
}
