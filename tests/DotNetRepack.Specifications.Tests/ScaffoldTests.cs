using System.Reflection;

namespace DotNetRepack.Specifications.Tests;

public sealed class ScaffoldTests
{
    [Fact]
    [Trait("WU", "000")]
    public void ReferencedAssemblyLoads()
    {
        var assembly = Assembly.Load("DotNetRepack.Specifications");

        Assert.NotNull(assembly);
    }
}
