using Guardian.ProgramStation.Infrastructure.Services;
using Xunit;

namespace Guardian.ProgramStation.Tests;

/// <summary>
/// These tests verify the <c>IClipboardService</c> abstraction contract only.
/// They run against the in-memory <see cref="ClipboardService"/> as a test
/// double. They do NOT prove the real Windows system clipboard works; the
/// production implementation (<c>SystemClipboardService</c>) uses Avalonia's
/// TopLevel.Clipboard API and requires a running Windows UI session.
/// </summary>
public sealed class ClipboardServiceTests
{
    [Fact]
    public async Task SetThenGet_ReturnsText()
    {
        var service = new ClipboardService();

        await service.SetTextAsync("hello");

        Assert.Equal("hello", await service.GetTextAsync());
    }

    [Fact]
    public async Task Get_WhenEmpty_ReturnsNull()
    {
        var service = new ClipboardService();

        Assert.Null(await service.GetTextAsync());
    }

    [Fact]
    public async Task Set_OverwritesPreviousText()
    {
        var service = new ClipboardService();
        await service.SetTextAsync("first");

        await service.SetTextAsync("second");

        Assert.Equal("second", await service.GetTextAsync());
    }

    [Fact]
    public async Task SetThenGet_FilesRoundTrips()
    {
        var service = new ClipboardService();
        var paths = new[] { @"C:\a.txt", @"C:\folder" };

        await service.SetFilesAsync(paths);

        var loaded = await service.GetFilesAsync();
        Assert.Equal(paths, loaded);
    }

    [Fact]
    public async Task GetFiles_WhenEmpty_ReturnsEmptyList()
    {
        var service = new ClipboardService();

        var loaded = await service.GetFilesAsync();

        Assert.Empty(loaded);
    }

    [Fact]
    public async Task Clear_RemovesTextAndFiles()
    {
        var service = new ClipboardService();
        await service.SetTextAsync("hello");
        await service.SetFilesAsync(new[] { @"C:\a.txt" });

        await service.ClearAsync();

        Assert.Null(await service.GetTextAsync());
        Assert.Empty(await service.GetFilesAsync());
    }
}
