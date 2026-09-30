using System.Reflection;

namespace DotNetRepack.Validation.Tests;

public sealed class ScaffoldTests
{
    [Fact]
    [Trait("WU", "000")]
    public void ReferencedAssemblyLoads()
    {
        var assembly = Assembly.Load("DotNetRepack.Validation");

        Assert.NotNull(assembly);
    }
}
