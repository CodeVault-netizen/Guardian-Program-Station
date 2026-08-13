using System.CommandLine;
using System.CommandLine.Parsing;
using Guardian.ProgramStation.Application.Interfaces;
using Guardian.ProgramStation.Application.UseCases;
using Guardian.ProgramStation.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Guardian.ProgramStation.Cli.Commands;

/// <summary>
/// <c>create</c> creates the tree's folder structure on disk through the
/// shared <see cref="CreateTreeUseCase"/> (the same use case the GUI's
/// "Create on Disk" action uses). It never deletes, overwrites, or moves
/// existing files — it only creates what is missing.
/// </summary>
public static class CreateCommand
{
    public static Command Build(IServiceProvider services, TextWriter output, TextWriter error)
    {
        var treeFile = new Option<string>("--tree")
        {
            Description = "Path to a tree JSON file.",
            Required = true,
            HelpName = "FILE",
        };

        var rootPath = new Option<string>("--path")
        {
            Description = "Destination folder where the tree structure is created.",
            Required = true,
            HelpName = "DIR",
        };

        var command = new Command("create", "Create the tree's folder structure on disk.")
        {
            treeFile,
            rootPath,
        };

        command.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
        {
            var treePath = parseResult.GetValue(treeFile)!;
            var destination = parseResult.GetValue(rootPath)!;

            try
            {
                var treeService = services.GetRequiredService<ITreeService>();
                var createTree = services.GetRequiredService<CreateTreeUseCase>();

                var tree = await LoadTreeAsync(treeService, treePath);
                if (tree is null)
                {
                    error.WriteLine($"Could not load the tree file: {treePath}");
                    return ExitCodes.OperationFailed;
                }

                var validation = services.GetRequiredService<ValidateTreeUseCase>().Execute(tree);
                if (!validation.IsValid)
                {
                    foreach (var problem in validation.Errors)
                    {
                        error.WriteLine(problem);
                    }

                    return ExitCodes.ValidationError;
                }

                await createTree.ExecuteAsync(tree, destination, cancellationToken);
                output.WriteLine($"Created tree structure in: {destination}");
                return ExitCodes.Success;
            }
            catch (Exception ex)
            {
                error.WriteLine($"Create failed: {ex.Message}");
                return ExitCodes.OperationFailed;
            }
        });

        return command;
    }

    private static Task<TreeModel?> LoadTreeAsync(ITreeService treeService, string treePath)
    {
        var fullPath = Path.GetFullPath(treePath);
        return treeService.LoadTreeFromFileAsync(fullPath);
    }
}
