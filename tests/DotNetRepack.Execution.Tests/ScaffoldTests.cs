using System.Reflection;

namespace DotNetRepack.Execution.Tests;

public sealed class ScaffoldTests
{
    [Fact]
    [Trait("WU", "000")]
    public void ReferencedAssemblyLoads()
    {
        var assembly = Assembly.Load("DotNetRepack.Execution");

        Assert.NotNull(assembly);
    }
}
