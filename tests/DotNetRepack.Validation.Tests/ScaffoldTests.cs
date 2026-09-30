using System.Reflection;

namespace DotNetRepack.Validation.Tests;

public sealed class ScaffoldTests
{
    [Fact]
    [Trait("WU", "000")]
    public void Referenced_assembly_loads()
    {
        var assembly = Assembly.Load("DotNetRepack.Validation");

        Assert.NotNull(assembly);
    }
}
