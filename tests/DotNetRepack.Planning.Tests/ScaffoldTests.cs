using System.Reflection;

namespace DotNetRepack.Planning.Tests;

public sealed class ScaffoldTests
{
    [Fact]
    [Trait("WU", "000")]
    public void Referenced_assembly_loads()
    {
        var assembly = Assembly.Load("DotNetRepack.Planning");

        Assert.NotNull(assembly);
    }
}
