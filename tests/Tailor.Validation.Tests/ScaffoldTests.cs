using System.Reflection;

namespace Tailor.Validation.Tests;

public sealed class ScaffoldTests
{
    [Fact]
    [Trait("WU", "000")]
    public void ReferencedAssemblyLoads()
    {
        var assembly = Assembly.Load("Tailor.Validation");

        Assert.NotNull(assembly);
    }
}
