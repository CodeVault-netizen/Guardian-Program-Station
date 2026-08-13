using System.CommandLine;
using System.CommandLine.Parsing;
using Guardian.ProgramStation.Application.Interfaces;
using Guardian.ProgramStation.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace Guardian.ProgramStation.Cli.Commands;

/// <summary>
/// <c>validate</c> checks a tree against the shared
/// <see cref="ValidateTreeUseCase"/> — the same structural rules the GUI
/// relies on. Prints "Valid" or "Invalid" plus every problem found.
/// </summary>
public static class ValidateCommand
{
    public static Command Build(IServiceProvider services, TextWriter output, TextWriter error)
    {
        var treeFile = new Option<string>("--tree")
        {
            Description = "Path to a tree JSON file.",
            Required = true,
            HelpName = "FILE",
        };

        var command = new Command("validate", "Validate a tree file.")
        {
            treeFile,
        };

        command.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
        {
            var treePath = parseResult.GetValue(treeFile)!;

            try
            {
                var treeService = services.GetRequiredService<ITreeService>();
                var validation = services.GetRequiredService<ValidateTreeUseCase>();

                var tree = await treeService.LoadTreeFromFileAsync(Path.GetFullPath(treePath), cancellationToken);
                var result = validation.Execute(tree);

                if (result.IsValid)
                {
                    output.WriteLine("Valid");
                    return ExitCodes.Success;
                }

                output.WriteLine("Invalid");
                foreach (var problem in result.Errors)
                {
                    error.WriteLine(problem);
                }

                return ExitCodes.ValidationError;
            }
            catch (Exception ex)
            {
                error.WriteLine($"Validation failed: {ex.Message}");
                return ExitCodes.OperationFailed;
            }
        });

        return command;
    }
}
