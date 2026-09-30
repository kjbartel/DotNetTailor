using System.Reflection;

namespace DotNetRepack.Analysis.Tests;

public sealed class ScaffoldTests
{
    [Fact]
    [Trait("WU", "000")]
    public void Referenced_assembly_loads()
    {
        var assembly = Assembly.Load("DotNetRepack.Analysis");

        Assert.NotNull(assembly);
    }
}
