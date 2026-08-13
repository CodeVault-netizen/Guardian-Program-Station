using System.Reflection;
using Guardian.ProgramStation.Cli;
using Guardian.ProgramStation.Kernel;
using Xunit;

namespace Guardian.ProgramStation.Tests;

/// <summary>
/// Tests the CLI end to end through <see cref="CliApplication.RunAsync"/>, the
/// same entry point the executable uses. The Kernel composition root builds the
/// real services; a per-test temp data directory (via the
/// GUARDIAN_PROGRAM_STATION_DATA override) keeps the tests isolated from the
/// user's data.
/// </summary>
public sealed class CliTests : IDisposable
{
    private readonly string _dataDir;

    public CliTests()
    {
        _dataDir = TestData.CreateTempDataDirectory();
    }

    public void Dispose() => TestData.Cleanup(_dataDir);

    private static async Task<(int ExitCode, string Output, string Error)> RunAsync(params string[] args)
    {
        using var services = DependencyInjection.BuildServiceProvider();
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await CliApplication.RunAsync(args, services, output, error);

        return (exitCode, output.ToString(), error.ToString());
    }

    private static string WriteTreeFile(string directory, string json, string fileName = "tree.json")
    {
        var path = Path.Combine(directory, fileName);
        File.WriteAllText(path, json);
        return path;
    }

    private const string ValidTreeJson = """
        {
          "Id": "cli-test-1",
          "Name": "CliTest",
          "Nodes": [
            { "Name": "Project", "NodeType": "folder", "Children": [
              { "Name": "Source", "NodeType": "folder", "Children": [
                { "Name": "Core", "NodeType": "folder", "Children": [] },
                { "Name": "UI", "NodeType": "folder", "Children": [] }
              ] },
              { "Name": "Tests", "NodeType": "folder", "Children": [] }
            ] }
          ]
        }
        """;

    // ---- Basic CLI ----

    [Fact]
    public async Task HelpCommand_ReturnsHelpText()
    {
        var (exitCode, output, _) = await RunAsync("--help");

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Contains("Usage:", output);
        Assert.Contains("create", output);
        Assert.Contains("preview", output);
        Assert.Contains("validate", output);
        Assert.Contains("template", output);
    }

    [Fact]
    public async Task VersionCommand_ReturnsVersion()
    {
        var (exitCode, output, _) = await RunAsync("--version");

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Contains("1.0.0", output);
    }

    [Fact]
    public async Task UnknownCommand_ReturnsError()
    {
        var (exitCode, _, error) = await RunAsync("frobnicate");

        Assert.Equal(ExitCodes.InvalidArguments, exitCode);
        Assert.Contains("frobnicate", error);
    }

    [Fact]
    public async Task InvalidArguments_ReturnsError()
    {
        // create requires --tree and --path
        var (exitCode, _, error) = await RunAsync("create");

        Assert.Equal(ExitCodes.InvalidArguments, exitCode);
        Assert.Contains("--tree", error);
    }

    [Fact]
    public async Task ExitCodes_AreCorrect()
    {
        Assert.Equal(0, ExitCodes.Success);
        Assert.Equal(1, ExitCodes.GeneralError);
        Assert.Equal(2, ExitCodes.InvalidArguments);
        Assert.Equal(3, ExitCodes.ValidationError);
        Assert.Equal(4, ExitCodes.OperationFailed);
    }

    [Fact]
    public void Cli_DoesNotReferenceAvalonia()
    {
        var cliAssembly = typeof(CliApplication).Assembly;
        var uiAssemblyName = "Guardian.ProgramStation.UI";

        // Walk the transitive closure of the CLI's references and make sure no
        // Avalonia assembly or the UI assembly is reachable.
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<AssemblyName>(cliAssembly.GetReferencedAssemblies());
        var offenders = new List<string>();

        while (queue.Count > 0)
        {
            var name = queue.Dequeue();
            if (!visited.Add(name.FullName))
            {
                continue;
            }

            if (name.Name!.StartsWith("Avalonia", StringComparison.OrdinalIgnoreCase)
                || name.Name!.Equals(uiAssemblyName, StringComparison.OrdinalIgnoreCase))
            {
                offenders.Add(name.Name);
                continue;
            }

            try
            {
                var referenced = Assembly.Load(name).GetReferencedAssemblies();
                foreach (var child in referenced)
                {
                    queue.Enqueue(child);
                }
            }
            catch
            {
                // Framework assemblies that cannot be resolved here are never Avalonia/UI.
            }
        }

        Assert.Empty(offenders);
    }

