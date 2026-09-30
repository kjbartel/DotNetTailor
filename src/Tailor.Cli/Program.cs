namespace Tailor.Cli;

/// <summary>Process entry point for dotnet-tailor.</summary>
public static class Program
{
    /// <summary>Runs dotnet-tailor from the process entry point.</summary>
    public static Task<int> Main(string[] args) => CliApplication.RunAsync(args, Console.Out, Console.Error);
}
