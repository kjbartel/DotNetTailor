using System.Reflection;

namespace Tailor.Inspection.Tests;

public sealed class ScaffoldTests
{
    [Fact]
    [Trait("WU", "000")]
    public void ReferencedAssemblyLoads()
    {
        var assembly = Assembly.Load("Tailor.Inspection");

        Assert.NotNull(assembly);
    }
}
