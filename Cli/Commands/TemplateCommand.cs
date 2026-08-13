using System.CommandLine;
using System.CommandLine.Parsing;
using Guardian.ProgramStation.Application.Interfaces;
using Guardian.ProgramStation.Application.UseCases;
using Guardian.ProgramStation.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Guardian.ProgramStation.Cli.Commands;

/// <summary>
/// <c>template</c> manages saved trees (the application's template library).
/// Every subcommand maps to an existing service operation — nothing is
/// invented here:
///   list   → ITreeService.LoadTreesAsync
///   create → ITreeService.LoadTreeFromFileAsync + SaveTreeAsync
///   import → ImportTreeUseCase (same import the GUI uses)
///   export → ITreeService.LoadTreeAsync + SaveTreeToFileAsync
///   delete → ITreeService.DeleteTreeAsync
/// </summary>
public static class TemplateCommand
{
    public static Command Build(IServiceProvider services, TextWriter output, TextWriter error)
    {
        var template = new Command("template", "Manage saved trees (the template library).");

        template.Subcommands.Add(BuildList(services, output, error));
        template.Subcommands.Add(BuildCreate(services, output, error));
        template.Subcommands.Add(BuildImport(services, output, error));
        template.Subcommands.Add(BuildExport(services, output, error));
        template.Subcommands.Add(BuildDelete(services, output, error));

        return template;
    }

    private static Command BuildList(IServiceProvider services, TextWriter output, TextWriter error)
    {
        var command = new Command("list", "List all saved trees.");

        command.SetAction(async (ParseResult _, CancellationToken cancellationToken) =>
        {
            try
            {
                var treeService = services.GetRequiredService<ITreeService>();
                var trees = await treeService.LoadTreesAsync(cancellationToken);

                if (trees.Count == 0)
                {
                    output.WriteLine("No saved trees.");
                    return ExitCodes.Success;
                }

                foreach (var tree in trees.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase))
                {
                    output.WriteLine($"{tree.Id}  {tree.Name}");
                }

                return ExitCodes.Success;
            }
            catch (Exception ex)
            {
                error.WriteLine($"Template list failed: {ex.Message}");
                return ExitCodes.OperationFailed;
            }
        });

        return command;
    }

    private static Command BuildCreate(IServiceProvider services, TextWriter output, TextWriter error)
    {
        var treeFile = new Option<string>("--tree")
        {
            Description = "Path to a tree JSON file to save as a template.",
            Required = true,
            HelpName = "FILE",
        };

        var name = new Option<string>("--name")
        {
            Description = "Optional template name (defaults to the tree's name).",
            HelpName = "NAME",
        };

        var command = new Command("create", "Save a tree file as a saved tree (template).")
        {
            treeFile,
            name,
        };

        command.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
        {
            var treePath = parseResult.GetValue(treeFile)!;

            try
            {
                var treeService = services.GetRequiredService<ITreeService>();
                var tree = await treeService.LoadTreeFromFileAsync(Path.GetFullPath(treePath), cancellationToken);
                if (tree is null)
                {
                    error.WriteLine($"Could not load the tree file: {treePath}");
                    return ExitCodes.OperationFailed;
                }

                var templateName = parseResult.GetValue(name);
                if (!string.IsNullOrWhiteSpace(templateName))
                {
                    tree.Name = templateName.Trim();
                }

                await treeService.SaveTreeAsync(tree, cancellationToken);
                output.WriteLine($"Template saved: {tree.Name} ({tree.Id})");
                return ExitCodes.Success;
            }
            catch (Exception ex)
            {
                error.WriteLine($"Template create failed: {ex.Message}");
                return ExitCodes.OperationFailed;
            }
        });

        return command;
    }

    private static Command BuildImport(IServiceProvider services, TextWriter output, TextWriter error)
    {
        var folder = new Option<string>("--path")
        {
            Description = "Folder to import as a template.",
            Required = true,
            HelpName = "DIR",
        };

        var command = new Command("import", "Import a folder structure as a saved tree (template).")
        {
            folder,
        };

        command.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
        {
            var folderPath = parseResult.GetValue(folder)!;

            try
            {
                var import = services.GetRequiredService<ImportTreeUseCase>();
                var result = await import.ExecuteAsync(Path.GetFullPath(folderPath), true, cancellationToken);
                output.WriteLine($"Template imported: {result.Tree.Name} ({result.Tree.Id})");
                return ExitCodes.Success;
            }
            catch (Exception ex)
            {
                error.WriteLine($"Template import failed: {ex.Message}");
                return ExitCodes.OperationFailed;
            }
        });

        return command;
    }

    private static Command BuildExport(IServiceProvider services, TextWriter output, TextWriter error)
    {
        var id = new Option<string>("--id")
        {
            Description = "Template (saved tree) id to export.",
            Required = true,
            HelpName = "ID",
        };

        var destination = new Option<string>("--output")
        {
            Description = "Destination file path.",
            Required = true,
            HelpName = "FILE",
        };

        var command = new Command("export", "Export a saved tree to a JSON file.")
        {
            id,
            destination,
        };

        command.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
        {
            var treeId = parseResult.GetValue(id)!;
            var outputPath = parseResult.GetValue(destination)!;

            try
            {
                var treeService = services.GetRequiredService<ITreeService>();
                var tree = await treeService.LoadTreeAsync(treeId, cancellationToken);
                if (tree is null)
                {
                    error.WriteLine($"No saved tree with id '{treeId}'.");
                    return ExitCodes.OperationFailed;
                }

                var fullPath = Path.GetFullPath(outputPath);
                await treeService.SaveTreeToFileAsync(tree, fullPath, cancellationToken);
                output.WriteLine($"Exported: {fullPath}");
                return ExitCodes.Success;
            }
            catch (Exception ex)
            {
                error.WriteLine($"Template export failed: {ex.Message}");
                return ExitCodes.OperationFailed;
            }
        });

        return command;
    }

    private static Command BuildDelete(IServiceProvider services, TextWriter output, TextWriter error)
    {
        var id = new Option<string>("--id")
        {
            Description = "Template (saved tree) id to delete.",
            Required = true,
            HelpName = "ID",
        };

        var command = new Command("delete", "Delete a saved tree (template).")
        {
            id,
        };

        command.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
        {
            var treeId = parseResult.GetValue(id)!;

            try
            {
                var treeService = services.GetRequiredService<ITreeService>();
                await treeService.DeleteTreeAsync(treeId, cancellationToken);
                output.WriteLine($"Deleted: {treeId}");
                return ExitCodes.Success;
            }
            catch (Exception ex)
            {
                error.WriteLine($"Template delete failed: {ex.Message}");
                return ExitCodes.OperationFailed;
            }
        });

        return command;
    }
}
