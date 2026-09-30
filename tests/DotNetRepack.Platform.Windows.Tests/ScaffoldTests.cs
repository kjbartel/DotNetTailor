using System.Reflection;

namespace DotNetRepack.Platform.Windows.Tests;

public sealed class ScaffoldTests
{
    [Fact]
    [Trait("WU", "000")]
    public void ReferencedAssemblyLoads()
    {
        var assembly = Assembly.Load("DotNetRepack.Platform.Windows");

        Assert.NotNull(assembly);
    }
}
