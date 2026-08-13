using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Reflection;
using Guardian.ProgramStation.Cli.Commands;

namespace Guardian.ProgramStation.Cli;

/// <summary>
/// Wires the CLI commands to the Application use cases and renders their
/// results to the terminal. No UI or platform-specific code lives here.
/// </summary>
public static class CliApplication
{
    /// <summary>
    /// Runs the CLI with the given arguments and returns the process exit code.
    /// <paramref name="services"/> is the application composition root built by
    /// the Kernel (the same one the GUI uses).
    /// </summary>
    public static async Task<int> RunAsync(
        string[] args,
        IServiceProvider services,
        TextWriter? output = null,
        TextWriter? error = null)
    {
        var outWriter = output ?? Console.Out;
        var errWriter = error ?? Console.Error;

        var root = BuildRootCommand(services, outWriter, errWriter);

        // The built-in version option reads the *entry* assembly, which is the
        // test host under `dotnet test`; we replace it with one that reports
        // this CLI assembly's unified version (set in Directory.Build.props).
        var builtInVersion = root.Options.OfType<VersionOption>().FirstOrDefault();
        if (builtInVersion is not null)
        {
            root.Options.Remove(builtInVersion);
        }

        var versionOption = new Option<bool>("--version")
        {
            Description = "Show version information.",
        };
        root.Options.Add(versionOption);

        var configuration = new CommandLineConfiguration(root)
        {
            Output = outWriter,
            Error = errWriter,
        };

        // Root action: `--version` reports the version; a bare invocation
        // (no subcommand) shows help like a well-behaved CLI. Subcommands have
        // their own actions, so they never reach this one.
        root.SetAction(parseResult =>
        {
            if (parseResult.GetValue(versionOption))
            {
                outWriter.WriteLine(GetVersion());
                return ExitCodes.Success;
            }

            return root.Parse(new[] { "--help" }, configuration).Invoke();
        });

        var parseResult = root.Parse(args, configuration);

        // System.CommandLine reports invalid input through ParseErrorAction and
        // returns 1; we map it to the CLI contract's "invalid arguments" = 2.
        if (parseResult.Action is ParseErrorAction)
        {
            foreach (var parseError in parseResult.Errors)
            {
                errWriter.WriteLine(parseError.Message);
            }

            return ExitCodes.InvalidArguments;
        }

        return await parseResult.InvokeAsync();
    }

    /// <summary>
    /// The unified application version, read from this assembly (set by
    /// Directory.Build.props) — never a hard-coded string.
    /// </summary>
    public static string GetVersion()
    {
        var assembly = typeof(CliApplication).Assembly;
        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        return string.IsNullOrWhiteSpace(informational)
            ? assembly.GetName().Version?.ToString(3) ?? "unknown"
            : informational;
    }

    private static RootCommand BuildRootCommand(IServiceProvider services, TextWriter output, TextWriter error)
    {
        var root = new RootCommand(
            "Guardian Program Station command-line interface. " +
            "Creates, previews, validates and manages trees using the same " +
            "application use cases as the GUI.")
        {
            CreateCommand.Build(services, output, error),
            PreviewCommand.Build(services, output, error),
            ValidateCommand.Build(services, output, error),
            TemplateCommand.Build(services, output, error),
        };

        return root;
    }
}
