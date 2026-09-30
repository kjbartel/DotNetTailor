using System.Reflection;

namespace DotNetRepack.Analysis.Tests;

public sealed class ScaffoldTests
{
    [Fact]
    [Trait("WU", "000")]
    public void ReferencedAssemblyLoads()
    {
        var assembly = Assembly.Load("DotNetRepack.Analysis");

        Assert.NotNull(assembly);
    }
}
