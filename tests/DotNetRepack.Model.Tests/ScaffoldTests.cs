using System.Reflection;

namespace DotNetRepack.Model.Tests;

public sealed class ScaffoldTests
{
    [Fact]
    [Trait("WU", "000")]
    public void ReferencedAssemblyLoads()
    {
        var assembly = Assembly.Load("DotNetRepack.Model");

        Assert.NotNull(assembly);
    }
}
