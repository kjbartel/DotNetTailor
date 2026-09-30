using System.Reflection;

namespace Tailor.Model.Tests;

public sealed class ScaffoldTests
{
    [Fact]
    [Trait("WU", "000")]
    public void ReferencedAssemblyLoads()
    {
        var assembly = Assembly.Load("Tailor.Model");

        Assert.NotNull(assembly);
    }
}
