using System.Reflection;

namespace Tailor.Cli.Tests;

public sealed class ScaffoldTests
{
    [Fact]
    [Trait("WU", "000")]
    public void ReferencedAssemblyLoads()
    {
        var assembly = Assembly.Load("Tailor.Cli");

        Assert.NotNull(assembly);
    }
}
