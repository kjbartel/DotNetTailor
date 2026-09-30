using System.Reflection;

namespace DotNetRepack.Acquisition.Tests;

public sealed class ScaffoldTests
{
    [Fact]
    [Trait("WU", "000")]
    public void ReferencedAssemblyLoads()
    {
        var assembly = Assembly.Load("DotNetRepack.Acquisition");

        Assert.NotNull(assembly);
    }
}
