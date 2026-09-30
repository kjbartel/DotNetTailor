using System.Reflection;

namespace Tailor.Platform.Abstractions.Tests;

public sealed class ScaffoldTests
{
    [Fact]
    [Trait("WU", "000")]
    public void ReferencedAssemblyLoads()
    {
        var assembly = Assembly.Load("Tailor.Platform.Abstractions");

        Assert.NotNull(assembly);
    }
}
