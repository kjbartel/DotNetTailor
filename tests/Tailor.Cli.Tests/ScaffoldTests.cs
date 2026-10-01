using System.Reflection;

namespace Tailor.Cli.Tests;

public sealed class ScaffoldTests
{
    [Fact]
    [Trait("WU", "000")]
    public void ReferencedAssemblyLoads()
    {
        var assembly = Assembly.Load("Tailor.Cli");

        Assert.NotNull(assembly);
    }

    [Fact]
    [Trait("WU", "105")]
    public async Task HelpListsCommandsAndGlobalOptions()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await CliApplication.RunAsync(["--help"], output, error, ct: TestContext.Current.CancellationToken);

        Assert.Equal(0, exitCode);
        Assert.Contains("analyse", output.ToString());
        Assert.Contains("--artefacts", output.ToString());
    }

    [Fact]
    [Trait("WU", "105")]
    public async Task AnalyseAliasReturnsNotImplemented()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await CliApplication.RunAsync(["analyze", "app"], output, error, ct: TestContext.Current.CancellationToken);

        Assert.Equal(70, exitCode);
        Assert.Contains("TLR0100", error.ToString());
    }

    [Theory]
    [InlineData("1x=a")]
    [InlineData("novalue")]
    [Trait("WU", "105")]
    public async Task InvalidVarReturnsUsage(string value)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await CliApplication.RunAsync(["validate", "app", "--spec", "s.json", "--var", value], output, error, ct: TestContext.Current.CancellationToken);

        Assert.Equal(2, exitCode);
        Assert.Contains("TLR0101", error.ToString());
    }

    [Fact]
    [Trait("WU", "105")]
    public async Task ConflictingFailureModesReturnUsage()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await CliApplication.RunAsync(["validate", "app", "--spec", "s.json", "--strict", "--permissive"], output, error, ct: TestContext.Current.CancellationToken);

        Assert.Equal(2, exitCode);
        Assert.Contains("TLR0103", error.ToString());
    }

    [Fact]
    [Trait("WU", "105")]
    public async Task DuplicateVarReturnsUsage()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await CliApplication.RunAsync(["validate", "app", "--spec", "s.json", "--var", "a=1", "--var", "a=2"], output, error, ct: TestContext.Current.CancellationToken);

        Assert.Equal(2, exitCode);
        Assert.Contains("TLR0102", error.ToString());
    }

    [Fact]
    [Trait("WU", "105")]
    public async Task VarValueMayContainEquals()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await CliApplication.RunAsync(["validate", "app", "--spec", "s.json", "--var", "a=b=c"], output, error, ct: TestContext.Current.CancellationToken);

        Assert.Equal(70, exitCode);
        Assert.Contains("TLR0100", error.ToString());
    }

    [Fact]
    [Trait("WU", "105")]
    public async Task UnknownOptionReturnsUsage()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await CliApplication.RunAsync(["validate", "app", "--unknown"], output, error, ct: TestContext.Current.CancellationToken);

        Assert.Equal(2, exitCode);
        Assert.Contains("TLR0105", error.ToString());
    }

    [Theory]
    [InlineData("analyse", "app")]
    [InlineData("validate", "app", "--spec", "s.json")]
    [InlineData("plan", "app", "--spec", "s.json", "--transform", "t.json")]
    [InlineData("apply", "app", "--spec", "s.json", "--transform", "t.json", "--output", "out")]
    [InlineData("inspect", "app", "--spec", "s.json")]
    [InlineData("schema", "export")]
    [Trait("WU", "105")]
    public async Task DeclaredStubReturnsNotImplemented(params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await CliApplication.RunAsync(args, output, error, ct: TestContext.Current.CancellationToken);

        Assert.Equal(70, exitCode);
        Assert.Contains("TLR0100", error.ToString());
    }

    [Fact]
    [Trait("WU", "105")]
    public async Task PlanSchemaKindReturnsUsage()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await CliApplication.RunAsync(["schema", "export", "plan"], output, error, ct: TestContext.Current.CancellationToken);

        Assert.Equal(2, exitCode);
        Assert.Contains("TLR0104", error.ToString());
    }

    [Fact]
    [Trait("WU", "105")]
    public async Task UnknownSchemaKindReturnsUsage()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await CliApplication.RunAsync(["schema", "export", "unknown"], output, error, ct: TestContext.Current.CancellationToken);

        Assert.Equal(2, exitCode);
        Assert.Contains("TLR0104", error.ToString());
    }

    [Fact]
    [Trait("WU", "105")]
    public async Task VersionWritesAssemblyVersion()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await CliApplication.RunAsync(["--version"], output, error, ct: TestContext.Current.CancellationToken);

        Assert.Equal(0, exitCode);
        Assert.NotEmpty(output.ToString().Trim());
    }

    [Fact]
    [Trait("WU", "105")]
    public async Task InvalidVerbosityReturnsUsage()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await CliApplication.RunAsync(["validate", "app", "--spec", "s.json", "--verbosity", "verbose"], output, error, ct: TestContext.Current.CancellationToken);

        Assert.Equal(2, exitCode);
        Assert.Contains("TLR0105", error.ToString());
    }
}
