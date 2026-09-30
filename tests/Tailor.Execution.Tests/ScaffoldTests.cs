using System.Reflection;

namespace Tailor.Execution.Tests;

public sealed class ScaffoldTests
{
    [Fact]
    [Trait("WU", "000")]
    public void ReferencedAssemblyLoads()
    {
        var assembly = Assembly.Load("Tailor.Execution");

        Assert.NotNull(assembly);
    }
}
