using System.IO;

namespace Guardian.ProgramStation.Infrastructure.Helpers;

public static class PathHelper
{
    private const string ApplicationFolderName = "Guardian Program Station";
    private const string DataFolderName = "Data";

    public static string GetDataDirectory()
    {
        var overridePath = Environment.GetEnvironmentVariable("GUARDIAN_PROGRAM_STATION_DATA");
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            return Path.Combine(overridePath, DataFolderName);
        }

        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ApplicationFolderName, DataFolderName);
    }

    public static string GetProgramsDirectory() => Path.Combine(GetDataDirectory(), "Programs");

    public static string GetTreesDirectory() => Path.Combine(GetDataDirectory(), "Trees");

    public static string GetThemesDirectory() => Path.Combine(GetDataDirectory(), "Themes");

    public static string GetSettingsDirectory() => Path.Combine(GetDataDirectory(), "Settings");

    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(GetProgramsDirectory());
        Directory.CreateDirectory(GetTreesDirectory());
        Directory.CreateDirectory(GetThemesDirectory());
        Directory.CreateDirectory(GetSettingsDirectory());
    }

    public static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c));
    }
}
