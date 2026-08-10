using Guardian.ProgramStation.Core.Enums;
using Guardian.ProgramStation.Infrastructure.Helpers;
using Xunit;

namespace Guardian.ProgramStation.Tests;

public sealed class ExecutableDetectorTests
{
    [Theory]
    [InlineData("setup.exe")]
    [InlineData("installer.msi")]
    [InlineData("run.bat")]
    [InlineData("run.cmd")]
    public void DetectType_ReturnsWindows_ForWindowsExtensions(string fileName)
    {
        Assert.Equal(ExecutableType.Windows, ExecutableDetector.DetectType(fileName));
    }

    [Theory]
    [InlineData("MyApp.app")]
    [InlineData("MyApp.app/Contents/MacOS/MyApp")]
    [InlineData("/Applications/MyApp.app/Contents/MacOS/MyApp")]
    public void DetectType_ReturnsMacOs_ForAppBundlePaths(string path)
    {
        Assert.Equal(ExecutableType.MacOs, ExecutableDetector.DetectType(path));
    }

    [Theory]
    [InlineData("/usr/bin/tool")]
    [InlineData("script.sh")]
    [InlineData("tool")]
    [InlineData("readme.txt")]
    public void DetectType_ReturnsLinux_ForEverythingElse(string path)
    {
        Assert.Equal(ExecutableType.Linux, ExecutableDetector.DetectType(path));
    }

    [Fact]
    public void DetectType_IsCaseInsensitive()
    {
        Assert.Equal(ExecutableType.Windows, ExecutableDetector.DetectType("SETUP.EXE"));
        Assert.Equal(ExecutableType.MacOs, ExecutableDetector.DetectType("MyApp.APP"));
    }

    [Fact]
    public void IsExecutable_Windows_AcceptsWindowsExecutablesOnly()
    {
        if (!OperatingSystem.IsWindows())
        {
            return; // This branch is specific to the Windows host.
        }

        Assert.True(ExecutableDetector.IsExecutable(@"C:\tools\setup.exe"));
        Assert.True(ExecutableDetector.IsExecutable(@"C:\tools\installer.msi"));
        Assert.True(ExecutableDetector.IsExecutable(@"C:\tools\run.bat"));
        Assert.True(ExecutableDetector.IsExecutable(@"C:\tools\run.cmd"));
        Assert.False(ExecutableDetector.IsExecutable(@"C:\tools\readme.txt"));
        Assert.False(ExecutableDetector.IsExecutable(@"C:\tools\archive.zip"));
    }
}
