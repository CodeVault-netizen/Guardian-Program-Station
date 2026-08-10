using Guardian.ProgramStation.Core.Enums;
using Guardian.ProgramStation.Infrastructure.Indexing;
using Xunit;

namespace Guardian.ProgramStation.Tests;

public sealed class FileSystemIndexerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "gps-indexer-" + Guid.NewGuid().ToString("N"));

    public FileSystemIndexerTests()
    {
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    [Fact]
    public async Task IndexAsync_ReturnsOnlyExecutables_FromGivenFolders()
    {
        if (!OperatingSystem.IsWindows())
        {
            return; // .exe recognition is specific to the Windows host.
        }

        var exe = Path.Combine(_root, "notepad.exe");
        await File.WriteAllTextAsync(exe, string.Empty);
        await File.WriteAllTextAsync(Path.Combine(_root, "readme.txt"), "hello");

        var indexer = new FileSystemIndexer();
        var programs = await indexer.IndexAsync(new[] { _root });

        var program = Assert.Single(programs);
        Assert.Equal("notepad", program.Name);
        Assert.Equal(exe, program.Path);
        Assert.Equal(ExecutableType.Windows, program.ExecutableType);
    }

    [Fact]
    public async Task IndexAsync_RecursesIntoSubdirectories()
    {
        if (!OperatingSystem.IsWindows())
        {
            return; // .exe recognition is specific to the Windows host.
        }

        var nested = Path.Combine(_root, "tools", "bin");
        Directory.CreateDirectory(nested);
        await File.WriteAllTextAsync(Path.Combine(nested, "app.exe"), string.Empty);
        await File.WriteAllTextAsync(Path.Combine(_root, "notes.txt"), "x");

        var indexer = new FileSystemIndexer();
        var programs = await indexer.IndexAsync(new[] { _root });

        var program = Assert.Single(programs);
        Assert.Equal("app", program.Name);
        Assert.Equal(Path.Combine(nested, "app.exe"), program.Path);
    }

    [Fact]
    public async Task IndexAsync_SkipsMissingFolders()
    {
        var indexer = new FileSystemIndexer();
        var programs = await indexer.IndexAsync(new[] { Path.Combine(_root, "does-not-exist") });

        Assert.Empty(programs);
    }

    [Fact]
    public async Task IndexAsync_PopulatesSize()
    {
        if (!OperatingSystem.IsWindows())
        {
            return; // .exe recognition is specific to the Windows host.
        }

        var exe = Path.Combine(_root, "tool.exe");
        await File.WriteAllTextAsync(exe, new string('x', 2048));

        var indexer = new FileSystemIndexer();
        var programs = await indexer.IndexAsync(new[] { _root });

        var program = Assert.Single(programs);
        Assert.Equal(2048, program.Size);
    }
}
