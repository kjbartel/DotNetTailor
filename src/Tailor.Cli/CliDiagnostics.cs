namespace Tailor.Cli;

internal static class CliDiagnostics
{
    public const string NotImplemented = "TLR0100";
    public const string InvalidVariable = "TLR0101";
    public const string DuplicateVariable = "TLR0102";
    public const string ConflictingFailureModes = "TLR0103";
    public const string UnknownSchemaKind = "TLR0104";
    public const string ParseError = "TLR0105";
    public const string UnhandledException = "TLR0199";
}

internal enum FailureMode
{
    Default,
    Strict,
    Permissive,
}

internal enum Verbosity
{
    Quiet,
    Minimal,
    Normal,
    Detailed,
    Diagnostic,
}

internal sealed record GlobalSettings(
    Verbosity Verbosity,
    FailureMode FailureMode,
    string? ArtifactsDirectory,
    bool Offline,
    IReadOnlyDictionary<string, string> Variables);

internal static class ExitCodes
{
    public const int Success = 0;
    public const int Usage = 2;
    public const int WarningsAsErrors = 3;
    public const int Environment = 4;
    public const int ExecutionFailure = 5;
    public const int InternalError = 70;
    public const int NotImplemented = 70;
    public const int Cancelled = 130;
}
