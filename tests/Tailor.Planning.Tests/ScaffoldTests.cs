using System.Reflection;

namespace Tailor.Planning.Tests;

public sealed class ScaffoldTests
{
    [Fact]
    [Trait("WU", "000")]
    public void ReferencedAssemblyLoads()
    {
        var assembly = Assembly.Load("Tailor.Planning");

        Assert.NotNull(assembly);
    }
}
