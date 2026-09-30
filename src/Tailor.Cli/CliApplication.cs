using System.CommandLine;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Tailor.Cli;

/// <summary>Composes and runs the dotnet-tailor command-line application.</summary>
public static class CliApplication
{
    /// <summary>Runs the in-process command-line application.</summary>
    public static async Task<int> RunAsync(
        string[] args,
        TextWriter stdout,
        TextWriter stderr,
        IServiceProvider? services = null,
        CancellationToken ct = default)
    {
        var root = RootCommandFactory.Create(out var globalOptions);
        var parseResult = root.Parse(args);

        if (args.Any(static arg => arg is "--help" or "-h"))
        {
            return await parseResult.InvokeAsync(new InvocationConfiguration
            {
                Output = stdout,
                Error = stderr,
            }, ct).ConfigureAwait(false);
        }

        if (args.Contains("--version", StringComparer.Ordinal))
        {
            var version = typeof(Program).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? typeof(Program).Assembly.GetName().Version?.ToString()
                ?? "unknown";
            await stdout.WriteLineAsync(version).ConfigureAwait(false);
            return ExitCodes.Success;
        }

        if (parseResult.Errors.Count > 0)
        {
            foreach (var error in parseResult.Errors)
            {
                await stderr.WriteLineAsync($"{CliDiagnostics.ParseError}: {error.Message}").ConfigureAwait(false);
            }

            await stderr.WriteLineAsync("Use --help for usage.").ConfigureAwait(false);
            return ExitCodes.Usage;
        }

        try
        {
            var settingsResult = GlobalSettingsParser.Parse(parseResult, globalOptions);
            foreach (var diagnostic in settingsResult.Diagnostics)
            {
                await stderr.WriteLineAsync(diagnostic).ConfigureAwait(false);
            }

            if (settingsResult.Diagnostics.Count > 0)
            {
                return ExitCodes.Usage;
            }

            if (parseResult.CommandResult.Command == root)
            {
                await stdout.WriteLineAsync(root.Description).ConfigureAwait(false);
                return ExitCodes.Success;
            }

            if (args.Length >= 3 &&
                string.Equals(args[0], "schema", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(args[1], "export", StringComparison.OrdinalIgnoreCase) &&
                !IsSupportedSchemaKind(args[2]))
            {
                await stderr.WriteLineAsync($"{CliDiagnostics.UnknownSchemaKind}: schema kind '{args[2]}' is not available yet.").ConfigureAwait(false);
                return ExitCodes.Usage;
            }

            await stderr.WriteLineAsync($"{CliDiagnostics.NotImplemented}: command '{parseResult.CommandResult.Command.Name}' is not implemented yet.").ConfigureAwait(false);
            return ExitCodes.NotImplemented;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            await stderr.WriteLineAsync("Operation cancelled.").ConfigureAwait(false);
            return ExitCodes.Cancelled;
        }
        catch (Exception exception)
        {
            await stderr.WriteLineAsync($"{CliDiagnostics.UnhandledException}: {exception.GetType().Name}: {exception.Message}").ConfigureAwait(false);
            return ExitCodes.InternalError;
        }
    }

    private static bool IsSupportedSchemaKind(string kind) =>
        kind.Equals("appspec", StringComparison.OrdinalIgnoreCase) ||
        kind.Equals("transformspec", StringComparison.OrdinalIgnoreCase);
}

internal sealed record GlobalSettingsParseResult(GlobalSettings? Settings, IReadOnlyList<string> Diagnostics);

internal static partial class GlobalSettingsParser
{
    private static readonly Regex VariableName = CreateVariableNameRegex();

    public static GlobalSettingsParseResult Parse(ParseResult parseResult, GlobalOptions options)
    {
        var diagnostics = new List<string>();
        var strict = parseResult.GetValue(options.Strict);
        var permissive = parseResult.GetValue(options.Permissive);
        if (strict && permissive)
        {
            diagnostics.Add($"{CliDiagnostics.ConflictingFailureModes}: --strict and --permissive cannot be used together.");
        }

        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in parseResult.GetValue(options.Variables) ?? [])
        {
            var separator = value.IndexOf('=');
            var name = separator < 0 ? value : value[..separator];
            if (separator <= 0 || !VariableName.IsMatch(name))
            {
                diagnostics.Add($"{CliDiagnostics.InvalidVariable}: --var must use name=value with a valid name.");
                continue;
            }

            if (!variables.TryAdd(name, value[(separator + 1)..]))
            {
                diagnostics.Add($"{CliDiagnostics.DuplicateVariable}: duplicate --var name '{name}'.");
            }
        }

        var verbosityValue = parseResult.GetValue(options.Verbosity) ?? "normal";
        if (!TryParseVerbosity(verbosityValue, out var verbosity))
        {
            diagnostics.Add($"{CliDiagnostics.ParseError}: invalid --verbosity value '{verbosityValue}'.");
            verbosity = Verbosity.Normal;
        }

        return new GlobalSettingsParseResult(
            new GlobalSettings(
                verbosity,
                strict ? FailureMode.Strict : permissive ? FailureMode.Permissive : FailureMode.Default,
                parseResult.GetValue(options.Artifacts)?.FullName,
                parseResult.GetValue(options.Offline),
                variables),
            diagnostics);
    }

    private static bool TryParseVerbosity(string? value, out Verbosity verbosity)
    {
        value ??= "normal";
        verbosity = value.ToLowerInvariant() switch
        {
            "q" or "quiet" => Verbosity.Quiet,
            "m" or "minimal" => Verbosity.Minimal,
            "n" or "normal" => Verbosity.Normal,
            "d" or "detailed" => Verbosity.Detailed,
            "diag" or "diagnostic" => Verbosity.Diagnostic,
            _ => Verbosity.Normal,
        };

        return value.Equals("q", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("quiet", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("m", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("minimal", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("n", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("normal", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("d", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("detailed", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("diag", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("diagnostic", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex CreateVariableNameRegex();
}
