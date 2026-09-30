using System.CommandLine;

namespace Tailor.Cli;

internal static class RootCommandFactory
{
    public static RootCommand Create(out GlobalOptions globalOptions)
    {
        var root = new RootCommand("dotnet-tailor");
        globalOptions = GlobalOptions.Create();

        foreach (var option in globalOptions.All)
        {
            option.Recursive = true;
            root.Options.Add(option);
        }

        root.Options.Add(globalOptions.Version);

        root.Subcommands.Add(CreateAnalyzeCommand());
        root.Subcommands.Add(CreateValidateCommand());
        root.Subcommands.Add(CreatePlanCommand());
        root.Subcommands.Add(CreateApplyCommand());
        root.Subcommands.Add(CreateInspectCommand());
        root.Subcommands.Add(CreateSchemaCommand());
        return root;
    }

    private static Command CreateAnalyzeCommand()
    {
        var command = new Command("analyse", "Analyse an application folder tree.");
        command.Aliases.Add("analyze");
        command.Arguments.Add(new Argument<DirectoryInfo>("appDir"));
        command.Options.Add(new Option<FileInfo?>("--spec-out"));
        return SetStubAction(command);
    }

    private static Command CreateValidateCommand()
    {
        var command = new Command("validate", "Validate an application specification.");
        command.Arguments.Add(new Argument<DirectoryInfo>("appDir"));
        command.Options.Add(new Option<FileInfo>("--spec") { Required = true });
        return SetStubAction(command);
    }

    private static Command CreatePlanCommand()
    {
        var command = new Command("plan", "Create a transformation plan.");
        command.Arguments.Add(new Argument<DirectoryInfo>("appDir"));
        command.Options.Add(new Option<FileInfo>("--spec") { Required = true });
        command.Options.Add(new Option<FileInfo>("--transform") { Required = true });
        command.Options.Add(new Option<FileInfo?>("--out-plan"));
        return SetStubAction(command);
    }

    private static Command CreateApplyCommand()
    {
        var command = new Command("apply", "Apply a transformation plan.");
        command.Arguments.Add(new Argument<DirectoryInfo>("appDir"));
        command.Options.Add(new Option<FileInfo>("--spec") { Required = true });
        command.Options.Add(new Option<FileInfo>("--transform") { Required = true });
        command.Options.Add(new Option<DirectoryInfo>("--output") { Required = true });
        command.Options.Add(new Option<bool>("--dry-run"));
        command.Options.Add(new Option<FileInfo?>("--spec-out"));
        command.Options.Add(new Option<FileInfo?>("--symbols-output"));
        return SetStubAction(command);
    }

    private static Command CreateInspectCommand()
    {
        var command = new Command("inspect", "Inspect an application specification.");
        command.Arguments.Add(new Argument<DirectoryInfo>("appDir"));
        command.Options.Add(new Option<FileInfo>("--spec") { Required = true });
        command.Arguments.Add(new Argument<string?>("view") { Arity = ArgumentArity.ZeroOrOne });
        command.Options.Add(new Option<string?>("--plugin"));
        return SetStubAction(command);
    }

    private static Command CreateSchemaCommand()
    {
        var command = new Command("schema", "Work with schemas.");
        var export = new Command("export", "Export versioned schemas.");
        export.Arguments.Add(new Argument<string?>("kind") { Arity = ArgumentArity.ZeroOrOne });
        export.Options.Add(new Option<DirectoryInfo?>("--output"));
        command.Subcommands.Add(SetStubAction(export));
        return command;
    }

    private static Command SetStubAction(Command command)
    {
        command.SetAction(_ => Task.FromResult(ExitCodes.NotImplemented));
        return command;
    }
}

internal sealed record GlobalOptions(
    Option<string> Verbosity,
    Option<bool> Strict,
    Option<bool> Permissive,
    Option<DirectoryInfo?> Artifacts,
    Option<bool> Offline,
    Option<string[]> Variables,
    Option<bool> Version)
{
    public IReadOnlyList<Option> All => [Verbosity, Strict, Permissive, Artifacts, Offline, Variables, Version];

    public static GlobalOptions Create() => new(
        new Option<string>("--verbosity") { DefaultValueFactory = _ => "normal" },
        new Option<bool>("--strict"),
        new Option<bool>("--permissive"),
        new Option<DirectoryInfo?>("--artefacts") { Aliases = { "--artifacts" } },
        new Option<bool>("--offline"),
        new Option<string[]>("--var") { Arity = ArgumentArity.ZeroOrMore },
        new Option<bool>("--version"));
}