    // ---- Create ----

    [Fact]
    public async Task Create_WithValidTree_CreatesFoldersAndSucceeds()
    {
        var treePath = WriteTreeFile(_dataDir, ValidTreeJson);
        var destination = Path.Combine(_dataDir, "out");

        var (exitCode, output, _) = await RunAsync("create", "--tree", treePath, "--path", destination);

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Contains("Created tree structure", output);
        Assert.True(Directory.Exists(Path.Combine(destination, "Project", "Source", "Core")));
        Assert.True(Directory.Exists(Path.Combine(destination, "Project", "Source", "UI")));
        Assert.True(Directory.Exists(Path.Combine(destination, "Project", "Tests")));
    }

    [Fact]
    public async Task Create_MissingArguments_ReturnsInvalidArguments()
    {
        var (exitCode, _, error) = await RunAsync("create", "--tree", "x.json");

        Assert.Equal(ExitCodes.InvalidArguments, exitCode);
        Assert.Contains("--path", error);
    }

    [Fact]
    public async Task Create_NonexistentTreeFile_ReturnsOperationFailed()
    {
        var missing = Path.Combine(_dataDir, "missing.json");

        var (exitCode, _, error) = await RunAsync("create", "--tree", missing, "--path", _dataDir);

        Assert.Equal(ExitCodes.OperationFailed, exitCode);
        Assert.Contains("Could not load the tree file", error);
    }

    [Fact]
    public async Task Create_InvalidTree_ReturnsValidationError()
    {
        var treePath = WriteTreeFile(_dataDir, """{"Name":"","Nodes":[]}""");

        var (exitCode, _, error) = await RunAsync("create", "--tree", treePath, "--path", Path.Combine(_dataDir, "o"));

        Assert.Equal(ExitCodes.ValidationError, exitCode);
        Assert.Contains("no name", error);
    }

    [Fact]
    public async Task Create_DoesNotOverwriteExistingFolders()
    {
        var treePath = WriteTreeFile(_dataDir, ValidTreeJson);
        var destination = Path.Combine(_dataDir, "existing");
        var marker = Path.Combine(destination, "Project", "keep.txt");
        Directory.CreateDirectory(Path.Combine(destination, "Project"));
        File.WriteAllText(marker, "keep");

        var (exitCode, _, _) = await RunAsync("create", "--tree", treePath, "--path", destination);

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.True(File.Exists(marker));
        Assert.Equal("keep", File.ReadAllText(marker));
    }

    // ---- Preview ----

    [Fact]
    public async Task Preview_ValidTree_ProducesAsciiTree()
    {
        var treePath = WriteTreeFile(_dataDir, ValidTreeJson);

        var (exitCode, output, _) = await RunAsync("preview", "--tree", treePath);

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Contains("Project", output);
        Assert.Contains("Source", output);
        Assert.Contains("Core", output);
        Assert.Contains("UI", output);
        Assert.Contains("Tests", output);
        // Box-drawing connectors present.
        Assert.Contains("├──", output);
        Assert.Contains("└──", output);
    }

    [Fact]
    public async Task Preview_NonexistentTree_ReturnsOperationFailed()
    {
        var missing = Path.Combine(_dataDir, "missing.json");

        var (exitCode, _, error) = await RunAsync("preview", "--tree", missing);

        Assert.Equal(ExitCodes.OperationFailed, exitCode);
        Assert.Contains("Could not load the tree file", error);
    }

    [Fact]
    public async Task Preview_MissingArgument_ReturnsInvalidArguments()
    {
        var (exitCode, _, _) = await RunAsync("preview");

        Assert.Equal(ExitCodes.InvalidArguments, exitCode);
    }

