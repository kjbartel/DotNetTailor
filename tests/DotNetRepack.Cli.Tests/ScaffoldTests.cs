using System.Reflection;

namespace DotNetRepack.Cli.Tests;

public sealed class ScaffoldTests
{
    [Fact]
    [Trait("WU", "000")]
    public void ReferencedAssemblyLoads()
    {
        var assembly = Assembly.Load("DotNetRepack.Cli");

        Assert.NotNull(assembly);
    }
}
