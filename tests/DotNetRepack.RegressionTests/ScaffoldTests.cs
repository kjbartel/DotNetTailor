using System.Reflection;

namespace DotNetRepack.RegressionTests;

public sealed class ScaffoldTests
{
    [Fact]
    [Trait("WU", "000")]
    public void Referenced_assembly_loads()
    {
        var assembly = Assembly.Load("DotNetRepack.Cli");

        Assert.NotNull(assembly);
    }
}