    // ---- Validate ----

    [Fact]
    public async Task Validate_ValidTree_PrintsValid()
    {
        var treePath = WriteTreeFile(_dataDir, ValidTreeJson);

        var (exitCode, output, _) = await RunAsync("validate", "--tree", treePath);

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Equal("Valid", output.Trim());
    }

    [Fact]
    public async Task Validate_InvalidTree_PrintsInvalidWithErrors()
    {
        var treePath = WriteTreeFile(_dataDir, """{"Name":"","Nodes":[]}""");

        var (exitCode, output, error) = await RunAsync("validate", "--tree", treePath);

        Assert.Equal(ExitCodes.ValidationError, exitCode);
        Assert.Equal("Invalid", output.Trim());
        Assert.Contains("no name", error);
        Assert.Contains("no root nodes", error);
    }

    [Fact]
    public async Task Validate_NonexistentFile_PrintsInvalid()
    {
        var missing = Path.Combine(_dataDir, "missing.json");

        var (exitCode, output, _) = await RunAsync("validate", "--tree", missing);

        Assert.Equal(ExitCodes.ValidationError, exitCode);
        Assert.Equal("Invalid", output.Trim());
    }

    // ---- Template ----

    [Fact]
    public async Task Template_CreateListExportDelete_RoundTrips()
    {
        var treePath = WriteTreeFile(_dataDir, ValidTreeJson, "template-source.json");

        var (createCode, createOut, _) = await RunAsync("template", "create", "--tree", treePath, "--name", "CliTemplate");
        Assert.Equal(ExitCodes.Success, createCode);
        Assert.Contains("CliTemplate", createOut);

        var (listCode, listOut, _) = await RunAsync("template", "list");
        Assert.Equal(ExitCodes.Success, listCode);
        Assert.Contains("CliTemplate", listOut);

        var (exportCode, exportOut, _) = await RunAsync("template", "export", "--id", "cli-test-1", "--output", Path.Combine(_dataDir, "exported.json"));
        Assert.Equal(ExitCodes.Success, exportCode);
        Assert.Contains("Exported", exportOut);
        Assert.True(File.Exists(Path.Combine(_dataDir, "exported.json")));

        var (deleteCode, deleteOut, _) = await RunAsync("template", "delete", "--id", "cli-test-1");
        Assert.Equal(ExitCodes.Success, deleteCode);
        Assert.Contains("Deleted", deleteOut);

        var (listAfterCode, listAfterOut, _) = await RunAsync("template", "list");
        Assert.Equal(ExitCodes.Success, listAfterCode);
        Assert.DoesNotContain("CliTemplate", listAfterOut);
    }

    [Fact]
    public async Task Template_Create_NonexistentFile_ReturnsOperationFailed()
    {
        var (exitCode, _, error) = await RunAsync("template", "create", "--tree", Path.Combine(_dataDir, "nope.json"));

        Assert.Equal(ExitCodes.OperationFailed, exitCode);
        Assert.Contains("Could not load the tree file", error);
    }

    [Fact]
    public async Task Template_Delete_MissingId_ReturnsInvalidArguments()
    {
        var (exitCode, _, _) = await RunAsync("template", "delete");

        Assert.Equal(ExitCodes.InvalidArguments, exitCode);
    }

    [Fact]
    public async Task Template_Delete_UnknownId_SucceedsIdempotently()
    {
        // Deleting a missing id is a no-op success (matching the storage service).
        var (exitCode, _, _) = await RunAsync("template", "delete", "--id", "does-not-exist");

        Assert.Equal(ExitCodes.Success, exitCode);
    }

    [Fact]
    public async Task Template_Export_UnknownId_ReturnsOperationFailed()
    {
        var (exitCode, _, error) = await RunAsync("template", "export", "--id", "nope", "--output", Path.Combine(_dataDir, "o.json"));

        Assert.Equal(ExitCodes.OperationFailed, exitCode);
        Assert.Contains("No saved tree", error);
    }
}
