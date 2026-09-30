using System.Reflection;

namespace Tailor.Specifications.Tests;

public sealed class ScaffoldTests
{
    [Fact]
    [Trait("WU", "000")]
    public void ReferencedAssemblyLoads()
    {
        var assembly = Assembly.Load("Tailor.Specifications");

        Assert.NotNull(assembly);
    }
}
