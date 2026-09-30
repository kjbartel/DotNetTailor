using System.Reflection;

namespace DotNetRepack.Platform.Abstractions.Tests;

public sealed class ScaffoldTests
{
    [Fact]
    [Trait("WU", "000")]
    public void Referenced_assembly_loads()
    {
        var assembly = Assembly.Load("DotNetRepack.Platform.Abstractions");

        Assert.NotNull(assembly);
    }
}
