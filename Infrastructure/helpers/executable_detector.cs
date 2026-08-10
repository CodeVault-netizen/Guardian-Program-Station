using System.IO;
using Guardian.ProgramStation.Core.Enums;

namespace Guardian.ProgramStation.Infrastructure.Helpers;

public static class ExecutableDetector
{
    private static readonly string[] WindowsExtensions = { ".exe", ".msi", ".bat", ".cmd" };

    private const string MacOsBundleMarker = ".app";

    private const string MacOsExecutablesPath = "/Contents/MacOS/";

    public static bool IsExecutable(string filePath)
    {
        if (OperatingSystem.IsWindows())
        {
            return WindowsExtensions.Contains(Path.GetExtension(filePath), StringComparer.OrdinalIgnoreCase);
        }

        if (OperatingSystem.IsMacOS())
        {
            if (Directory.Exists(filePath) && filePath.EndsWith(MacOsBundleMarker, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (filePath.Contains(MacOsExecutablesPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        if (Directory.Exists(filePath))
        {
            return false;
        }

        try
        {
            var mode = File.GetUnixFileMode(filePath);
            return (mode & (UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute)) != 0;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (PlatformNotSupportedException)
        {
            return false;
        }
    }

    public static ExecutableType DetectType(string filePath)
    {
        var extension = Path.GetExtension(filePath);

        if (WindowsExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return ExecutableType.Windows;
        }

        if (filePath.EndsWith(MacOsBundleMarker, StringComparison.OrdinalIgnoreCase)
            || filePath.Contains(MacOsExecutablesPath, StringComparison.OrdinalIgnoreCase))
        {
            return ExecutableType.MacOs;
        }

        return ExecutableType.Linux;
    }
}
