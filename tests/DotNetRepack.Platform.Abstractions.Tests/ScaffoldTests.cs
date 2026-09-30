using System.Reflection;

namespace DotNetRepack.Platform.Abstractions.Tests;

public sealed class ScaffoldTests
{
    [Fact]
    [Trait("WU", "000")]
    public void ReferencedAssemblyLoads()
    {
        var assembly = Assembly.Load("DotNetRepack.Platform.Abstractions");

        Assert.NotNull(assembly);
    }
}
