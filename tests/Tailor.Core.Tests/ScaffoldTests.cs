using System.Reflection;

namespace Tailor.Core.Tests;

public sealed class ScaffoldTests
{
    [Fact]
    [Trait("WU", "000")]
    public void ReferencedAssemblyLoads()
    {
        var assembly = Assembly.Load("Tailor.Core");

        Assert.NotNull(assembly);
    }
}
