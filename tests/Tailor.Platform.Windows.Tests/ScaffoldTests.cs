using System.Reflection;

namespace Tailor.Platform.Windows.Tests;

public sealed class ScaffoldTests
{
    [Fact]
    [Trait("WU", "000")]
    public void ReferencedAssemblyLoads()
    {
        var assembly = Assembly.Load("Tailor.Platform.Windows");

        Assert.NotNull(assembly);
    }
}
