using System.Reflection;
using FluentAssertions;

namespace BankingApi.UnitTests.Architecture;

public class LayerDependencyTests
{
    private const string DomainAssembly = "BankingApi.Domain";
    private const string ApplicationAssembly = "BankingApi.Application";
    private const string InfrastructureAssembly = "BankingApi.Infrastructure";
    private const string ApiAssembly = "BankingApi.Api";

    [Fact]
    public void Domain_ReferencesOtherLayers_DoesNotDependOnAnyLayer()
    {
        var references = GetSolutionReferences(typeof(global::BankingApi.Domain.AssemblyReference).Assembly);

        references.Should().BeEmpty();
    }

    [Fact]
    public void Application_ReferencesOtherLayers_DependsOnlyOnDomain()
    {
        var references = GetSolutionReferences(typeof(global::BankingApi.Application.AssemblyReference).Assembly);

        references.Should().BeSubsetOf([DomainAssembly]);
    }

    [Fact]
    public void Infrastructure_ReferencesOtherLayers_DoesNotDependOnApi()
    {
        var references = GetSolutionReferences(typeof(global::BankingApi.Infrastructure.AssemblyReference).Assembly);

        references.Should().NotContain(ApiAssembly);
    }

    [Fact]
    public void Domain_ReferencesFrameworks_DoesNotDependOnEntityFrameworkOrAspNetCore()
    {
        var references = typeof(global::BankingApi.Domain.AssemblyReference).Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name ?? string.Empty);

        references.Should().NotContain(name =>
            name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) ||
            name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    private static string[] GetSolutionReferences(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => name is DomainAssembly or ApplicationAssembly or InfrastructureAssembly or ApiAssembly)
            .ToArray();
}
