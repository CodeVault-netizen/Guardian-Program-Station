using System.CommandLine;
using System.CommandLine.Parsing;
using Guardian.ProgramStation.Application.Interfaces;
using Guardian.ProgramStation.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace Guardian.ProgramStation.Cli.Commands;

/// <summary>
/// <c>preview</c> renders a tree as ASCII text using the shared
/// <see cref="PreviewTreeUseCase"/> — the same preview the GUI shows, so the
/// terminal and the UI always agree.
/// </summary>
public static class PreviewCommand
{
    public static Command Build(IServiceProvider services, TextWriter output, TextWriter error)
    {
        var treeFile = new Option<string>("--tree")
        {
            Description = "Path to a tree JSON file.",
            Required = true,
            HelpName = "FILE",
        };

        var command = new Command("preview", "Render a tree as ASCII text.")
        {
            treeFile,
        };

        command.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
        {
            var treePath = parseResult.GetValue(treeFile)!;

            try
            {
                var treeService = services.GetRequiredService<ITreeService>();
                var preview = services.GetRequiredService<PreviewTreeUseCase>();

                var tree = await treeService.LoadTreeFromFileAsync(Path.GetFullPath(treePath), cancellationToken);
                if (tree is null)
                {
                    error.WriteLine($"Could not load the tree file: {treePath}");
                    return ExitCodes.OperationFailed;
                }

                output.WriteLine(preview.Execute(tree));
                return ExitCodes.Success;
            }
            catch (Exception ex)
            {
                error.WriteLine($"Preview failed: {ex.Message}");
                return ExitCodes.OperationFailed;
            }
        });

        return command;
    }
}
